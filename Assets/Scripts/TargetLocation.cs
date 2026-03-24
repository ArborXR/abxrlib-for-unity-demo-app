using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TargetLocation : MonoBehaviour
{
    public GrabbableObjectManager.GrabbableObjectType targetType;
    public double positionError = .2;
    // 0.0000004 = 1 degree
    public double rotationError = 0.000004; // 20 Degrees
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
        completionData.positionDistance = Vector3.Distance(collider.transform.position, this.transform.position);
        completionData.rotationDistance = CompareQuaternions(collider.transform.rotation, this.transform.rotation);
        completionData.validPlacement = completionData.positionDistance < positionError && completionData.rotationDistance < rotationError;
        completionData.usedType = collider.gameObject.GetComponent<GrabbableObject>().type;
        completionData.usedObject = collider.gameObject;
        completionData.usedTarget = this.gameObject;
    }

    public void OnTriggerExit(Collider collider)
    {
        completionData.validPlacement = false;
    }
    public void OnRelease()
    {
        string jsonData = JsonUtility.ToJson(completionData);
        Abxr.LogInfo(jsonData);
        //Debug.Log(jsonData);
        if (!completionData.validPlacement || OnCompleted == null) return;
        isCompleted = true;
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
