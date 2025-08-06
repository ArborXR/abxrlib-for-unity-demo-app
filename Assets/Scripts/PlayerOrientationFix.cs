using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

/// <summary>
/// Fixes the player orientation issue where the player starts facing backwards.
/// This script rotates the XR Origin 180 degrees on start to face the correct direction.
/// </summary>
public class PlayerOrientationFix : MonoBehaviour
{
    [Header("Orientation Settings")]
    [Tooltip("The Y-axis rotation to apply to fix the orientation (default: 180 for backwards fix)")]
    public float rotationOffset = 180f;
    
    [Tooltip("Whether to apply the rotation fix automatically on Start")]
    public bool applyOnStart = true;

    private XROrigin xrOrigin;

    private void Start()
    {
        if (applyOnStart)
        {
            ApplyOrientationFix();
        }
    }

    /// <summary>
    /// Applies the orientation fix by rotating the XR Origin
    /// </summary>
    public void ApplyOrientationFix()
    {
        // Find the XR Origin if not already assigned
        if (xrOrigin == null)
        {
            xrOrigin = FindObjectOfType<XROrigin>();
        }

        if (xrOrigin != null)
        {
            // Apply the rotation offset to the XR Origin
            Vector3 currentRotation = xrOrigin.transform.eulerAngles;
            xrOrigin.transform.rotation = Quaternion.Euler(
                currentRotation.x, 
                currentRotation.y + rotationOffset, 
                currentRotation.z
            );
            
            //Debug.Log($"PlayerOrientationFix: Applied {rotationOffset}° Y rotation to XR Origin. New rotation: {xrOrigin.transform.eulerAngles}");
        }
        else
        {
            //Debug.LogWarning("PlayerOrientationFix: Could not find XR Origin in the scene!");
        }
    }

    /// <summary>
    /// Resets the XR Origin rotation to its original state
    /// </summary>
    public void ResetOrientation()
    {
        if (xrOrigin != null)
        {
            xrOrigin.transform.rotation = Quaternion.identity;
            Debug.Log("PlayerOrientationFix: Reset XR Origin rotation to default");
        }
    }
}