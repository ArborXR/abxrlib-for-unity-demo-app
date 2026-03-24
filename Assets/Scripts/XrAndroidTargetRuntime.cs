using UnityEngine;

/// <summary>
/// Loads <see cref="XrAndroidTargetConfig"/> from Resources for runtime checks (e.g. PICO locomotion wiring).
/// </summary>
public static class XrAndroidTargetRuntime
{
    static XrAndroidTargetConfig s_config;

    public static XrAndroidTargetConfig Config
    {
        get
        {
            if (s_config == null)
                s_config = Resources.Load<XrAndroidTargetConfig>("XrAndroidTargetConfig");
            return s_config;
        }
    }

    public static bool IsPico =>
        Config != null && Config.activeVendor == XrAndroidTargetConfig.Vendor.Pico;

    public static bool IsHtc =>
        Config != null && Config.activeVendor == XrAndroidTargetConfig.Vendor.Htc;

    public static bool IsMeta =>
        Config == null || Config.activeVendor == XrAndroidTargetConfig.Vendor.Meta;
}
