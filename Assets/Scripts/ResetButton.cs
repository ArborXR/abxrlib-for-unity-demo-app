using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class ResetButton : XRBaseInteractable
{
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}