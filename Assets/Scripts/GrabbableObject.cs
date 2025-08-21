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
        _grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnGrab);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        //Abxr.EventInteractionStart($"place_item_{Id}");
        //Abxr.EventInteractionComplete is called in LevelManager.cs->CompleteTask()
    }

    private void OnDestroy()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrab);
        }
    }

    private void OnDisable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrab);
        }
    }

    private void OnEnable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnGrab);
        }
    }
}
