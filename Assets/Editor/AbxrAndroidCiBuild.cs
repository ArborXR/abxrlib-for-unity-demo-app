#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Batchmode entry for CI: <c>-executeMethod AbxrAndroidCiBuild.BuildFromEnvironment</c>.
/// Set <c>XR_TARGET</c> to meta, pico, or htc (Android build profile names align with Android_Meta / Android_Pico / Android_HTC).
/// </summary>
public static class AbxrAndroidCiBuild
{
    /// <summary>
    /// Reads XR_TARGET (meta|pico|htc), applies <see cref="ApplyXrTargetEditor"/>, then builds Android using scenes in Build Settings.
    /// </summary>
    public static void BuildFromEnvironment()
    {
        var raw = Environment.GetEnvironmentVariable("XR_TARGET") ?? "meta";
        var v = ParseVendor(raw);
        if (v == null)
        {
            Debug.LogError("[AbxrAndroidCiBuild] XR_TARGET must be meta, pico, or htc. Got: " + raw);
            EditorApplication.Exit(1);
            return;
        }

        ApplyXrTargetEditor.Apply(v.Value, ok =>
        {
            if (!ok)
            {
                Debug.LogError("[AbxrAndroidCiBuild] Apply XR target failed (PICO OpenXR package add/remove). For PICO, install the SDK at the path in ApplyXrTargetEditor or set EditorPrefs AbxrPicoOpenXrPackageSpecifier.");
                EditorApplication.Exit(4);
                return;
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[AbxrAndroidCiBuild] No scenes in Build Settings.");
                EditorApplication.Exit(2);
                return;
            }

            var outDir = Environment.GetEnvironmentVariable("ABXR_BUILD_OUTPUT_DIR") ?? "Builds/Android";
            var name = Environment.GetEnvironmentVariable("ABXR_BUILD_NAME") ?? "abxrlibforunitydemoapp";
            var path = $"{outDir}/{name}.apk".Replace("\\", "/");

            var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });

            if (r.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[AbxrAndroidCiBuild] Build failed: " + r.summary.result);
                EditorApplication.Exit(3);
                return;
            }

            Debug.Log("[AbxrAndroidCiBuild] Build succeeded: " + path);
            EditorApplication.Exit(0);
        });
    }

    static XrAndroidTargetConfig.Vendor? ParseVendor(string raw)
    {
        var s = raw.Trim().ToLowerInvariant();
        if (s is "meta" or "quest" or "android_meta")
            return XrAndroidTargetConfig.Vendor.Meta;
        if (s is "pico" or "android_pico")
            return XrAndroidTargetConfig.Vendor.Pico;
        if (s is "htc" or "vive" or "android_htc")
            return XrAndroidTargetConfig.Vendor.Htc;
        return null;
    }
}
#endif
