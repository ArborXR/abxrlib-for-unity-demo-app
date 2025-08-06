using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

/// <summary>
/// Provides keyboard and mouse movement controls for the Unity editor when VR controllers are not available.
/// This script directly interfaces with the locomotion providers to enable keyboard/mouse input.
/// </summary>
public class KeyboardMovementProvider : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private bool enableKeyboardMovement = true;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float mouseSensitivity = 0.5f;
    [SerializeField] private float mouseTurnSensitivity = 0.5f;
    
    [Header("Key Bindings")]
//    [SerializeField] private KeyCode moveForward = KeyCode.W;
//    [SerializeField] private KeyCode moveBackward = KeyCode.S;
//    [SerializeField] private KeyCode moveLeft = KeyCode.A;
//    [SerializeField] private KeyCode moveRight = KeyCode.D;
//    [SerializeField] private KeyCode turnLeft = KeyCode.Q;
//    [SerializeField] private KeyCode turnRight = KeyCode.E;
    
    private XROrigin xrOrigin;
    private Camera xrCamera;
    private CharacterController characterController;
    private ActionBasedContinuousMoveProvider moveProvider;
    private ActionBasedContinuousTurnProvider turnProvider;
    
    // Input System references
    private Keyboard keyboard;
    private Mouse mouse;
    
    private Vector2 moveInput;
    private Vector2 turnInput;
    private bool isMouseLookActive = false;
    private bool setupComplete = false;
    private float currentPitch = 0f;
    
    private void Start()
    {
        // Only enable in editor
        if (!Application.isEditor || !enableKeyboardMovement)
        {
            enabled = false;
            return;
        }
        
        SetupComponents();
    }
    
    private void SetupComponents()
    {
        // Find XR Origin
        xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogWarning("KeyboardMovementProvider: No XROrigin found in scene");
            enabled = false;
            return;
        }
        
        // Get camera
        xrCamera = xrOrigin.Camera;
        if (xrCamera == null)
        {
            Debug.LogWarning("KeyboardMovementProvider: No camera found on XROrigin");
            enabled = false;
            return;
        }
        
        // Get character controller
        characterController = xrOrigin.GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogWarning("KeyboardMovementProvider: No CharacterController found on XROrigin");
            enabled = false;
            return;
        }
        
        // Setup Input System
        keyboard = Keyboard.current;
        mouse = Mouse.current;
        
        if (keyboard == null)
        {
            Debug.LogWarning("KeyboardMovementProvider: No keyboard detected!");
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
            //Debug.Log("KeyboardMovementProvider: Disabled ActionBasedContinuousMoveProvider");
        }
        if (turnProvider != null)
        {
            turnProvider.enabled = false;
            //Debug.Log("KeyboardMovementProvider: Disabled ActionBasedContinuousTurnProvider");
        }
        
        setupComplete = true;
        //Debug.Log("KeyboardMovementProvider: Setup complete - Keyboard controls enabled");
    }
    
    private void Update()
    {
        if (!enabled || !setupComplete) return;
        
        HandleKeyboardInput();
        HandleMouseInput();
        ApplyMovement();
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
    
    private void OnGUI()
    {
        return;
        //if (!enabled || !Application.isEditor || !setupComplete) return;
        
        // Display controls help
        //GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        //GUILayout.BeginVertical("box");
        //GUILayout.Label("Editor Controls:", GUI.skin.box);
        //GUILayout.Label($"Move: {moveForward}/{moveBackward}/{moveLeft}/{moveRight}");
        //GUILayout.Label($"Turn: {turnLeft}/{turnRight}");
        //GUILayout.Label("Mouse Look: Right Click + Mouse");
        //GUILayout.Label("Toggle Mouse Look: Right Click");
        //GUILayout.Label("Grab Objects: Spacebar");
        //GUILayout.Label($"Move Speed: {moveSpeed}");
        //GUILayout.Label($"Turn Speed: {turnSpeed}");
        //GUILayout.Label($"Mouse Sensitivity: {mouseSensitivity}");
        //GUILayout.Label($"Mouse Turn Sensitivity: {mouseTurnSensitivity}");
        //GUILayout.Label($"Keyboard: {(keyboard != null ? "Connected" : "Not Found")}");
        //GUILayout.Label($"Mouse: {(mouse != null ? "Connected" : "Not Found")}");
        //GUILayout.EndVertical();
        //GUILayout.EndArea();
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