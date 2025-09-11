using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Provides comprehensive desktop input controls for the Unity editor when VR controllers are not available.
/// Combines keyboard movement, mouse look, and mouse-based object interaction into a single controller.
/// </summary>
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
    private ContinuousMoveProvider moveProvider;
    private ContinuousTurnProvider turnProvider;
    
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
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable currentGrabbedObject;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable currentSimpleInteractable;
    private GameObject currentGrabbedGameObject; // For WebGL compatibility
    private Rigidbody grabbedRigidbody;
    private Vector3 grabOffset;
    private bool isGrabbing = false;
    
    private void Start()
    {
        // Enable desktop controls only when VR headset is not active
        bool shouldEnable = enableDesktopControls;
        
        // Check VR status (with WebGL safety)
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            shouldEnable = shouldEnable && !IsVRHeadsetActive();
        }
        
        if (!shouldEnable)
        {
            enabled = false;
            return;
        }
        
        SetupComponents();
        SetupExitCube();
    }
    
    /// <summary>
    /// Checks if a VR headset is currently active and being used.
    /// Returns true if VR is active, false if we should use desktop controls.
    /// </summary>
    private bool IsVRHeadsetActive()
    {
        // Runtime safety check for WebGL
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            return false; // No VR support in WebGL
        }

        try
        {
            // Check if XR is enabled and a device is active
            if (XRSettings.enabled && XRSettings.isDeviceActive)
            {
                return true;
            }
            
            // Additional check for XR Display subsystems (newer XR SDK)
            var displaySubsystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displaySubsystems);
            
            foreach (var display in displaySubsystems)
            {
                if (display.running)
                {
                    return true;
                }
            }
        }
        catch (System.Exception)
        {
            // XR systems may not be available - safely return false
            return false;
        }
        
        return false;
    }
    
    private void SetupComponents()
    {
        // Platform-specific component setup
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // WebGL setup - find camera without XR Origin dependency
            xrCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (xrCamera == null)
            {
                Debug.LogWarning("DesktopInputController: No camera found in WebGL scene");
                enabled = false;
                return;
            }
            
            // Look for character controller on camera or parent objects
            characterController = xrCamera.GetComponent<CharacterController>() ?? xrCamera.GetComponentInParent<CharacterController>();
            if (characterController == null)
            {
                Debug.LogWarning("DesktopInputController: No CharacterController found in WebGL scene");
                enabled = false;
                return;
            }
        }
        else
        {
            // VR/Desktop setup with XR Origin
            xrOrigin = FindFirstObjectByType<XROrigin>();
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
            
            // Get locomotion providers (optional - we'll work without them if needed)
            moveProvider = xrOrigin.GetComponent<ContinuousMoveProvider>();
            turnProvider = xrOrigin.GetComponent<ContinuousTurnProvider>();
            
            // Disable the original locomotion providers to prevent conflicts
            if (moveProvider != null)
            {
                moveProvider.enabled = false;
            }
            if (turnProvider != null)
            {
                turnProvider.enabled = false;
            }
        }
        
        // Setup Input System (common for both platforms)
        keyboard = Keyboard.current;
        mouse = Mouse.current;
        
        if (keyboard == null)
        {
            Debug.LogWarning("DesktopInputController: No keyboard detected!");
            enabled = false;
            return;
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
            UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable exitInteractable = exitCube.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
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
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // WebGL: Use GrabbableObject components instead of XR
            TryGrabObjectWebGL();
            return;
        }

        // Track closest object of any type
        float closestDistance = float.MaxValue;
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable closestGrabbable = null;
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable closestSimple = null;
        Vector3 closestHitPoint = Vector3.zero;

        // Find all grabbable objects in the scene
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable[] allGrabbables = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None);
        
        // Check grabbable objects
        foreach (var interactable in allGrabbables)
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
                    closestGrabbable = interactable;
                    closestSimple = null; // Clear the other type
                    closestDistance = distance;
                    closestHitPoint = interactable.transform.position;
                }
            }
        }
        
        // Check simple interactables (buttons)
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable[] simpleInteractables = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>(FindObjectsSortMode.None);
        foreach (var simpleInteractable in simpleInteractables)
        {
            float distance = Vector3.Distance(xrCamera.transform.position, simpleInteractable.transform.position);
            
            if (distance <= interactionDistance && distance < closestDistance)
            {
                Vector3 directionToObject = (simpleInteractable.transform.position - xrCamera.transform.position).normalized;
                float angle = Vector3.Angle(xrCamera.transform.forward, directionToObject);
                
                if (angle <= 60f)
                {
                    closestSimple = simpleInteractable;
                    closestGrabbable = null; // Clear the other type
                    closestDistance = distance;
                    closestHitPoint = simpleInteractable.transform.position;
                }
            }
        }
        
        // Act on the closest interactable found (regardless of type)
        if (closestSimple != null)
        {
            ActivateSimpleInteractable(closestSimple);
        }
        else if (closestGrabbable != null)
        {
            GrabObject(closestGrabbable, closestHitPoint);
        }
    }
    
    private void TryGrabObjectWebGL()
    {
        // Find all GrabbableObject components in the scene
        GrabbableObject[] grabbableObjects = FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);
        
        GameObject closestObject = null;
        float closestDistance = float.MaxValue;
        Vector3 closestHitPoint = Vector3.zero;
        
        // Find the closest grabbable object within interaction distance
        foreach (var grabbableObj in grabbableObjects)
        {
            // Skip objects that don't have a Rigidbody (can't be grabbed)
            if (grabbableObj.GetComponent<Rigidbody>() == null) continue;
            
            // Calculate distance from camera to object
            float distance = Vector3.Distance(xrCamera.transform.position, grabbableObj.transform.position);
            
            if (distance <= interactionDistance && distance < closestDistance)
            {
                // Check if the object is in front of the camera (within a reasonable angle)
                Vector3 directionToObject = (grabbableObj.transform.position - xrCamera.transform.position).normalized;
                float angle = Vector3.Angle(xrCamera.transform.forward, directionToObject);
                
                // Only consider objects within a 60-degree cone in front of the camera
                if (angle <= 60f)
                {
                    closestObject = grabbableObj.gameObject;
                    closestDistance = distance;
                    closestHitPoint = grabbableObj.transform.position;
                }
            }
        }
        
        if (closestObject != null)
        {
            GrabObjectWebGL(closestObject, closestHitPoint);
        }
    }
    
    private void GrabObjectWebGL(GameObject obj, Vector3 hitPoint)
    {
        currentGrabbedGameObject = obj;
        grabbedRigidbody = obj.GetComponent<Rigidbody>();
        
        if (grabbedRigidbody != null)
        {
            // Calculate offset from hit point to object center
            grabOffset = obj.transform.position - hitPoint;
            
            // Disable gravity while grabbed
            grabbedRigidbody.useGravity = false;
            grabbedRigidbody.isKinematic = true;
            
            isGrabbing = true;
            
            //Debug.Log($"WebGL grabbed object: {obj.name}");
            
            // Call GrabbableObject's grab event handler for consistency with VR behavior
            GrabbableObject grabbableComponent = obj.GetComponent<GrabbableObject>();
            if (grabbableComponent != null)
            {
                grabbableComponent.HandleGrabEvent();
            }
        }
    }
    
    private void GrabObject(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable, Vector3 hitPoint)
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
            
            // Call GrabbableObject's grab event handler for consistency with VR behavior
            GrabbableObject grabbableComponent = interactable.GetComponent<GrabbableObject>();
            if (grabbableComponent != null)
            {
                grabbableComponent.HandleGrabEvent();
            }
        }
    }
    
    private void ActivateSimpleInteractable(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable simpleInteractable)
    {
        try
        {
            currentSimpleInteractable = simpleInteractable;
            
            // Check if the object has our custom script and call it directly if needed
            var toggleButton = simpleInteractable.GetComponent<ToggleActionButton>();
            var resetButton = simpleInteractable.GetComponent<ResetButton>();
            var exitButton = simpleInteractable.GetComponent<ExitButton>();
            var reAuthButton = simpleInteractable.GetComponent<ReAuthenticateButton>();
            
            // Try the standard XR event system first
            var selectEnterEventArgs = new SelectEnterEventArgs();
            simpleInteractable.selectEntered.Invoke(selectEnterEventArgs);
            
            // Direct method calls as backup (more reliable than reflection)
            if (toggleButton != null)
            {
                Debug.Log("DesktopInputController: About to call ToggleActionButton.TriggerAction()");
                toggleButton.TriggerAction();
                Debug.Log("DesktopInputController: Successfully called ToggleActionButton.TriggerAction()");
            }
            else if (resetButton != null)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else if (exitButton != null)
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
            else if (reAuthButton != null)
            {
                Abxr.ReAuthenticate();
            }
            
            // Clear the reference after activation
            currentSimpleInteractable = null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DesktopInputController: Exception in ActivateSimpleInteractable: {ex.Message}\n{ex.StackTrace}");
            currentSimpleInteractable = null;
        }
    }
    
    private void UpdateGrabbedObject()
    {
        // Check if we're grabbing something (either XR or WebGL)
        bool hasXRObject = currentGrabbedObject != null;
        bool hasWebGLObject = currentGrabbedGameObject != null;
        
        if (!isGrabbing || (!hasXRObject && !hasWebGLObject) || grabbedRigidbody == null) return;
        
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
        // Check if we're grabbing something (either XR or WebGL)
        bool hasXRObject = currentGrabbedObject != null;
        bool hasWebGLObject = currentGrabbedGameObject != null;
        
        if (!isGrabbing || (!hasXRObject && !hasWebGLObject) || grabbedRigidbody == null) return;
        
        // Re-enable physics
        grabbedRigidbody.useGravity = true;
        grabbedRigidbody.isKinematic = false;
        
        // Clear references
        currentGrabbedObject = null;
        currentGrabbedGameObject = null;
        grabbedRigidbody = null;
        isGrabbing = false;
        
        //Debug.Log("Dropped object");
    }
    
    private void ThrowObject()
    {
        // Check if we're grabbing something (either XR or WebGL)
        bool hasXRObject = currentGrabbedObject != null;
        bool hasWebGLObject = currentGrabbedGameObject != null;
        
        if (!isGrabbing || (!hasXRObject && !hasWebGLObject) || grabbedRigidbody == null) return;
        
        // Re-enable physics
        grabbedRigidbody.useGravity = true;
        grabbedRigidbody.isKinematic = false;
        
        // Apply throw force in the direction the camera is facing
        Vector3 throwDirection = xrCamera.transform.forward;
        grabbedRigidbody.AddForce(throwDirection * throwForce, ForceMode.Impulse);
        
        // Clear references
        currentGrabbedObject = null;
        currentGrabbedGameObject = null;
        grabbedRigidbody = null;
        isGrabbing = false;
        
        //Debug.Log("Threw object");
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
        if (!enabled || !setupComplete) return;
        
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
        
        // Re-enable locomotion providers if they exist (VR/Desktop only)
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
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
}