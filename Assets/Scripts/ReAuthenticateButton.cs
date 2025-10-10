using UnityEngine.XR.Interaction.Toolkit;

public class ReAuthenticateButton : UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable
{
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        Abxr.ReAuthenticate();
    }
}