using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

/// <summary>
/// PICO builds previously changed the Level scene XR Interaction Setup prefab overrides (mediator wiring).
/// Those overrides are applied at runtime when <see cref="XrAndroidTargetConfig"/> is set to Pico.
/// </summary>
public static class XrPicoLocomotionBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterSceneLoad()
    {
        if (Application.platform != RuntimePlatform.Android)
            return;
        if (!XrAndroidTargetRuntime.IsPico)
            return;

        var origin = Object.FindFirstObjectByType<XROrigin>();
        if (origin == null)
            return;

        var mediator = origin.GetComponentInChildren<LocomotionMediator>(true);
        if (mediator == null)
        {
            Debug.LogWarning("[XrPicoLocomotionBootstrap] No LocomotionMediator under XROrigin; skipping PICO locomotion link.");
            return;
        }

        var providers = origin.GetComponentsInChildren<LocomotionProvider>(true);
        foreach (var provider in providers)
        {
            if (provider != null && provider.mediator == null)
                provider.mediator = mediator;
        }
    }
}
