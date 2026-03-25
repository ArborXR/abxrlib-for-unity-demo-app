#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEngine;

/// <summary>
/// Applies Android XR vendor selection: Resources config, OpenXR YAML feature toggles, Android scripting defines,
/// and (Unity 6) the active Build Profile under <c>Assets/Settings/Build Profiles/</c>.
/// <c>com.htc.upm.vive.openxr</c> stays in <c>Packages/manifest.json</c> so merged Open XR settings deserialize; vendor behavior is toggled here, not by removing the package.
/// <para />
/// <c>Open XR Package Settings.baseline.asset</c> is a frozen multi-vendor layout (PICO + VIVE + Meta blocks). Each <see cref="Apply"/> copies it over
/// <c>Open XR Package Settings.asset</c> before toggling, so the active file does not accumulate endless manual merges.
/// </summary>
public static class ApplyXrTargetEditor
{
    const string DefineMeta = "ABXR_ANDROID_TARGET_META";
    const string DefinePico = "ABXR_ANDROID_TARGET_PICO";
    const string DefineHtc = "ABXR_ANDROID_TARGET_HTC";

    static readonly string OpenXrSettingsPath = Path.Combine("Assets", "XR", "Settings", "Open XR Package Settings.asset");
    static readonly string OpenXrBaselinePath = Path.Combine("Assets", "XR", "Settings", "Open XR Package Settings.baseline.asset");
    const string OpenXrSettingsAssetPath = "Assets/XR/Settings/Open XR Package Settings.asset";

    /// <summary>Root object name in YAML must match the .asset filename; baseline uses a .baseline suffix.</summary>
    const string OpenXrYamlMainNameBaseline = "  m_Name: Open XR Package Settings.baseline";

    const string OpenXrYamlMainNameActive = "  m_Name: Open XR Package Settings";
    static readonly string ConfigResourcePath = Path.Combine("Assets", "Resources", "XrAndroidTargetConfig.asset");

    const string BuildProfileMetaPath = "Assets/Settings/Build Profiles/Android_Meta.asset";
    const string BuildProfilePicoPath = "Assets/Settings/Build Profiles/Android_Pico.asset";
    const string BuildProfileHtcPath = "Assets/Settings/Build Profiles/Android_HTC.asset";

    [MenuItem("XRBuildTools/Android XR Target/Meta (Quest)")]
    public static void ApplyMeta() => Apply(XrAndroidTargetConfig.Vendor.Meta);

    [MenuItem("XRBuildTools/Android XR Target/Pico")]
    public static void ApplyPico() => Apply(XrAndroidTargetConfig.Vendor.Pico);

    [MenuItem("XRBuildTools/Android XR Target/HTC (VIVE)")]
    public static void ApplyHtc() => Apply(XrAndroidTargetConfig.Vendor.Htc);

    /// <summary>
    /// Copies baseline → active OpenXR settings, re-applies YAML toggles, Android scripting defines, and the Unity 6 Build Profile
    /// for <see cref="XrAndroidTargetConfig.activeVendor"/> without changing the selected vendor (use after editing the baseline asset).
    /// </summary>
    [MenuItem("XRBuildTools/Android XR Target/Restore OpenXR from baseline (keep current vendor)")]
    public static void RestoreOpenXrFromBaselineMenu()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<XrAndroidTargetConfig>(ConfigResourcePath);
        if (cfg == null)
        {
            Debug.LogError("[ApplyXrTarget] Missing XrAndroidTargetConfig at " + ConfigResourcePath);
            return;
        }

        if (!TryRestoreOpenXrFromBaseline())
            return;

        var v = cfg.activeVendor;
        ToggleOpenXrYamlFeatures(v);
        SetAndroidScriptingDefines(v);
        TrySetActiveBuildProfile(v);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ApplyXrTarget] Restored OpenXR from baseline; toggles, defines, and Build Profile re-applied for vendor = {v}.");
    }

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

        TryRestoreOpenXrFromBaseline();
        ToggleOpenXrYamlFeatures(vendor);
        SetAndroidScriptingDefines(vendor);
        TrySetActiveBuildProfile(vendor);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ApplyXrTarget] Active vendor = {vendor}. Restored OpenXR from baseline (if present), applied toggles, Build Profile, and defines.");
    }

    /// <summary>
    /// Copies <c>Open XR Package Settings.baseline.asset</c> onto <c>Open XR Package Settings.asset</c>.
    /// Returns false if baseline is missing (Apply continues with toggles on whatever is already in the active asset).
    /// </summary>
    public static bool TryRestoreOpenXrFromBaseline()
    {
        var baselineFull = Path.GetFullPath(OpenXrBaselinePath);
        var targetFull = Path.GetFullPath(OpenXrSettingsPath);
        if (!File.Exists(baselineFull))
        {
            Debug.LogWarning("[ApplyXrTarget] Missing baseline at " + OpenXrBaselinePath + ". Skipping restore; edit toggles on the active OpenXR asset only.");
            return false;
        }

        File.Copy(baselineFull, targetFull, overwrite: true);

        // Active asset filename is "Open XR Package Settings.asset" — root m_Name must be "Open XR Package Settings", not ".baseline".
        var yaml = File.ReadAllText(targetFull);
        if (yaml.Contains(OpenXrYamlMainNameBaseline))
            yaml = yaml.Replace(OpenXrYamlMainNameBaseline, OpenXrYamlMainNameActive, StringComparison.Ordinal);
        File.WriteAllText(targetFull, yaml);

        AssetDatabase.ImportAsset(OpenXrSettingsAssetPath, ImportAssetOptions.ForceUpdate);
        return true;
    }

    static void TrySetActiveBuildProfile(XrAndroidTargetConfig.Vendor vendor)
    {
        var path = vendor switch
        {
            XrAndroidTargetConfig.Vendor.Meta => BuildProfileMetaPath,
            XrAndroidTargetConfig.Vendor.Pico => BuildProfilePicoPath,
            XrAndroidTargetConfig.Vendor.Htc => BuildProfileHtcPath,
            _ => null
        };
        if (path == null)
            return;

        var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
        if (profile == null)
        {
            Debug.LogWarning("[ApplyXrTarget] Build Profile missing at " + path + ". Expected Unity 6 assets Android_Meta / Android_Pico / Android_HTC under Assets/Settings/Build Profiles.");
            return;
        }

        BuildProfile.SetActiveBuildProfile(profile);
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

            // VIVE: Spectator Camera off ↔ First Person Observer off (ViveSpectatorCameraProcess). Handle before generic VIVE toggle.
            if (IsViveFirstPersonObserverBlock(block))
            {
                parts[i] = SetEveryMEnabledInBlock(block, false);
                continue;
            }

            if (IsViveSpectatorSecondaryViewBlock(block))
            {
                parts[i] = SetFirstMEnabledInBlock(block, false);
                continue;
            }

            // VIVE Android: VivePassthrough and ViveCompositionLayerPassthrough both register XR_HTC_passthrough; Unity OpenXR
            // validation fails if both are enabled. Keep VivePassthrough; the Composition Layer feature is deprecated.
            if (IsViveCompositionLayerPassthroughAndroidBlock(block))
            {
                parts[i] = SetFirstMEnabledInBlock(block, false);
                continue;
            }

            // Merged OpenXR YAML can contain duplicate VIVE features (same featureIdInternal) where one copy lost its MonoScript
            // (m_Script: {fileID: 0}). Enabling both fails OpenXR project validation ("duplicate OpenXR extensions").
            if (IsViveOpenXrBlockWithMissingMonoScript(block))
            {
                parts[i] = SetFirstMEnabledInBlock(block, false);
                continue;
            }

            // Meta Android: Unity 6 deprecates OculusQuestFeature — keep it off; use MetaQuestFeature Android for Quest builds.
            if (IsDeprecatedOculusQuestAndroidBlock(block))
            {
                parts[i] = SetFirstMEnabledInBlock(block, false);
                continue;
            }

            if (IsMetaQuestFeatureAndroidBlock(block))
            {
                var metaOn = vendor == XrAndroidTargetConfig.Vendor.Meta;
                parts[i] = SetFirstMEnabledInBlock(block, metaOn);
                continue;
            }

            // Meta Quest touch/controller Android features: on for Meta only; off for Pico/HTC.
            if (IsMetaQuestAndroidAuxiliaryBlock(block))
            {
                var metaOn = vendor == XrAndroidTargetConfig.Vendor.Meta;
                parts[i] = SetFirstMEnabledInBlock(block, metaOn);
                continue;
            }

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
        if (block.Contains("VIVE.OpenXR::", StringComparison.Ordinal))
            return true;
        // Merged YAML sometimes uses m_EditorClassIdentifier: VIVE.OpenXR:VIVE... (single colon after OpenXR)
        return block.Contains("m_EditorClassIdentifier: VIVE.OpenXR", StringComparison.Ordinal);
    }

    /// <summary>Duplicate/merged VIVE feature stubs reference no script; they must never be enabled alongside the real block.</summary>
    static bool IsViveOpenXrBlockWithMissingMonoScript(string block)
    {
        if (!block.Contains("VIVE.OpenXR", StringComparison.Ordinal))
            return false;
        return block.Contains("m_Script: {fileID: 0}", StringComparison.Ordinal);
    }

    static bool IsViveFirstPersonObserverBlock(string block)
    {
        return block.Contains("ViveFirstPersonObserver", StringComparison.Ordinal)
               || block.Contains("FirstPersonObserver.ViveFirstPersonObserver", StringComparison.Ordinal);
    }

    static bool IsViveSpectatorSecondaryViewBlock(string block)
    {
        return block.Contains("ViveSecondaryViewConfiguration", StringComparison.Ordinal);
    }

    static bool IsViveCompositionLayerPassthroughAndroidBlock(string block)
    {
        return block.Contains("ViveCompositionLayerPassthrough Android", StringComparison.Ordinal);
    }

    static bool IsDeprecatedOculusQuestAndroidBlock(string block)
    {
        if (!block.Contains("Android", StringComparison.Ordinal))
            return false;
        return block.Contains("OculusQuestFeature", StringComparison.Ordinal)
               || block.Contains("com.unity.openxr.feature.oculusquest", StringComparison.Ordinal);
    }

    static bool IsMetaQuestFeatureAndroidBlock(string block)
    {
        return block.Contains("MetaQuestFeature Android", StringComparison.Ordinal)
               || (block.Contains("Android", StringComparison.Ordinal)
                   && block.Contains("featureIdInternal: com.unity.openxr.feature.metaquest", StringComparison.Ordinal)
                   && !block.Contains("oculusquest", StringComparison.Ordinal));
    }

    static bool IsMetaQuestAndroidAuxiliaryBlock(string block)
    {
        if (!block.Contains("Android", StringComparison.Ordinal))
            return false;
        if (block.Contains("OculusQuestFeature", StringComparison.Ordinal)
            || block.Contains("MetaQuestFeature Android", StringComparison.Ordinal))
            return false;
        return block.Contains("OculusTouchControllerProfile Android", StringComparison.Ordinal)
               || block.Contains("MetaQuestTouchPlusControllerProfile Android", StringComparison.Ordinal)
               || block.Contains("MetaQuestTouchProControllerProfile Android", StringComparison.Ordinal);
    }

    static string SetEveryMEnabledInBlock(string block, bool enabled)
    {
        var val = enabled ? "1" : "0";
        return Regex.Replace(
            block,
            @"^(\s*m_enabled:\s*)(0|1)\s*$",
            m => $"{m.Groups[1]}{val}",
            RegexOptions.Multiline,
            TimeSpan.FromSeconds(2));
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
