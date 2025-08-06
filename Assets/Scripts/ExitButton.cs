using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ExitButton : XRBaseInteractable
{
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        // This will close the app on standalone/Android/iOS builds
        Application.Quit();

        // (In the Editor, stop play mode for testing)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}