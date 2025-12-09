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
	private System.Threading.Thread _thread = null;
	private bool m_bInvokeSucceeded = false;

    private void Start()
    {
        Debug.Log("LevelManager: Start()");
        //Debug.Log("LevelManager: WhatTimeIsIt: " + Abxr.WhatTimeIsIt());
        //Debug.Log("LevelManager: DeviceId: " + Abxr.GetDeviceId());
        //Debug.Log("LevelManager: DeviceSerial: " + Abxr.GetDeviceSerial());
        //Debug.Log("LevelManager: DeviceTitle: " + Abxr.GetDeviceTitle());
        // Debug.Log("LevelManager: DeviceTags: " + Abxr.GetDeviceTags());
        // Debug.Log("LevelManager: OrgId: " + Abxr.GetOrgId());
        // Debug.Log("LevelManager: OrgTitle: " + Abxr.GetOrgTitle());
        // Debug.Log("LevelManager: OrgSlug: " + Abxr.GetOrgSlug());        

        // Initialize Android deep link handler for external deep links (not moduleTarget)
        InitializeAndroidDeepLinkHandler();
        
        InitializeGame();
        InvokeRepeating(nameof(CheckRunTime), 0, 300); // Call every 5 minutes
        InvokeRepeating(nameof(TestCheck), 0, 30); // Call every 30 seconds
        
        // Set up authentication completion callback to log module information
        // See OnAuthenticationCompleted below for authentication completion callback
        Abxr.OnAuthCompleted += OnAuthenticationCompleted;
        // Subscribe to AbxrLib's moduleTarget event
        Abxr.OnModuleTarget += OnDeepLinkReceived;
        
        Abxr.EventAssessmentStart("stocking_training_unit_1");

		// Set up authentication completion callback to log module information
		//Abxr.OnAuthCompleted(OnAuthenticationCompleted);
		//See OnAuthenticationCompleted below for authentication completion callback
		// ---
		Debug.Log("AbxrLib[AbxrInsightServiceClient] - About to start the thread that is going to wait for service to be not null and then call whatTimeIsIt()");
		_thread = new System.Threading.Thread(ThreadMain);
		if (_thread != null)
		{
			Debug.Log("AbxrLib[AbxrInsightServiceClient] - The thread got created successfully, now let us start it.");
			_thread.Start(this);
		}
		else
		{
			Debug.Log("AbxrLib[AbxrInsightServiceClient] - ERROR, the thread didn't start.");
		}
	}
	void InvokeWhenServiceIsThere()
	{
		Debug.Log("AbxrLib[AbxrInsightServiceClient] - SUCCESS, the service exists, going to call whatTimeIsIt() and then bail this thread... drumroll please, whatTimeIsIt() returned " + Abxr.WhatTimeIsIt());
	}
	void InvokeWhenServiceIsNull()
	{
		Debug.Log("AbxrLib[AbxrInsightServiceClient] - Abxr.IsServiceAvailable() returned false but there is a point where the service variable is indeed not null so let us call the bloody thing anyway... whatTimeIsIt() returned " + Abxr.WhatTimeIsIt());
	}
	void InvokeWhenServiceIsWhateverItIs()
	{
		if (Abxr.ServiceIsFullyInitialized())
		{
			Debug.Log("AbxrLib[AbxrInsightServiceClient] - SUCCESS, the service exists, going to call whatTimeIsIt() and then bail this thread... drumroll please, whatTimeIsIt() returned " + Abxr.WhatTimeIsIt());
			// ---
			m_bInvokeSucceeded = true;
		}
		else
		{
			//Debug.Log("AbxrLib[AbxrInsightServiceClient] - Abxr.IsServiceAvailable() returned false but there is a point where the service variable is indeed not null so let us call the bloody thing anyway... whatTimeIsIt() returned " + Abxr.WhatTimeIsIt());
			Debug.Log("AbxrLib[AbxrInsightServiceClient] - ERROR, service still does not exist yet so not going to call whatTimeIsit().");
		}
	}
	static void ThreadMain(object pThis)
	{
		for (;;)
		{
			System.Threading.Thread.Sleep(1000);
			//if (Abxr.IsServiceAvailable())
			//{
			//	(pThis as LevelManager).Invoke("InvokeWhenServiceIsThere", 0.0f);
			//	//Debug.Log("AbxrLib[AbxrInsightServiceClient] - SUCCESS, the service exists, going to call whatTimeIsIt() and then bail this thread... drumroll please, whatTimeIsIt() returned " + Abxr.WhatTimeIsIt());
			//	break;
			//}
			//else
			//{
			//	(pThis as LevelManager).Invoke("InvokeWhenServiceIsNull", 0.0f);
			//	//Debug.Log("AbxrLib[AbxrInsightServiceClient] - Abxr.IsServiceAvailable() returned false but there is a point where the service variable is indeed not null so let us call the bloody thing anyway... whatTimeIsIt() returned " + Abxr.WhatTimeIsIt());
			//	//Debug.Log("AbxrLib[AbxrInsightServiceClient] - ERROR, service still does not exist yet so not going to call whatTimeIsit().");
			//}
			(pThis as LevelManager).Invoke("InvokeWhenServiceIsWhateverItIs", 0.0f);
			if ((pThis as LevelManager).m_bInvokeSucceeded)
			{
				Debug.Log("AbxrLib[AbxrInsightServiceClient] - Ok, so here we are after invoke succeeded.  There should be one of these as we are returning from the thread right here.");
				return;
			}
		}
	}
	private void CheckForCompletion()
    {
        if (_completedTargets >= _totalTargets)
        {
            //Without meta data
            //Abxr.EventAssessmentComplete("stocking_training_unit_1", $"{score}", result: score > passingScore ? Abxr.EventStatus.Pass : Abxr.EventStatus.Fail);

            //With meta data
            var assessmentMetadata = new Abxr.Dict
            {
                ["mode"] = "easy",
                ["touched_floor"] = "true"
            };
            Abxr.EventAssessmentComplete("stocking_training_unit_1", $"{score}", result: score > passingScore ? Abxr.EventStatus.Pass : Abxr.EventStatus.Fail, meta: assessmentMetadata);
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
        Abxr.LogCritical("LevelManager: CheckRunTime() - Spending way too much time sorting fruit! This is not that hard a task!");
    }

    private void TestCheck()
    {
        Abxr.LogError("LevelManager: TestCheck() - Bad Luck, Description: We rolled the dice for fun and found you lost! This is mostly just for testing purposes.");
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
        Debug.Log("LevelManager: CompleteTask() - Placement Attempted");

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
            Abxr.EventInteractionComplete("toggle_button_second_action", Abxr.InteractionType.Text, Abxr.InteractionResult.Neutral, "Second action completed");
            //Abxr.EventInteractionComplete($"place_item_{objectId}", "False", "Wrong spot", Abxr.InteractionType.Bool, placementMetadata);
            //Abxr.EventInteractionComplete($"place_item_{objectId}", Abxr.InteractionType.Bool, Abxr.InteractionResult.Incorrect, "Wrong spot", placementMetadata);
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
            //Abxr.EventInteractionComplete($"place_item_{objectId}", Abxr.InteractionType.Bool, Abxr.InteractionResult.Correct, "Correct spot", placementMetadata);
            //Abxr.EventInteractionComplete($"place_item_{objectId}", "True", "Correct spot", Abxr.InteractionType.Bool, placementMetadata);

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
        if (authData.UserData != null)
        {
            Debug.Log($"User - ID: {(authData.UserData.ContainsKey("id") ? authData.UserData["id"] : "Not provided")}");
            Debug.Log($"User - Name: {(authData.UserData.ContainsKey("name") ? authData.UserData["name"] : "Not provided")}");
            Debug.Log($"User - user_id: {(authData.UserData.ContainsKey("user_id") ? authData.UserData["user_id"] : "Not provided")}");
            Debug.Log($"User - Email: {(authData.UserData.ContainsKey("email") ? authData.UserData["email"] : "Not provided")}");
            Debug.Log($"User ID: {(authData.UserData.ContainsKey("userId") ? authData.UserData["userId"] : "Not provided")}");
            Debug.Log($"App ID: {(authData.UserData.ContainsKey("appId") ? authData.UserData["appId"] : "Not provided")}");
            Debug.Log($"Package Name: {(authData.UserData.ContainsKey("packageName") ? authData.UserData["packageName"] : "Not provided")}");
        }
        else
        {
            Debug.Log("User data is not available in authentication response");
        }
        Debug.Log("=== AUTHENTICATION COMPLETED ===");
        Debug.Log("=== MODULE INFORMATION ===");
        
        if (authData.Modules == null || authData.Modules.Count == 0)
        {
            Debug.Log("No modules defined.");
        } else {
            Debug.Log("Modules defined, will execute next.");
        }
        Debug.Log("=== END MODULE INFORMATION ===");
        return;
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
        
        // Unsubscribe from AbxrLib moduleTarget events
        Abxr.OnModuleTarget -= OnDeepLinkReceived;
    }
    
    private void OnDeepLinkReceived(string deepLink)
    {
        Debug.Log($"LevelManager: Deep link received for module: {deepLink}");
        
        // Map module names to actual module methods
        switch (deepLink.ToLower())
        {
            case "b787_baggage_load":
            case "b787-baggage-load":
                Debug.Log("LevelManager: Executing b787_baggage_load module via deep link");
                Module_b787_baggage_load();
                break;
                
            case "b787_refuel":
            case "b787-refuel":
                Debug.Log("LevelManager: Executing b787_refuel module via deep link");
                Module_b787_refuel();
                break;
                
            case "b787_baggage_unload":
            case "b787-baggage-unload":
                Debug.Log("LevelManager: Executing b787_baggage_unload module via deep link");
                Module_b787_baggage_unload();
                break;
                
            default:
                Debug.LogWarning($"LevelManager: Unknown deep link: {deepLink}");
                break;
        }
    }
    
    
    #endregion

}
