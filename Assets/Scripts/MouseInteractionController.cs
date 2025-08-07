using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// Provides mouse-based interaction controls for the Unity editor.
/// Allows grabbing and dropping objects using mouse clicks, similar to VR controllers.
/// </summary>
public class MouseInteractionController : MonoBehaviour
{
    [Header("Mouse Interaction Settings")]
    [SerializeField] private float interactionDistance = 20f;
    [SerializeField] private LayerMask interactableLayers = -1;
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private Color debugRayColor = Color.red;
    
    [Header("Grab Settings")]
    [SerializeField] private float grabDistance = 2f;
    [SerializeField] private float throwForce = 5f;
    
    private Camera playerCamera;
    private XROrigin xrOrigin;
    private XRGrabInteractable currentGrabbedObject;
    private XRSimpleInteractable currentSimpleInteractable;
    private Rigidbody grabbedRigidbody;
    private Vector3 grabOffset;
    private bool isGrabbing = false;
    
    // Input System
    private Mouse mouse;
    private Keyboard keyboard;
    
    private void Start()
    {
        // Enable mouse controls only when VR headset is not active
        bool shouldEnable = !IsVRHeadsetActive();
        
        if (!shouldEnable)
        {
            enabled = false;
            return;
        }
        
        SetupMouseInteraction();
    }
    
    /// <summary>
    /// Checks if a VR headset is currently active and being used.
    /// Returns true if VR is active, false if we should use desktop controls.
    /// </summary>
    private bool IsVRHeadsetActive()
    {
        // Check if XR is enabled and a device is active
        if (XRSettings.enabled && XRSettings.isDeviceActive)
        {
            return true;
        }
        
        // Additional check for XR Display subsystems (newer XR SDK)
        var displaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances(displaySubsystems);
        
        foreach (var display in displaySubsystems)
        {
            if (display.running)
            {
                return true;
            }
        }
        
        return false;
    }
    
    private void SetupMouseInteraction()
    {
        //Debug.Log("MouseInteractionController: Setting up mouse interaction...");
        
        // Find XR Origin and camera
        xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("MouseInteractionController: No XROrigin found!");
            return;
        }
        
        playerCamera = xrOrigin.Camera;
        if (playerCamera == null)
        {
            Debug.LogError("MouseInteractionController: No camera found!");
            return;
        }
        
        // Setup Input System
        mouse = Mouse.current;
        keyboard = Keyboard.current;
        
        if (mouse == null)
        {
            Debug.LogError("MouseInteractionController: No mouse detected!");
            return;
        }
        
        //Debug.Log("MouseInteractionController: Setup complete! Left-click to grab/drop, Right-click for mouse look");
        
        // Debug: Look specifically for ExitCube
        GameObject exitCube = GameObject.Find("ExitCube");
        if (exitCube != null)
        {
            //Debug.Log($"MouseInteractionController: Found ExitCube GameObject: {exitCube.name}");
            XRSimpleInteractable exitInteractable = exitCube.GetComponent<XRSimpleInteractable>();
            ExitButton exitButton = exitCube.GetComponent<ExitButton>();
            //Debug.Log($"MouseInteractionController: ExitCube has XRSimpleInteractable: {exitInteractable != null}");
            //Debug.Log($"MouseInteractionController: ExitCube has ExitButton: {exitButton != null}");
            //Debug.Log($"MouseInteractionController: ExitCube position: {exitCube.transform.position}");
            
            // Only add ExitButton if it doesn't exist and there's no XRSimpleInteractable
            // This prevents the collider conflict issue
            if (exitButton == null && exitInteractable == null)
            {
                //Debug.Log("MouseInteractionController: Adding missing ExitButton to ExitCube");
                exitButton = exitCube.AddComponent<ExitButton>();
            }
            
            // Ensure the ExitCube has a collider for interaction
            Collider exitCollider = exitCube.GetComponent<Collider>();
            if (exitCollider == null)
            {
                //Debug.Log("MouseInteractionController: Adding missing Collider to ExitCube");
                BoxCollider boxCollider = exitCube.AddComponent<BoxCollider>();
                boxCollider.size = Vector3.one; // Default size
                boxCollider.isTrigger = false; // Not a trigger
            }
        }
        else
        {
            //Debug.LogWarning("MouseInteractionController: ExitCube GameObject not found in scene!");
        }
    }
    
    private void Update()
    {
        if (playerCamera == null) return;
        
        HandleMouseInteraction();
        UpdateGrabbedObject();
    }
    
    private void HandleMouseInteraction()
    {
        // Only handle mouse interaction when not in mouse look mode
        // We'll use a different key for grabbing to avoid conflicts
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
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
        if (mouse.rightButton.wasPressedThisFrame && isGrabbing)
        {
            // Check if we're in mouse look mode (this will be handled by KeyboardMovementProvider)
            // For now, we'll just throw the object
            ThrowObject();
        }
        
        // Debug: Add escape key to exit game for testing
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            //Debug.Log("MouseInteractionController: Escape key pressed - exiting game");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        

    }
    
    private void TryGrabObject()
    {
        Ray ray = playerCamera.ScreenPointToRay(mouse.position.ReadValue());
        RaycastHit hit;
        
        // Debug: Check what objects are in the scene (only log once per frame)
        XRGrabInteractable[] allInteractables = FindObjectsOfType<XRGrabInteractable>();
        XRSimpleInteractable[] allSimpleInteractables = FindObjectsOfType<XRSimpleInteractable>();
        
        if (allInteractables.Length > 0)
        {
            //Debug.Log($"MouseInteractionController: Found {allInteractables.Length} XRGrabInteractable objects - can grab these!");
        }
        else
        {
            //Debug.Log("MouseInteractionController: No XRGrabInteractable objects found - nothing to grab");
        }
        
        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayers))
        {
            //Debug.Log($"MouseInteractionController: Hit object: {hit.collider.name} at distance: {hit.distance}");
            
            // Check for XRGrabInteractable first
            XRGrabInteractable interactable = hit.collider.GetComponent<XRGrabInteractable>();
            if (interactable != null)
            {
                //Debug.Log($"MouseInteractionController: Found XRGrabInteractable on {hit.collider.name}");
                GrabObject(interactable, hit.point);
            }
            else
            {
                // Check for XRSimpleInteractable (like the exit cube)
                XRSimpleInteractable simpleInteractable = hit.collider.GetComponent<XRSimpleInteractable>();
                if (simpleInteractable != null)
                {
                    //Debug.Log($"MouseInteractionController: Found XRSimpleInteractable on {hit.collider.name}");
                    ActivateSimpleInteractable(simpleInteractable);
                }
                else
                {
                    //Debug.LogWarning($"MouseInteractionController: No interactable component found on {hit.collider.name}");
                }
            }
        }
        else
        {
            //Debug.LogWarning($"MouseInteractionController: No object hit within {interactionDistance} units");
            
            // Debug: Try a longer raycast to see if objects are further away
            if (Physics.Raycast(ray, out hit, 50f, interactableLayers))
            {
                //Debug.LogWarning($"MouseInteractionController: Found object {hit.collider.name} at {hit.distance} units (too far)");
            }
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
            //Debug.Log($"MouseInteractionController: Grabbed {interactable.name}");
        }
    }
    
    private void ActivateSimpleInteractable(XRSimpleInteractable simpleInteractable)
    {
        currentSimpleInteractable = simpleInteractable;
        
        // Simulate the interaction by calling the selectEntered event
        // This will trigger the ExitButton's OnSelect method
        var selectEnterEventArgs = new SelectEnterEventArgs();
        simpleInteractable.selectEntered.Invoke(selectEnterEventArgs);
        
        //Debug.Log($"MouseInteractionController: Activated simple interactable {simpleInteractable.name}");
        
        // Clear the reference after activation
        currentSimpleInteractable = null;
    }
    

    
    private void UpdateGrabbedObject()
    {
        if (!isGrabbing || currentGrabbedObject == null || grabbedRigidbody == null) return;
        
        // Position the object in front of the camera
        Vector3 targetPosition = playerCamera.transform.position + 
                               playerCamera.transform.forward * grabDistance + 
                               grabOffset;
        
        // Smoothly move the object to the target position
        grabbedRigidbody.MovePosition(Vector3.Lerp(grabbedRigidbody.position, targetPosition, Time.deltaTime * 10f));
        
        // Optional: Make the object follow camera rotation
        Quaternion targetRotation = playerCamera.transform.rotation;
        grabbedRigidbody.MoveRotation(Quaternion.Lerp(grabbedRigidbody.rotation, targetRotation, Time.deltaTime * 5f));
    }
    
    private void DropObject()
    {
        if (!isGrabbing || currentGrabbedObject == null || grabbedRigidbody == null) return;
        
        // Re-enable physics
        grabbedRigidbody.useGravity = true;
        grabbedRigidbody.isKinematic = false;
        
        //Debug.Log($"MouseInteractionController: Dropped {currentGrabbedObject.name}");
        
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
        Vector3 throwDirection = playerCamera.transform.forward;
        grabbedRigidbody.AddForce(throwDirection * throwForce, ForceMode.Impulse);
        
        //Debug.Log($"MouseInteractionController: Threw {currentGrabbedObject.name}");
        
        // Clear references
        currentGrabbedObject = null;
        grabbedRigidbody = null;
        isGrabbing = false;
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugRay || playerCamera == null || mouse == null) return;
        
        // Draw interaction ray
        Ray ray = playerCamera.ScreenPointToRay(mouse.position.ReadValue());
        Gizmos.color = debugRayColor;
        Gizmos.DrawRay(ray.origin, ray.direction * interactionDistance);
        
        // Draw grab distance sphere
        if (isGrabbing)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerCamera.transform.position + playerCamera.transform.forward * grabDistance, 0.1f);
        }
    }
    
    private void OnGUI()
    {
        return;
        //if (!Application.isEditor) return;
        
        //GUILayout.BeginArea(new Rect(10, 220, 300, 150));
        //GUILayout.BeginVertical("box");
        //GUILayout.Label("Mouse Interaction Controls:", GUI.skin.box);
        //GUILayout.Label("Spacebar: Grab/Drop Objects");
        //GUILayout.Label("Spacebar: Activate Buttons (Exit Cube)");
        //GUILayout.Label("Right Click: Throw (if holding)");
        //GUILayout.Label("Right Click: Toggle Mouse Look");
        //GUILayout.Label("Escape: Exit Game (Debug)");
        //GUILayout.Label($"Interaction Distance: {interactionDistance}");
        //GUILayout.Label($"Grab Distance: {grabDistance}");
        //GUILayout.Label($"Currently Grabbing: {(isGrabbing ? currentGrabbedObject?.name : "Nothing")}");
        //GUILayout.EndVertical();
        //GUILayout.EndArea();
    }
} 