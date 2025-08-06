using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using Random = System.Random;

public class LevelManager : MonoBehaviour
{
    public Dropper dropper;
    public AudioSource successAudioSource;
    public AudioSource failureAudioSource;
    public AudioSource victoryAudioSource;
    public double score;
    private int _totalTargets;
    private int _completedTargets;
    private static readonly Random random = new();

    private void Start()
    {
        //Abxr.LogInfo("Content started (LevelManager)");
        //Abxr.EventAssessmentStart("stocking_training_unit_1");
        InitializeGame();
        InvokeRepeating(nameof(CheckRunTime), 0, 300); // Call every 5 minutes
        InvokeRepeating(nameof(TestCheck), 0, 30); // Call every 30 seconds
        if (random.NextDouble() < 0.5)
        {
            //Abxr.LogError("Bad Life Direction, Description: The job market is bad, but wow you really couldn't find a better job than a shelf stocker in the void? At least rent must be cheap.");
        }
    }

    private void CheckRunTime()
    {
        //Abxr.LogCritical("AbxrLib - Spending way too much time sorting fruit! This is not that hard a task!");
    }

    private void TestCheck()
    {
        //Abxr.LogError("AbxrLib - Bad Luck, Description: We rolled the dice for fun and found you lost! This is mostly just for testing purposes.");
    }

    private void InitializeGame()
    {
        _totalTargets = FindObjectsOfType<TargetLocation>().Length;
        _completedTargets = 0;
        score = 0;
    }

    public void CompleteTask(TargetLocation.CompletionData completionData)
    {
        //Abxr.LogInfo("Placement Attempted");
        Debug.Log("AbxrLib - Placement Attempted");

        if (completionData.usedType != completionData.targetType)
        {
            dropper.Replace(completionData.targetType, completionData.usedType);

            completionData.usedTarget.GetComponent<MeshFilter>().sharedMesh = completionData.usedObject.GetComponent<MeshFilter>().sharedMesh;
            string objectId = completionData.usedObject.GetComponent<GrabbableObject>().Id; // Change 'id' to 'Id'
            //Abxr.EventInteractionComplete($"place_item_{objectId}", "False", "Wrong spot", Abxr.InteractionType.Bool,
            //    new Dictionary<string, string>
            //    {
            //        ["placed_fruit"] = completionData.usedType.ToString(),
            //        ["intended_fruit"] = completionData.targetType.ToString()
            //    });
            //Abxr.LogCritical($"Improper placement of {completionData.usedType}");
            StartCoroutine(PlayFailSoundThenRestart());
        }
        else
        {
            string objectId = completionData.usedObject.GetComponent<GrabbableObject>().Id; // Change 'id' to 'Id'

            //Abxr.EventInteractionComplete($"place_item_{objectId}", "True", "Correct spot", Abxr.InteractionType.Bool,
            //    new Dictionary<string, string>
            //    {
            //        ["placed_fruit"] = completionData.usedType.ToString(),
            //        ["intended_fruit"] = completionData.targetType.ToString()
            //    });

            StartCoroutine(PlaySuccessSoundAndCheckVictory());
        }

        completionData.usedObject.GetComponent<XRGrabInteractable>().colliders.Clear();

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

    private void CheckForCompletion()
    {
        if (_completedTargets >= _totalTargets)
        {
            if (score > 70)
            {
                //Abxr.EventAssessmentComplete("stocking_training_unit_1", $"{score}", result: Abxr.ResultOptions.Pass);
                PlaySuccessSound();
            }
            else
            {
                //Abxr.EventAssessmentComplete("stocking_training_unit_1", $"{score}", result: Abxr.ResultOptions.Fail);
                PlayFailSound();
            }
        }
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
}
