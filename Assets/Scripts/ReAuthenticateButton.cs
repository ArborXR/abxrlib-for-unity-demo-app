using UnityEngine.XR.Interaction.Toolkit;

public class ReAuthenticateButton : XRBaseInteractable
{
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        //Abxr.ReAuthenticate();
    }
}