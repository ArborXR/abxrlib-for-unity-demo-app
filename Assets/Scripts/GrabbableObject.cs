using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabbableObject : MonoBehaviour
{
    public GrabbableObjectManager.GrabbableObjectType type;
    public string Id { get; private set; }

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grabInteractable;
    private int _lastReleaseFrame = -1; // Track the last frame HandleReleaseEvent was called to prevent duplicate calls

    private void Awake()
    {
        Id = System.Guid.NewGuid().ToString();
        // Runtime safety check for WebGL compatibility
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            _grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.AddListener(OnGrab);
                _grabInteractable.selectExited.AddListener(OnRelease);
            }
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        HandleGrabEvent();
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        HandleReleaseEvent();
    }

    /// <summary>
    /// Public method to handle grab events - can be called from both VR and desktop input systems
    /// </summary>
    public void HandleGrabEvent()
    {
        Debug.Log("GrabbableObject: HandleGrabEvent() - Interaction Start");
        Abxr.EventInteractionStart($"place_item_{Id}");
        
        // Disable the Dropper's AbxrTarget when an item is picked up
        // Uses the target's display name "Dropper" to find and disable it
        //Abxr.TargetDisable("Dropper");
        //Abxr.EventInteractionComplete is called in LevelManager.cs->CompleteTask()
    }

    /// <summary>
    /// Handles release/drop events - re-enables the Dropper's AbxrTarget when item is dropped
    /// Note: TargetLocation.OnRelease() also re-enables it, but this ensures it happens even if
    /// the item is dropped away from any target location
    /// </summary>
    private void HandleReleaseEvent()
    {
        // Prevent duplicate calls in the same frame (can happen if listeners are registered multiple times)
        if (_lastReleaseFrame == Time.frameCount)
        {
            return;
        }
        _lastReleaseFrame = Time.frameCount;
        
        // Re-enable the Dropper's AbxrTarget when item is dropped/released
        // Uses the target's display name "Dropper" to find and enable it
        // This is a backup in case the item is dropped away from any TargetLocation
        Abxr.Event("Dropped item", transform.position);
        //Abxr.TargetEnable("Dropper");
    }

    private void OnDestroy()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer && _grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrab);
            _grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnDisable()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer && _grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrab);
            _grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnEnable()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer && _grabInteractable != null)
        {
            // Remove listeners first to prevent duplicates if OnEnable is called multiple times
            _grabInteractable.selectEntered.RemoveListener(OnGrab);
            _grabInteractable.selectExited.RemoveListener(OnRelease);
            
            // Then add listeners
            _grabInteractable.selectEntered.AddListener(OnGrab);
            _grabInteractable.selectExited.AddListener(OnRelease);
        }
    }
}
