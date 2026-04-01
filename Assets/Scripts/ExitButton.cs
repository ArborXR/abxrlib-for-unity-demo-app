using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ExitButton : UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable
{
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        RequestQuit();
    }

    /// <summary>
    /// Desktop middle-click and VR grab both end up here. Finalizes stocking assessment if the player
    /// began the activity (fruit spawned or at least one slot filled) before quitting.
    /// </summary>
    public void RequestQuit()
    {
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
            levelManager.TryFinalizeAssessmentBeforeExit();
        else
            QuitApplicationImmediate();
    }

    public static void QuitApplicationImmediate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
