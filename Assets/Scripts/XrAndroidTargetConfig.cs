using UnityEngine;

/// <summary>
/// Build-time and runtime Android XR vendor (Meta Quest OpenXR, PICO OpenXR, HTC VIVE OpenXR).
/// The asset at Resources/XrAndroidTargetConfig is updated by ArborXR/Apply Android XR Target (Editor) and read at runtime for PICO-specific rig fixes.
/// </summary>
[CreateAssetMenu(fileName = "XrAndroidTargetConfig", menuName = "ArborXR/Android XR Target Config")]
public class XrAndroidTargetConfig : ScriptableObject
{
    public enum Vendor
    {
        Meta = 0,
        Pico = 1,
        Htc = 2
    }

    [Tooltip("Active vendor for this build. Keep in sync with Build Profile / Apply Android XR Target.")]
    public Vendor activeVendor = Vendor.Meta;
}
