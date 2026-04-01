using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TargetLocation : MonoBehaviour
{
    public GrabbableObjectManager.GrabbableObjectType targetType;
    public double positionError = .2;
    /// <summary>Max rotation mismatch (same metric as <see cref="CompareQuaternions"/>). Ignored when <see cref="requireRotationMatch"/> is false.</summary>
    public double rotationError = 0.15;
    [Tooltip("If off, only position must match the slot (recommended for desktop/mouse grab where the object follows camera rotation).")]
    public bool requireRotationMatch = false;
    public UnityEvent<CompletionData> OnCompleted;

    public struct CompletionData
    {
        // This is used as a 1 frame delay to avoid errors from some of the xr libraries it's not optimal but...
        public bool completed; // If this value is false outside of this script there is probably a problem!!!
        public bool validPlacement;
        public GameObject usedTarget;
        public GameObject usedObject;
        public GrabbableObjectManager.GrabbableObjectType targetType;
        public GrabbableObjectManager.GrabbableObjectType usedType;
        public double positionDistance;
        public double rotationDistance;
    }

    private CompletionData completionData;
    private bool isCompleted = false;

    public void Start()
    {
        completionData.targetType = this.targetType;
    }
    
    public void OnTriggerStay(Collider collider)
    {
        // Is Valid Collision
        if (collider.gameObject.GetComponent<GrabbableObject>() == null) return;
        ApplyPlacementSample(collider.transform, collider.gameObject.GetComponent<GrabbableObject>());
    }

    public void OnTriggerExit(Collider collider)
    {
        completionData.validPlacement = false;
    }

    /// <summary>
    /// Called when a grabbable is released (XR selectExited or WebGL drop). Evaluates placement from the
    /// object transform at release time so we are not dependent on OnTriggerStay/Exit ordering.
    /// </summary>
    public void OnGrabbableReleased(GrabbableObject grabbable)
    {
        if (grabbable == null || OnCompleted == null) return;
        // Stale listeners can still hold references to a TargetLocation removed after a successful placement.
        if (!this || !isActiveAndEnabled) return;

        Collider triggerCol = GetComponent<Collider>();
        Collider grabbableCol = grabbable.GetComponent<Collider>();
        if (triggerCol == null || grabbableCol == null) return;
        if (!triggerCol.bounds.Intersects(grabbableCol.bounds)) return;

        ApplyPlacementSample(grabbable.transform, grabbable);

        string jsonData = JsonUtility.ToJson(completionData);
        Abxr.LogInfo(jsonData);

        if (!completionData.validPlacement) return;
        isCompleted = true;
    }

    private void ApplyPlacementSample(Transform sample, GrabbableObject grabbable)
    {
        completionData.positionDistance = Vector3.Distance(sample.position, transform.position);
        completionData.rotationDistance = CompareQuaternions(sample.rotation, transform.rotation);
        bool rotationOk = !requireRotationMatch || completionData.rotationDistance < rotationError;
        completionData.validPlacement = completionData.positionDistance < positionError && rotationOk;
        completionData.usedType = grabbable.type;
        completionData.usedObject = grabbable.gameObject;
        completionData.usedTarget = gameObject;
    }

    public void Update()
    {
        if (isCompleted)
        {
            OnCompleted.Invoke(completionData);
            isCompleted = false;
        }
    }

    private double CompareQuaternions(Quaternion a, Quaternion b)
    {
        return 1 - Mathf.Abs(Quaternion.Dot(a, b));
    }

    public void ResetState()
    {
        completionData = new CompletionData();
        completionData.targetType = this.targetType;
    }
}
