using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ToggleActionButton : UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable
{
    [Header("Toggle Button Configuration")]
    [SerializeField] private string firstActionName = "First Action";
    [SerializeField] private string secondActionName = "Second Action";
    
    private bool isFirstTouch = true;
    
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        
        if (isFirstTouch)
        {
            PerformFirstAction();
            isFirstTouch = false;
        }
        else
        {
            PerformSecondAction();
            isFirstTouch = true;
        }
    }
    
    private void PerformFirstAction()
    {
        Debug.Log($"ToggleActionButton - {firstActionName}");
        
        // Add your first action here
        // Example: Change a GameObject's color, play a sound, trigger an animation, etc.
        
        // For demonstration, let's log an Abxr interaction
        Abxr.EventObjectiveStart("toggle_button_first_action");
        Abxr.EventObjectiveComplete("toggle_button_first_action", 100, Abxr.EventStatus.Complete);
    }
    
    private void PerformSecondAction()
    {
        Debug.Log($"ToggleActionButton - {secondActionName}");
              
        // For demonstration, let's log an Abxr interaction
        Abxr.EventInteractionStart("toggle_button_second_action");
        Abxr.EventInteractionComplete("toggle_button_second_action", "second_action", "Second action completed", Abxr.InteractionType.Text);

        // Just for testing
        Debug.Log("AbxrLib - About to send assessment complete");
        Abxr.EventAssessmentComplete("stocking_training_unit_1", "88", result: Abxr.ResultOptions.Pass);
        Debug.Log("AbxrLib - Assessment complete sent, waiting before exit");
        
        // Wait a moment for the assessment to be sent before exiting
        StartCoroutine(ExitAfterDelay());
    }

    private System.Collections.IEnumerator ExitAfterDelay()
    {
        // Wait 2 seconds to allow the assessment event to be sent
        yield return new WaitForSeconds(2f);
        
        Debug.Log("AbxrLib - Exiting application");
        // Exit the app and return to launcher
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    // Optional: Method to reset the toggle state
    public void ResetToggleState()
    {
        isFirstTouch = true;
        Debug.Log("ToggleActionButton - State reset to first action");
    }
    
    // Optional: Method to check current state
    public bool IsFirstTouchNext()
    {
        return isFirstTouch;
    }
    
    /// <summary>
    /// Public method to directly trigger the toggle action - called by DesktopInputController
    /// </summary>
    public void TriggerAction()
    {
        if (isFirstTouch)
        {
            PerformFirstAction();
            isFirstTouch = false;
        }
        else
        {
            PerformSecondAction();
            isFirstTouch = true;
        }
    }
}
