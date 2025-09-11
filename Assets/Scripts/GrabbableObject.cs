using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabbableObject : MonoBehaviour
{
    public GrabbableObjectManager.GrabbableObjectType type;
    public string Id { get; private set; }

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grabInteractable;

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
            }
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        HandleGrabEvent();
    }

    /// <summary>
    /// Public method to handle grab events - can be called from both VR and desktop input systems
    /// </summary>
    public void HandleGrabEvent()
    {
        Debug.Log("AbxrLib - Interaction Start");
        Abxr.EventInteractionStart($"place_item_{Id}");
        //Abxr.EventInteractionComplete is called in LevelManager.cs->CompleteTask()

        // Just for testing
        Abxr.EventInteractionComplete($"place_item_{Id}", "True", "Correct spot", Abxr.InteractionType.Bool);
        Debug.Log("AbxrLib - About to send assessment complete");
        Abxr.EventAssessmentComplete("stocking_training_unit_1", "88", result: Abxr.ResultOptions.Pass);
        Debug.Log("AbxrLib - Assessment complete sent, waiting before exit");
        
        // Wait a moment for the assessment to be sent before exiting
        StartCoroutine(ExitAfterDelay());

    }

    private System.Collections.IEnumerator ExitAfterDelay()
    {
        // Wait 2 seconds to allow the assessment event to be sent
        yield return new WaitForSeconds(2f);
        
        Debug.Log("AbxrLib - Exiting application");
        // Exit the app and return to launcher
        Application.Quit();
    }

    private void OnDestroy()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer && _grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrab);
        }
    }

    private void OnDisable()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer && _grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrab);
        }
    }

    private void OnEnable()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer && _grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnGrab);
        }
    }
}
