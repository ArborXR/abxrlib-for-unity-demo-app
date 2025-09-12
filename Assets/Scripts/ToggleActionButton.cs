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
        
        try
        {
            // For demonstration, let's log an Abxr interaction
            Debug.Log("ToggleActionButton: About to call Abxr.EventObjectiveStart");
            Abxr.EventObjectiveStart("toggle_button_first_action");
            Debug.Log("ToggleActionButton: About to call Abxr.EventObjectiveComplete");
            Abxr.EventObjectiveComplete("toggle_button_first_action", 100, Abxr.EventStatus.Complete);
            Debug.Log("ToggleActionButton: Successfully completed first action");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ToggleActionButton: Exception in PerformFirstAction: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    private void PerformSecondAction()
    {
        Debug.Log($"ToggleActionButton - {secondActionName}");
              
        // For demonstration, let's log an Abxr interaction
        Abxr.EventInteractionStart("toggle_button_second_action");
        Abxr.EventInteractionComplete("toggle_button_second_action", "second_action", "Second action completed", Abxr.InteractionType.Text);

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
