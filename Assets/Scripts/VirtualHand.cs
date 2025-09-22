using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Creates a virtual hand for non-VR users (WebGL and Unity Editor Player).
/// The hand floats in front of the camera and acts as the grab point for objects.
/// </summary>
public class VirtualHand : MonoBehaviour
{
    [Header("Virtual Hand Settings")]
    [SerializeField] private bool createHandOnStart = true;
    [SerializeField] private Color handColor = Color.cyan;
    [SerializeField] private float handSize = 0.1f;
    
    [Header("Position Settings")]
    [SerializeField] private float distanceFromCamera = 1.5f;
    [SerializeField] private float heightOffset = -0.3f; // Negative = below center of screen
    
    [Header("Mouse Wheel Control")]
    [SerializeField] private bool enableMouseWheelControl = true;
    [SerializeField] private float wheelSensitivity = 0.5f;
    [SerializeField] private float minHeight = -1f;
    [SerializeField] private float maxHeight = 1f;
    
    [Header("Distance Control")]
    [SerializeField] private bool enableDistanceControl = true;
    [SerializeField] private float distanceSensitivity = 0.3f;
    [SerializeField] private float minDistance = 0.5f;
    [SerializeField] private float maxDistance = 3f;
    
    [Header("Keyboard Controls")]
    [SerializeField] private bool enableKeyboardControls = true;
    [SerializeField] private float keyboardSensitivity = 0.5f;
    
    private GameObject virtualHand;
    private Camera mainCamera;
    private float currentHeight;
    private float currentDistance;
    
    private void Start()
    {
        // Only create virtual hand if not in VR
        if (!IsVRMode() && createHandOnStart)
        {
            CreateVirtualHand();
        }
    }
    
    private void Update()
    {
        if (virtualHand != null && mainCamera != null)
        {
            UpdateHandPosition();
            
            if (enableMouseWheelControl)
            {
                HandleMouseWheel();
            }
            
            if (enableKeyboardControls)
            {
                HandleKeyboardInput();
            }
        }
    }
    
    private bool IsVRMode()
    {
        // Check if we're in VR mode
        return UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.isDeviceActive;
    }
    
    private void CreateVirtualHand()
    {
        // Find the main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("[VirtualHand] No camera found in scene!");
            return;
        }
        
        // Create the virtual hand
        virtualHand = new GameObject("VirtualHand");
        
        // Create a simple sphere for the hand
        GameObject handModel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        handModel.name = "HandModel";
        handModel.transform.SetParent(virtualHand.transform);
        handModel.transform.localPosition = Vector3.zero;
        handModel.transform.localRotation = Quaternion.identity;
        handModel.transform.localScale = Vector3.one * handSize;
        
        // Apply material to make it clearly visible
        Material handMaterial = new Material(Shader.Find("Standard"));
        handMaterial.color = handColor;
        handMaterial.SetFloat("_Metallic", 0.2f);
        handMaterial.SetFloat("_Smoothness", 0.8f);
        handMaterial.SetFloat("_Emission", 0.1f); // Slight glow
        
        // Apply material to all parts
        Renderer[] renderers = virtualHand.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material = handMaterial;
        }
        
        // Make the virtual hand a trigger so it can pass through objects
        Collider[] colliders = virtualHand.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.isTrigger = true;
        }
        
        // Initialize height and distance
        currentHeight = heightOffset;
        currentDistance = distanceFromCamera;
        
        Debug.Log("[VirtualHand] Virtual hand created for non-VR mode!");
    }
    
    private void UpdateHandPosition()
    {
        if (virtualHand == null || mainCamera == null) return;
        
        // Position the hand in front of the camera
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        Vector3 cameraUp = mainCamera.transform.up;
        
        // Calculate position in front of camera
        Vector3 targetPosition = mainCamera.transform.position + 
                                cameraForward * currentDistance + 
                                cameraUp * currentHeight;
        
        virtualHand.transform.position = targetPosition;
        
        // Make the hand face the same direction as the camera
        virtualHand.transform.rotation = mainCamera.transform.rotation;
    }
    
    private void HandleMouseWheel()
    {
        // Use the new Input System for mouse wheel
        float scroll = Mouse.current.scroll.ReadValue().y / 120f; // Normalize scroll value
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // Check if Ctrl is held for distance control (more reliable than Shift)
            bool isCtrlHeld = Keyboard.current != null && 
                             (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
            
            // Debug logging
            Debug.Log($"[VirtualHand] Scroll: {scroll:F2}, Ctrl held: {isCtrlHeld}, Distance control enabled: {enableDistanceControl}");
            
            if (isCtrlHeld && enableDistanceControl)
            {
                // Ctrl + scroll: adjust distance (closer/further)
                currentDistance += scroll * distanceSensitivity;
                currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
                
                Debug.Log($"[VirtualHand] Hand distance adjusted to: {currentDistance:F2}");
            }
            else if (enableMouseWheelControl)
            {
                // Normal scroll: adjust height (up/down)
                currentHeight += scroll * wheelSensitivity;
                currentHeight = Mathf.Clamp(currentHeight, minHeight, maxHeight);
                
                Debug.Log($"[VirtualHand] Hand height adjusted to: {currentHeight:F2}");
            }
        }
    }
    
    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null) return;
        
        // Z/X keys for distance control (closer/further)
        if (Keyboard.current.zKey.isPressed)
        {
            currentDistance -= keyboardSensitivity * Time.deltaTime;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            Debug.Log($"[VirtualHand] Hand distance adjusted to: {currentDistance:F2} (Z key)");
        }
        
        if (Keyboard.current.xKey.isPressed)
        {
            currentDistance += keyboardSensitivity * Time.deltaTime;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            Debug.Log($"[VirtualHand] Hand distance adjusted to: {currentDistance:F2} (X key)");
        }
        
        // R/F keys for height control (up/down)
        if (Keyboard.current.rKey.isPressed)
        {
            currentHeight += keyboardSensitivity * Time.deltaTime;
            currentHeight = Mathf.Clamp(currentHeight, minHeight, maxHeight);
            Debug.Log($"[VirtualHand] Hand height adjusted to: {currentHeight:F2} (R key)");
        }
        
        if (Keyboard.current.fKey.isPressed)
        {
            currentHeight -= keyboardSensitivity * Time.deltaTime;
            currentHeight = Mathf.Clamp(currentHeight, minHeight, maxHeight);
            Debug.Log($"[VirtualHand] Hand height adjusted to: {currentHeight:F2} (F key)");
        }
    }
    
    // Public methods for external control
    public void SetHandColor(Color color)
    {
        handColor = color;
        if (virtualHand != null)
        {
            Renderer[] renderers = virtualHand.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material.color = color;
            }
        }
    }
    
    public void SetHandSize(float size)
    {
        handSize = size;
        // Note: This would require recreating the hand model to take effect
        Debug.Log($"[VirtualHand] Hand size set to: {size}");
    }
    
    public void SetDistanceFromCamera(float distance)
    {
        distanceFromCamera = distance;
        Debug.Log($"[VirtualHand] Distance from camera set to: {distance}");
    }
    
    public GameObject GetVirtualHand()
    {
        return virtualHand;
    }
    
    public bool IsHandActive()
    {
        return virtualHand != null && virtualHand.activeInHierarchy;
    }
    
    public Vector3 GetHandPosition()
    {
        if (virtualHand != null)
        {
            return virtualHand.transform.position;
        }
        return Vector3.zero;
    }
    
    public Transform GetHandTransform()
    {
        return virtualHand?.transform;
    }
}
