using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

/// <summary>
/// Provides comprehensive desktop input controls for the Unity editor when VR controllers are not available.
/// Combines keyboard movement, mouse look, and mouse-based object interaction into a single controller.
/// Note: This component is disabled on Android builds to avoid conflicts with VR input.
/// </summary>
#if UNITY_ANDROID && !UNITY_EDITOR
// Empty MonoBehaviour for Android builds to maintain component references
public class DesktopInputController : MonoBehaviour
{
    // This class is intentionally empty on Android builds
    // to prevent desktop input conflicts with VR controllers
}
#else
public class DesktopInputController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private bool enableDesktopControls = true;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float mouseSensitivity = 0.5f;
    [SerializeField] private float mouseTurnSensitivity = 0.5f;
    
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 20f;
    [SerializeField] private LayerMask interactableLayers = -1;
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private Color debugRayColor = Color.red;
    
    [Header("Grab Settings")]
    [SerializeField] private float grabDistance = 2f;
    [SerializeField] private float throwForce = 5f;
    
    // Component references
    private XROrigin xrOrigin;
    private Camera xrCamera;
    private CharacterController characterController;
    private ActionBasedContinuousMoveProvider moveProvider;
    private ActionBasedContinuousTurnProvider turnProvider;
    
    // Input System references
    private Keyboard keyboard;
    private Mouse mouse;
    
    // Movement state
    private Vector2 moveInput;
    private Vector2 turnInput;
    private bool isMouseLookActive = false;
    private bool setupComplete = false;
    private float currentPitch = 0f;
    
    // Interaction state
    private XRGrabInteractable currentGrabbedObject;
    private XRSimpleInteractable currentSimpleInteractable;
    private Rigidbody grabbedRigidbody;
    private Vector3 grabOffset;
    private bool isGrabbing = false;
    
    private void Start()
    {
        // Only enable in editor
        if (!Application.isEditor || !enableDesktopControls)
        {
            enabled = false;
            return;
        }
        
        SetupComponents();
        SetupExitCube();
    }
    
    private void SetupComponents()
    {
        // Find XR Origin
        xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogWarning("DesktopInputController: No XROrigin found in scene");
            enabled = false;
            return;
        }
        
        // Get camera
        xrCamera = xrOrigin.Camera;
        if (xrCamera == null)
        {
            Debug.LogWarning("DesktopInputController: No camera found on XROrigin");
            enabled = false;
            return;
        }
        
        // Get character controller
        characterController = xrOrigin.GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogWarning("DesktopInputController: No CharacterController found on XROrigin");
            enabled = false;
            return;
        }
        
        // Setup Input System
        keyboard = Keyboard.current;
        mouse = Mouse.current;
        
        if (keyboard == null)
        {
            Debug.LogWarning("DesktopInputController: No keyboard detected!");
            enabled = false;
            return;
        }
        
        // Get locomotion providers (optional - we'll work without them if needed)
        moveProvider = xrOrigin.GetComponent<ActionBasedContinuousMoveProvider>();
        turnProvider = xrOrigin.GetComponent<ActionBasedContinuousTurnProvider>();
        
        // Disable the original locomotion providers to prevent conflicts
        if (moveProvider != null)
        {
            moveProvider.enabled = false;
        }
        if (turnProvider != null)
        {
            turnProvider.enabled = false;
        }
        
        setupComplete = true;
        
        // Initialize currentPitch to match the camera's current rotation
        if (xrCamera != null)
        {
            currentPitch = xrCamera.transform.localEulerAngles.x;
            // Normalize the pitch to be within our expected range
            if (currentPitch > 180f)
                currentPitch -= 360f;
        }
        
        Debug.Log("DesktopInputController: Setup complete - Desktop controls enabled");
    }
    
    private void SetupExitCube()
    {
        // Debug: Look specifically for ExitCube
        GameObject exitCube = GameObject.Find("ExitCube");
        if (exitCube != null)
        {
            XRSimpleInteractable exitInteractable = exitCube.GetComponent<XRSimpleInteractable>();
            ExitButton exitButton = exitCube.GetComponent<ExitButton>();
            
            // Only add ExitButton if it doesn't exist and there's no XRSimpleInteractable
            if (exitButton == null && exitInteractable == null)
            {
                exitButton = exitCube.AddComponent<ExitButton>();
            }
            
            // Ensure the ExitCube has a collider for interaction
            Collider exitCollider = exitCube.GetComponent<Collider>();
            if (exitCollider == null)
            {
                BoxCollider boxCollider = exitCube.AddComponent<BoxCollider>();
                boxCollider.size = Vector3.one; // Default size
                boxCollider.isTrigger = false; // Not a trigger
            }
        }
    }
    
    private void Update()
    {
        if (!enabled || !setupComplete) return;
        
        HandleKeyboardInput();
        HandleMouseInput();
        HandleMouseInteraction();
        ApplyMovement();
        UpdateGrabbedObject();
        
        // Ensure camera pitch is maintained even when not in mouse look mode
        MaintainCameraPitch();
    }
    
    private void HandleKeyboardInput()
    {
        // Reset inputs
        moveInput = Vector2.zero;
        turnInput = Vector2.zero;
        
        // Movement input (WASD) using new Input System
        if (keyboard.wKey.isPressed)
            moveInput.y += 1f;
        if (keyboard.sKey.isPressed)
            moveInput.y -= 1f;
        if (keyboard.dKey.isPressed)
            moveInput.x += 1f;
        if (keyboard.aKey.isPressed)
            moveInput.x -= 1f;
        
        // Turn input (QE) using new Input System
        if (keyboard.eKey.isPressed)
            turnInput.x += 1f;
        if (keyboard.qKey.isPressed)
            turnInput.x -= 1f;
        
        // Normalize movement input
        if (moveInput.magnitude > 1f)
            moveInput.Normalize();
    }
    
    private void HandleMouseInput()
    {
        if (mouse == null) return;
        
        // Toggle mouse look with right mouse button
        if (mouse.rightButton.wasPressedThisFrame)
        {
            isMouseLookActive = !isMouseLookActive;
            Cursor.lockState = isMouseLookActive ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isMouseLookActive;
            
            // When disabling mouse look, ensure we preserve the current pitch
            if (!isMouseLookActive && xrCamera != null)
            {
                // Preserve the current pitch when exiting mouse look mode
                xrCamera.transform.localRotation = Quaternion.Euler(currentPitch, xrCamera.transform.localEulerAngles.y, 0f);
            }
        }
        
        if (isMouseLookActive)
        {
            // Mouse look for turning using new Input System
            Vector2 mouseDelta = mouse.delta.ReadValue();
            float mouseX = mouseDelta.x * mouseTurnSensitivity;
            turnInput.x += mouseX;
            
            // Mouse look for camera pitch (up/down)
            float mouseY = mouseDelta.y * mouseSensitivity;
            
            // Apply pitch to camera
            if (xrCamera != null)
            {
                // Update pitch directly without converting from Euler angles
                currentPitch = Mathf.Clamp(currentPitch - mouseY, -80f, 80f);
                xrCamera.transform.localRotation = Quaternion.Euler(currentPitch, xrCamera.transform.localEulerAngles.y, 0f);
            }
        }
    }
    
    private void HandleMouseInteraction()
    {
        // Middle mouse button for grab/drop - only when not in mouse look mode
        if (mouse != null && mouse.middleButton.wasPressedThisFrame && !isMouseLookActive)
        {
            if (isGrabbing)
            {
                DropObject();
            }
            else
            {
                TryGrabObject();
            }
        }
        
        // Right click to throw (if holding an object) - only when not in mouse look mode
        if (mouse.rightButton.wasPressedThisFrame && isGrabbing && !isMouseLookActive)
        {
            ThrowObject();
        }
        
        // Debug: Add escape key to exit game for testing
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
    
    private void TryGrabObject()
    {
        // Find all grabbable objects in the scene
        XRGrabInteractable[] allInteractables = FindObjectsOfType<XRGrabInteractable>();
        
        XRGrabInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;
        Vector3 closestHitPoint = Vector3.zero;
        
        // Find the closest grabbable object within interaction distance
        foreach (var interactable in allInteractables)
        {
            // Calculate distance from camera to object
            float distance = Vector3.Distance(xrCamera.transform.position, interactable.transform.position);
            
            if (distance <= interactionDistance && distance < closestDistance)
            {
                // Check if the object is in front of the camera (within a reasonable angle)
                Vector3 directionToObject = (interactable.transform.position - xrCamera.transform.position).normalized;
                float angle = Vector3.Angle(xrCamera.transform.forward, directionToObject);
                
                // Only consider objects within a 60-degree cone in front of the camera
                if (angle <= 60f)
                {
                    closestInteractable = interactable;
                    closestDistance = distance;
                    closestHitPoint = interactable.transform.position;
                }
            }
        }
        
        // Also check for simple interactables (like exit cube)
        if (closestInteractable == null)
        {
            XRSimpleInteractable[] simpleInteractables = FindObjectsOfType<XRSimpleInteractable>();
            foreach (var simpleInteractable in simpleInteractables)
            {
                float distance = Vector3.Distance(xrCamera.transform.position, simpleInteractable.transform.position);
                if (distance <= interactionDistance && distance < closestDistance)
                {
                    Vector3 directionToObject = (simpleInteractable.transform.position - xrCamera.transform.position).normalized;
                    float angle = Vector3.Angle(xrCamera.transform.forward, directionToObject);
                    
                    if (angle <= 60f)
                    {
                        ActivateSimpleInteractable(simpleInteractable);
                        return;
                    }
                }
            }
        }
        
        // Act on the closest interactable found
        if (closestInteractable != null)
        {
            GrabObject(closestInteractable, closestHitPoint);
        }
    }
    
    private void GrabObject(XRGrabInteractable interactable, Vector3 hitPoint)
    {
        currentGrabbedObject = interactable;
        grabbedRigidbody = interactable.GetComponent<Rigidbody>();
        
        if (grabbedRigidbody != null)
        {
            // Calculate offset from hit point to object center
            grabOffset = interactable.transform.position - hitPoint;
            
            // Disable gravity while grabbed
            grabbedRigidbody.useGravity = false;
            grabbedRigidbody.isKinematic = true;
            
            isGrabbing = true;
        }
    }
    
    private void ActivateSimpleInteractable(XRSimpleInteractable simpleInteractable)
    {
        currentSimpleInteractable = simpleInteractable;
        
        // Simulate the interaction by calling the selectEntered event
        var selectEnterEventArgs = new SelectEnterEventArgs();
        simpleInteractable.selectEntered.Invoke(selectEnterEventArgs);
        
        // Clear the reference after activation
        currentSimpleInteractable = null;
    }
    
    private void UpdateGrabbedObject()
    {
        if (!isGrabbing || currentGrabbedObject == null || grabbedRigidbody == null) return;
        
        // Position the object in front of the camera
        Vector3 targetPosition = xrCamera.transform.position + 
                               xrCamera.transform.forward * grabDistance + 
                               grabOffset;
        
        // Smoothly move the object to the target position
        grabbedRigidbody.MovePosition(Vector3.Lerp(grabbedRigidbody.position, targetPosition, Time.deltaTime * 10f));
        
        // Optional: Make the object follow camera rotation
        Quaternion targetRotation = xrCamera.transform.rotation;
        grabbedRigidbody.MoveRotation(Quaternion.Lerp(grabbedRigidbody.rotation, targetRotation, Time.deltaTime * 5f));
    }
    
    private void DropObject()
    {
        if (!isGrabbing || currentGrabbedObject == null || grabbedRigidbody == null) return;
        
        // Re-enable physics
        grabbedRigidbody.useGravity = true;
        grabbedRigidbody.isKinematic = false;
        
        // Clear references
        currentGrabbedObject = null;
        grabbedRigidbody = null;
        isGrabbing = false;
    }
    
    private void ThrowObject()
    {
        if (!isGrabbing || currentGrabbedObject == null || grabbedRigidbody == null) return;
        
        // Re-enable physics
        grabbedRigidbody.useGravity = true;
        grabbedRigidbody.isKinematic = false;
        
        // Apply throw force in the direction the camera is facing
        Vector3 throwDirection = xrCamera.transform.forward;
        grabbedRigidbody.AddForce(throwDirection * throwForce, ForceMode.Impulse);
        
        // Clear references
        currentGrabbedObject = null;
        grabbedRigidbody = null;
        isGrabbing = false;
    }
    
    private void MaintainCameraPitch()
    {
        // Ensure the camera maintains its pitch even when not in mouse look mode
        if (xrCamera != null && !isMouseLookActive)
        {
            // Only update if the current camera pitch doesn't match our stored pitch
            float currentCameraPitch = xrCamera.transform.localEulerAngles.x;
            if (currentCameraPitch > 180f)
                currentCameraPitch -= 360f;
            
            if (Mathf.Abs(currentCameraPitch - currentPitch) > 0.1f)
            {
                xrCamera.transform.localRotation = Quaternion.Euler(currentPitch, xrCamera.transform.localEulerAngles.y, 0f);
            }
        }
    }
    
    private void ApplyMovement()
    {
        if (characterController == null) return;
        
        // Calculate movement direction relative to camera
        Vector3 forward = xrCamera.transform.forward;
        Vector3 right = xrCamera.transform.right;
        
        // Remove Y component for ground movement
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        
        // Calculate movement vector
        Vector3 movement = (forward * moveInput.y + right * moveInput.x) * moveSpeed * Time.deltaTime;
        
        // Apply movement
        characterController.Move(movement);
        
        // Apply turning
        if (Mathf.Abs(turnInput.x) > 0.1f)
        {
            xrOrigin.transform.Rotate(0f, turnInput.x * turnSpeed * Time.deltaTime, 0f);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugRay || xrCamera == null || mouse == null) return;
        
        // Draw interaction ray
        Ray ray = xrCamera.ScreenPointToRay(mouse.position.ReadValue());
        Gizmos.color = debugRayColor;
        Gizmos.DrawRay(ray.origin, ray.direction * interactionDistance);
        
        // Draw grab distance sphere
        if (isGrabbing)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(xrCamera.transform.position + xrCamera.transform.forward * grabDistance, 0.1f);
        }
    }
    
    private void OnGUI()
    {
        if (!enabled || !Application.isEditor || !setupComplete) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 350, 280));
        GUILayout.BeginVertical("box");
        GUILayout.Label("Desktop Controls:", GUI.skin.box);
        
        // Movement controls
        GUILayout.Label("Movement:", GUI.skin.box);
        GUILayout.Label("WASD: Move | QE: Turn | Esc: Exit Game");
        GUILayout.Label("Right Click: Toggle Mouse Look");
        
        // Interaction controls
        GUILayout.Label("Interaction:", GUI.skin.box);
        GUILayout.Label("Middle Click: Grab/Drop Objects");
        GUILayout.Label("Middle Click: Activate Buttons (Exit Cube)");
        GUILayout.Label("Right Click: Throw (if holding)");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    private void OnDestroy()
    {
        // Restore cursor state
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Re-enable locomotion providers if they exist
        if (moveProvider != null)
        {
            moveProvider.enabled = true;
        }
        if (turnProvider != null)
        {
            turnProvider.enabled = true;
        }
    }
}
#endif