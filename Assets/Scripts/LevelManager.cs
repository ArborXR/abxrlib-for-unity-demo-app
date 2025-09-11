using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public Dropper dropper;
    public AudioSource successAudioSource;
    public AudioSource failureAudioSource;
    public AudioSource victoryAudioSource;
    public double score;
    private int _totalTargets;
    private int _completedTargets;
    private const double passingScore = 70;

    private void Start()
    {
        //Debug.Log("AbxrLib - Assessment Start");
        Debug.Log("AbxrLib - WhatTimeIsIt: " + Abxr.WhatTimeIsIt());
        Abxr.EventAssessmentStart("stocking_training_unit_1");
        InitializeGame();
        InvokeRepeating(nameof(CheckRunTime), 0, 300); // Call every 5 minutes
        InvokeRepeating(nameof(TestCheck), 0, 30); // Call every 30 seconds
        
        // Set up authentication completion callback to log module information
        //Abxr.OnAuthCompleted(OnAuthenticationCompleted);
        //See OnAuthenticationCompleted below for authentication completion callback
    }

    private void CheckForCompletion()
    {
        if (_completedTargets >= _totalTargets)
        {
            //Without meta data
            //Abxr.EventAssessmentComplete("stocking_training_unit_1", $"{score}", result: score > passingScore ? Abxr.ResultOptions.Pass : Abxr.ResultOptions.Fail);

            //With meta data
            var assessmentMetadata = new Abxr.Dict
            {
                ["mode"] = "easy",
                ["touched_floor"] = "true"
            };
            Abxr.EventAssessmentComplete("stocking_training_unit_1", $"{score}", result: score > passingScore ? Abxr.ResultOptions.Pass : Abxr.ResultOptions.Fail, meta: assessmentMetadata);
            if (score > passingScore)
            {
                PlaySuccessSound();
            }
            else
            {
                PlayFailSound();
            }
        }
    }

    private void CheckRunTime()
    {
        Abxr.LogCritical("AbxrLib - Spending way too much time sorting fruit! This is not that hard a task!");
    }

    private void TestCheck()
    {
        Abxr.LogError("AbxrLib - Bad Luck, Description: We rolled the dice for fun and found you lost! This is mostly just for testing purposes.");
    }

    private void InitializeGame()
    {
        _totalTargets = FindObjectsByType<TargetLocation>(FindObjectsSortMode.None).Length;
        _completedTargets = 0;
        score = 0;
    }

    public void CompleteTask(TargetLocation.CompletionData completionData)
    {
        Abxr.LogInfo("Placement Attempted");
        Debug.Log("AbxrLib - Placement Attempted");

        if (completionData.usedType != completionData.targetType)
        {
            dropper.Replace(completionData.targetType, completionData.usedType);

            completionData.usedTarget.GetComponent<MeshFilter>().sharedMesh = completionData.usedObject.GetComponent<MeshFilter>().sharedMesh;
            string objectId = completionData.usedObject.GetComponent<GrabbableObject>().Id; // Change 'id' to 'Id'
            // Abxr.EventInteractionStart is called in GrabbableObject.cs
            var placementMetadata = new Abxr.Dict
            {
                ["placed_fruit"] = completionData.usedType.ToString(),
                ["intended_fruit"] = completionData.targetType.ToString()
            };
            Abxr.EventInteractionComplete($"place_item_{objectId}", "False", "Wrong spot", Abxr.InteractionType.Bool, placementMetadata);
            Abxr.LogCritical($"Improper placement of {completionData.usedType}");
            StartCoroutine(PlayFailSoundThenRestart());
        }
        else
        {
            string objectId = completionData.usedObject.GetComponent<GrabbableObject>().Id; // Change 'id' to 'Id'

            var placementMetadata = new Abxr.Dict
            {
                ["placed_fruit"] = completionData.usedType.ToString(),
                ["intended_fruit"] = completionData.targetType.ToString()
            };
            Abxr.EventInteractionComplete($"place_item_{objectId}", "True", "Correct spot", Abxr.InteractionType.Bool, placementMetadata);

            StartCoroutine(PlaySuccessSoundAndCheckVictory());
        }

        // Clear XR Grab Interactable colliders (runtime safety check for WebGL)
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            var xrGrab = completionData.usedObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (xrGrab != null)
            {
                xrGrab.colliders.Clear();
            }
        }

        // Disable the collision box of the usedTarget
        Collider targetCollider = completionData.usedTarget.GetComponent<Collider>();
        if (targetCollider != null)
        {
            targetCollider.enabled = false;
        }

        completionData.usedTarget.GetComponent<MeshRenderer>().materials = GrabbableObjectManager.getInstance().getGrabbableObjectData(completionData.usedType).model.GetComponent<MeshRenderer>().sharedMaterials;

        Destroy(completionData.usedObject);
        Destroy(completionData.usedTarget.GetComponent<Outline>());
        Destroy(completionData.usedTarget.GetComponent<TargetLocation>());

        // Calculate Score - later this should be moved out of level manager into its own score manager class that is persistant
        score += (1 / completionData.positionDistance) > 5 ? 5 : 1 / completionData.positionDistance;
        score += 1 - completionData.rotationDistance;
        score *= completionData.targetType == completionData.usedType ? 1 : .25;
    }

    private IEnumerator PlaySuccessSoundAndCheckVictory()
    {
        successAudioSource.Play();
        yield return new WaitForSeconds(successAudioSource.clip.length);

        // Increment completed targets and check for victory
        _completedTargets++;
        CheckForCompletion();
    }
    private void PlaySuccessSound()
    {
        if (victoryAudioSource != null && !victoryAudioSource.isPlaying)
        {
            victoryAudioSource.Play();
            Debug.Log("Level Completed! Success!");

            StartCoroutine(RestartAfterCompletionSound(victoryAudioSource.clip.length));
        }
    }

    private void PlayFailSound()
    {
        if (failureAudioSource != null && !failureAudioSource.isPlaying)
        {
            failureAudioSource.Play();
            Debug.Log("Level Completed! Failure!");

            StartCoroutine(RestartAfterCompletionSound(failureAudioSource.clip.length));
        }
    }

    private IEnumerator RestartAfterCompletionSound(float delay)
    {
        // Wait for the sound to finish playing
        yield return new WaitForSeconds(delay);
        RestartExperience();
    }

    private IEnumerator PlayFailSoundThenRestart()
    {
        if (failureAudioSource != null && !failureAudioSource.isPlaying)
        {
            failureAudioSource.Play();
            yield return new WaitForSeconds(failureAudioSource.clip.length);
        }
        RestartExperience();
    }

    private static void RestartExperience()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

/*
    private void OnAuthenticationCompleted(Abxr.AuthCompletedData authData)
    {
        Debug.Log("=== AUTHENTICATION COMPLETED - MODULE INFORMATION ===");
        
        // Get module count from authData instead of deprecated API
        int totalModuleCount = authData.modules?.Count ?? 0;
        Debug.Log($"Total Modules Available: {totalModuleCount}");
        
        if (totalModuleCount > 0)
        {
            Debug.Log("=== CYCLING THROUGH MODULE TARGETS (Developer API) ===");
            int moduleIndex = 0;
            
            // Keep getting the next module target until none are left (returns null)
            Abxr.CurrentSessionData moduleTarget;
            while ((moduleTarget = Abxr.GetModuleTarget()) != null)
            {
                Debug.Log($"Module [{moduleIndex}]: Target='{moduleTarget.moduleTarget}', UserID='{moduleTarget.userId}', UserEmail='{moduleTarget.userEmail}'");
                Debug.Log($"  - UserData: {(moduleTarget.userData != null ? "Available" : "None")}");
                moduleIndex++;
            }
            
            Debug.Log($"Finished cycling through {moduleIndex} modules");
            
            // Check if all modules have been consumed
            if (moduleIndex == totalModuleCount)
            {
                Debug.Log("=== ALL MODULES COMPLETED - READY FOR NEXT USER ===");
                Debug.Log("Application should now exit user back to start and reauthenticate for next user");
                // TODO: Implement exit to start screen and reauthentication logic
            }
        }
        else
        {
            Debug.Log("No module targets available in authentication response");
        }
        
        Debug.Log("=== END MODULE INFORMATION ===");
    }
    
    private void OnDestroy()
    {
        // Clean up the authentication callback to avoid memory leaks
        Abxr.RemoveAuthCompletedCallback(OnAuthenticationCompleted);
    }
*/
}
