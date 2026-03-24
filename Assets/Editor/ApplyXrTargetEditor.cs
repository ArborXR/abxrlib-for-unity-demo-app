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

    [MenuItem("XRBuildTools/Android XR Target/Meta (Quest)")]
    public static void ApplyMeta() => Apply(XrAndroidTargetConfig.Vendor.Meta);

    [MenuItem("XRBuildTools/Android XR Target/Pico")]
    public static void ApplyPico() => Apply(XrAndroidTargetConfig.Vendor.Pico);

    [MenuItem("XRBuildTools/Android XR Target/HTC (VIVE)")]
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

        SyncVivePackageInManifest(vendor == XrAndroidTargetConfig.Vendor.Htc);
        ToggleOpenXrYamlFeatures(vendor);
        SetAndroidScriptingDefines(vendor);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ApplyXrTarget] Active vendor = {vendor}. Review Packages/manifest and OpenXR settings; resolve packages if prompted.");
    }

    /// <summary>
    /// Adds or removes the VIVE OpenXR UPM line. When adding, inserts immediately after the <c>com.arborxr.unity</c> dependency line (any source: git, local path, etc.).
    /// HTC target includes the package; Meta/Pico remove it so local abxrlib paths and non-HTC workflows stay valid.
    /// </summary>
    public static void SyncVivePackageInManifest(bool includeVive)
    {
        var full = Path.GetFullPath(ManifestPath);
        if (!File.Exists(full))
            return;

        var lines = File.ReadAllLines(full).ToList();
        lines.RemoveAll(l => IsViveManifestLine(l));

        if (!includeVive)
        {
            File.WriteAllLines(full, lines);
            return;
        }

        var insertAt = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Contains("\"com.arborxr.unity\"", StringComparison.Ordinal))
            {
                insertAt = i + 1;
                break;
            }
        }

        if (insertAt < 0)
        {
            Debug.LogWarning("[ApplyXrTarget] Packages/manifest.json has no \"com.arborxr.unity\" dependency; cannot insert VIVE OpenXR package.");
            File.WriteAllLines(full, lines);
            return;
        }

        lines.Insert(insertAt, VivePackageLine);
        File.WriteAllLines(full, lines);
    }

    static bool IsViveManifestLine(string line)
    {
        var t = line.TrimStart();
        return t.StartsWith($"\"{VivePackageId}\"", StringComparison.Ordinal);
    }

    /// <summary>Backward-compatible alias: ensures VIVE is present (HTC-style).</summary>
    public static void EnsureVivePackageInManifest() => SyncVivePackageInManifest(true);

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
