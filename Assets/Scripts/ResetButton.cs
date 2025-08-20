using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class ResetButton : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
{
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}