#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Applies Android XR vendor selection: Resources config, optional VIVE UPM line, OpenXR YAML feature toggles, and Android scripting defines.
/// Pair with Unity 6 Build Profiles named Android_Meta, Android_Pico, Android_HTC.
/// </summary>
public static class ApplyXrTargetEditor
{
    const string VivePackageId = "com.htc.upm.vive.openxr";
    const string VivePackageLine = "    \"com.htc.upm.vive.openxr\": \"https://github.com/ViveSoftware/VIVE-OpenXR.git?path=com.htc.upm.vive.openxr\",";

    const string DefineMeta = "ABXR_ANDROID_TARGET_META";
    const string DefinePico = "ABXR_ANDROID_TARGET_PICO";
    const string DefineHtc = "ABXR_ANDROID_TARGET_HTC";

    static readonly string OpenXrSettingsPath = Path.Combine("Assets", "XR", "Settings", "Open XR Package Settings.asset");
    static readonly string ManifestPath = Path.Combine("Packages", "manifest.json");
    static readonly string ConfigResourcePath = Path.Combine("Assets", "Resources", "XrAndroidTargetConfig.asset");

    [MenuItem("ArborXR/Android XR Target/Meta (Quest)")]
    public static void ApplyMeta() => Apply(XrAndroidTargetConfig.Vendor.Meta);

    [MenuItem("ArborXR/Android XR Target/Pico")]
    public static void ApplyPico() => Apply(XrAndroidTargetConfig.Vendor.Pico);

    [MenuItem("ArborXR/Android XR Target/HTC (VIVE)")]
    public static void ApplyHtc() => Apply(XrAndroidTargetConfig.Vendor.Htc);

    public static void Apply(XrAndroidTargetConfig.Vendor vendor)
    {
        var cfg = AssetDatabase.LoadAssetAtPath<XrAndroidTargetConfig>(ConfigResourcePath);
        if (cfg == null)
        {
            Debug.LogError("[ApplyXrTarget] Missing XrAndroidTargetConfig at " + ConfigResourcePath);
            return;
        }

        Undo.RecordObject(cfg, "Set Android XR vendor");
        cfg.activeVendor = vendor;
        EditorUtility.SetDirty(cfg);

        EnsureVivePackageInManifest();
        ToggleOpenXrYamlFeatures(vendor);
        SetAndroidScriptingDefines(vendor);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ApplyXrTarget] Active vendor = {vendor}. Review Packages/manifest and OpenXR settings; resolve packages if prompted.");
    }

    /// <summary>
    /// Merged OpenXR settings reference VIVE features; keep the UPM entry so the project resolves. Optional CI may strip it for Meta-only builds (advanced).
    /// </summary>
    public static void EnsureVivePackageInManifest()
    {
        var full = Path.GetFullPath(ManifestPath);
        if (!File.Exists(full))
            return;
        var text = File.ReadAllText(full);
        if (text.Contains($"\"{VivePackageId}\"", StringComparison.Ordinal))
            return;
        text = text.Replace(
            "\"com.arborxr.unity\": \"https://github.com/ArborXR/abxrlib-for-unity.git\",",
            "\"com.arborxr.unity\": \"https://github.com/ArborXR/abxrlib-for-unity.git\",\n" + VivePackageLine);
        File.WriteAllText(full, text);
    }

    static void ToggleOpenXrYamlFeatures(XrAndroidTargetConfig.Vendor vendor)
    {
        var full = Path.GetFullPath(OpenXrSettingsPath);
        if (!File.Exists(full))
            return;

        var text = File.ReadAllText(full);
        var parts = Regex.Split(text, @"(?=--- !u!114)");
        for (var i = 0; i < parts.Length; i++)
        {
            var block = parts[i];
            if (string.IsNullOrEmpty(block) || !block.StartsWith("--- !u!114", StringComparison.Ordinal))
                continue;

            var pico = IsPicoOpenXrBlock(block);
            var vive = IsViveOpenXrBlock(block);
            if (!pico && !vive)
                continue;

            var on = (vendor == XrAndroidTargetConfig.Vendor.Pico && pico)
                     || (vendor == XrAndroidTargetConfig.Vendor.Htc && vive);
            if (vendor == XrAndroidTargetConfig.Vendor.Meta)
                on = false;

            parts[i] = SetFirstMEnabledInBlock(block, on);
        }

        File.WriteAllText(full, string.Concat(parts));
    }

    static bool IsPicoOpenXrBlock(string block)
    {
        return block.Contains("company: PICO", StringComparison.Ordinal)
               || block.Contains("Unity.XR.OpenXR.Features.PICOSupport::", StringComparison.Ordinal)
               || block.Contains("Unity.XR.OpenXRPico::", StringComparison.Ordinal);
    }

    static bool IsViveOpenXrBlock(string block)
    {
        return block.Contains("VIVE.OpenXR::", StringComparison.Ordinal);
    }

    static string SetFirstMEnabledInBlock(string block, bool enabled)
    {
        var val = enabled ? "1" : "0";
        var n = 0;
        return Regex.Replace(
            block,
            @"^(\s*m_enabled:\s*)(0|1)\s*$",
            m =>
            {
                if (++n > 1)
                    return m.Value;
                return $"{m.Groups[1]}{val}";
            },
            RegexOptions.Multiline,
            TimeSpan.FromSeconds(2));
    }

    static void SetAndroidScriptingDefines(XrAndroidTargetConfig.Vendor vendor)
    {
        var raw = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
        var set = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        set.RemoveAll(x => x == DefineMeta || x == DefinePico || x == DefineHtc);
        switch (vendor)
        {
            case XrAndroidTargetConfig.Vendor.Meta:
                set.Add(DefineMeta);
                break;
            case XrAndroidTargetConfig.Vendor.Pico:
                set.Add(DefinePico);
                break;
            case XrAndroidTargetConfig.Vendor.Htc:
                set.Add(DefineHtc);
                break;
        }

        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, string.Join(";", set.Distinct()));
    }
}
#endif
