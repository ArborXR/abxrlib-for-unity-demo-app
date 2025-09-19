using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        Debug.Log("AbxrLib - Assessment Start");
        //Debug.Log("AbxrLib - WhatTimeIsIt: " + Abxr.WhatTimeIsIt());
        //Debug.Log("AbxrLib - DeviceId: " + Abxr.GetDeviceId());
        //Debug.Log("AbxrLib - DeviceSerial: " + Abxr.GetDeviceSerial());
        //Debug.Log("AbxrLib - DeviceTitle: " + Abxr.GetDeviceTitle());
        // Debug.Log("AbxrLib - DeviceTags: " + Abxr.GetDeviceTags());
        // Debug.Log("AbxrLib - OrgId: " + Abxr.GetOrgId());
        // Debug.Log("AbxrLib - OrgTitle: " + Abxr.GetOrgTitle());
        // Debug.Log("AbxrLib - OrgSlug: " + Abxr.GetOrgSlug());
        Abxr.EventAssessmentStart("stocking_training_unit_1");
        
        // Initialize Android deep link handler for external deep links (not moduleTarget)
        InitializeAndroidDeepLinkHandler();
        
        // Subscribe to AbxrLib's moduleTarget deep link event
        Abxr.OnModuleTargetDeepLink += OnDeepLinkReceived;
        
        InitializeGame();
        InvokeRepeating(nameof(CheckRunTime), 0, 300); // Call every 5 minutes
        InvokeRepeating(nameof(TestCheck), 0, 30); // Call every 30 seconds
        
        // Set up authentication completion callback to log module information
        // See OnAuthenticationCompleted below for authentication completion callback
        Abxr.OnAuthCompleted += OnAuthenticationCompleted;
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
    private void OnAuthenticationCompleted(bool success, string error)
    {
        if (!success)
        {
            Debug.Log("=== AUTHENTICATION COMPLETED - FAILURE ===");
            Debug.Log("Error: " + error);
            return;
        }
        Debug.Log("=== AUTHENTICATION COMPLETED - SUCCESS ===");

        var authData = AbxrLib.Runtime.Authentication.Authentication.GetAuthResponse();

        Debug.Log("=== AUTHENTICATION COMPLETED - USER INFORMATION ===");
        Debug.Log($"User - ID: {(authData.UserData.ContainsKey("id") ? authData.UserData["id"] : "Not provided")}");
        Debug.Log($"User - Name: {(authData.UserData.ContainsKey("name") ? authData.UserData["name"] : "Not provided")}");
        Debug.Log($"User - user_id: {(authData.UserData.ContainsKey("user_id") ? authData.UserData["user_id"] : "Not provided")}");
        Debug.Log($"User - Email: {(authData.UserData.ContainsKey("email") ? authData.UserData["email"] : "Not provided")}");
        Debug.Log($"User ID: {(authData.UserData.ContainsKey("userId") ? authData.UserData["userId"] : "Not provided")}");
        Debug.Log($"App ID: {(authData.UserData.ContainsKey("appId") ? authData.UserData["appId"] : "Not provided")}");
        Debug.Log($"Package Name: {(authData.UserData.ContainsKey("packageName") ? authData.UserData["packageName"] : "Not provided")}");
        Debug.Log("=== AUTHENTICATION COMPLETED - MODULE INFORMATION ===");
        
        if (authData.Modules == null || authData.Modules.Count == 0)
        {
            Debug.Log("No modules defined.");
            Debug.Log("=== END MODULE INFORMATION ===");
            return;
        }

        Abxr.ExecuteModuleSequence(this, "Module_");
        Debug.Log("=== END MODULE INFORMATION ===");
    }
    private void Module_b787_baggage_load()
    {
        Debug.Log("Entered module: b787-baggage-load");
        //Debug.Log($"  - UserData: {(Abxr.GetUserData() != null ? JsonConvert.SerializeObject(Abxr.GetUserData()) : "None")}");
        Debug.Log("Completed module: b787-baggage-load");
    }

    private void Module_b787_refuel()
    {
        Debug.Log("Entered module: b787-refuel");
        //Debug.Log($"  - UserData: {(Abxr.GetUserData() != null ? JsonConvert.SerializeObject(Abxr.GetUserData()) : "None")}");
        Debug.Log("Completed module: b787-refuel");
    }

    private void Module_b787_baggage_unload()
    {
        Debug.Log("Entered module: b787-baggage-unload");
        //Debug.Log($"  - UserData: {(Abxr.GetUserData() != null ? JsonConvert.SerializeObject(Abxr.GetUserData()) : "None")}");
        Debug.Log("Completed module: b787-baggage-unload)");
    }

    
    #region Android Deep Link Integration (for external Android deep links)
    
    private void InitializeAndroidDeepLinkHandler()
    {
        // Subscribe to Android deep link events (not from moduleTarget)
        DeepLinkHandler.OnDeepLinkReceived += OnDeepLinkReceived;
        
        // Initialize the Android deep link handler singleton
        DeepLinkHandler.Instance.enabled = true;
        
        Debug.Log("LevelManager: Android deep link handler initialized");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from Android deep link events
        DeepLinkHandler.OnDeepLinkReceived -= OnDeepLinkReceived;
        
        // Unsubscribe from AbxrLib moduleTarget deep link events
        Abxr.OnModuleTargetDeepLink -= OnDeepLinkReceived;
    }
    
    private void OnDeepLinkReceived(string moduleName)
    {
        Debug.Log($"LevelManager: Deep link received for module: {moduleName}");
        
        // Map module names to actual module methods
        switch (moduleName.ToLower())
        {
            case "b787_baggage_load":
                Debug.Log("LevelManager: Executing b787_baggage_load module via deep link");
                Module_b787_baggage_load();
                break;
                
            case "b787_refuel":
                Debug.Log("LevelManager: Executing b787_refuel module via deep link");
                Module_b787_refuel();
                break;
                
            case "b787_baggage_unload":
                Debug.Log("LevelManager: Executing b787_baggage_unload module via deep link");
                Module_b787_baggage_unload();
                break;
                
            default:
                Debug.LogWarning($"LevelManager: Unknown module name in deep link: {moduleName}");
                break;
        }
    }
    
    
    #endregion

}
