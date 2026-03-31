#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time warning if the local PICO dual-SDK folders referenced by Packages/manifest.json are missing.
/// AbxrLib PICO enterprise QR requires com.unity.xr.picoxr + com.unity.xr.openxr.picoxr from those paths.
/// </summary>
[InitializeOnLoad]
internal static class PicoDemoSdkPathValidator
{
    private const string LogPrefix = "[abxrlib-for-unity-demo-app] ";

    static PicoDemoSdkPathValidator()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        EditorApplication.delayCall -= RunOnce;

        // Packages/manifest.json uses file:../../… — resolved from project root's parent (workspace).
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string workspaceRoot = Path.GetFullPath(Path.Combine(projectRoot, ".."));

        string openXrPkg = Path.Combine(workspaceRoot, "Unity OpenXR IntegrationSDK-demo-app", "package.json");
        string picoIntPkg = Path.Combine(workspaceRoot, "PICO Unity Integration SDK-demo-app", "package.json");
        string abxrPkg = Path.Combine(workspaceRoot, "abxrlib-for-unity", "package.json");

        bool ok = true;
        if (!File.Exists(openXrPkg))
        {
            ok = false;
            Debug.LogWarning(LogPrefix + "Missing OpenXR PICO package at:\n  " + openXrPkg, null);
        }

        if (!File.Exists(picoIntPkg))
        {
            ok = false;
            Debug.LogWarning(LogPrefix + "Missing PICO Unity Integration package at:\n  " + picoIntPkg, null);
        }

        if (!File.Exists(abxrPkg))
        {
            Debug.LogWarning(LogPrefix + "Local AbxrLib package not found at:\n  " + abxrPkg + "\n(OK if you use a different path in manifest.json.)", null);
        }

        if (!ok)
        {
            Debug.LogWarning(
                LogPrefix + "PICO SDK layout is incomplete. AbxrLib PICO QR (PXR_Enterprise) will not build correctly. " +
                "From the demo repo root run: bash tools/pico-setup-demo-sdks.sh — see tools/README.md.",
                null);
        }
    }
}
#endif
