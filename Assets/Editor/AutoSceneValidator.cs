using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatically validates and fixes scene issues when Unity loads or scenes change.
/// This runs in the Editor and doesn't rely on scene-based MonoBehaviour components,
/// so it works even when GUIDs are out of sync after fresh git checkouts.
/// </summary>
[InitializeOnLoad]
public static class AutoSceneValidator
{
    private static bool hasRunForCurrentSession = false;

    static AutoSceneValidator()
    {
        // Run validation when Unity starts up
        EditorApplication.delayCall += OnEditorStartup;
        
        // Run validation when scenes are loaded
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnEditorStartup()
    {
        if (!hasRunForCurrentSession)
        {
            hasRunForCurrentSession = true;
            
            // Longer delay to ensure Unity is fully ready for serialization changes
            EditorApplication.delayCall += () => {
                EditorApplication.delayCall += () => {
                    //Debug.Log("AutoSceneValidator: Unity startup - checking for missing script issues...");
                    ValidateCurrentScene();
                };
            };
        }
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        Debug.Log($"AutoSceneValidator: Scene '{scene.name}' opened - validating...");
        EditorApplication.delayCall += () => ValidateCurrentScene();
    }
    


    private static void ValidateCurrentScene()
    {
        if (!Application.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            // Ensure we're not in the middle of asset refresh or other Unity operations
            if (EditorApplication.timeSinceStartup < 2.0f)
            {
                // Too early in Unity startup, retry later
                EditorApplication.delayCall += () => ValidateCurrentScene();
                return;
            }
            
            try
            {
                bool foundIssues = FixMissingScriptReferences();
                
                if (foundIssues)
                {
                    Debug.Log("AutoSceneValidator: Found and reconnected missing scripts. Cleaning up broken references...");
                    
                    // Use Unity's built-in cleanup - it's cleaner and doesn't cause serialization errors
                    TryBuiltInCleanup();
                    
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
                else
                {
                    //Debug.Log("AutoSceneValidator: No missing script references found.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"AutoSceneValidator: Validation failed, will retry later: {e.Message}");
                // Retry after a longer delay
                EditorApplication.delayCall += () => {
                    EditorApplication.delayCall += () => ValidateCurrentScene();
                };
            }
        }
        else if (!Application.isPlaying)
        {
            // Unity is busy, retry later
            EditorApplication.delayCall += () => ValidateCurrentScene();
        }
    }

    private static bool FixMissingScriptReferences()
    {
        bool foundIssues = false;
        
        // Build a lookup of available MonoScripts by GUID
        var scriptLookup = BuildScriptGuidLookup();
        
        // Find all GameObjects with missing script components
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            Component[] components = obj.GetComponents<Component>();
            
            // Check for missing script components
            bool hasMissingComponents = false;
            foreach (Component component in components)
            {
                if (component == null)
                {
                    hasMissingComponents = true;
                    break;
                }
            }
            
            if (hasMissingComponents)
            {
                // Try to reconnect all expected scripts for this object type
                TryReconnectAllExpectedScripts(obj, scriptLookup);
                foundIssues = true;
            }
        }
        
        return foundIssues;
    }
    
    /// <summary>
    /// Tries to reconnect all expected scripts for a given GameObject based on its name/type
    /// </summary>
    private static void TryReconnectAllExpectedScripts(GameObject obj, System.Collections.Generic.Dictionary<string, MonoScript> scriptLookup)
    {
        string objName = obj.name.ToLower();
        System.Collections.Generic.List<System.Type> expectedScriptTypes = new System.Collections.Generic.List<System.Type>();
        
        // Determine what scripts this object should have based on its name
        if (objName.Contains("exitcube") || objName.Contains("exit"))
        {
            expectedScriptTypes.Add(typeof(ExitButton));
        }
        else if (objName.Contains("resetcube") || objName.Contains("reset"))
        {
            expectedScriptTypes.Add(typeof(ResetButton));
        }
        else if (objName.Contains("reauthcube") || objName.Contains("reauth"))
        {
            expectedScriptTypes.Add(typeof(ReAuthenticateButton));
        }
        else if (objName.Contains("player") || objName.Contains("xr"))
        {
            // Player objects can have multiple scripts
            expectedScriptTypes.Add(typeof(PlayerOrientationFix));
            expectedScriptTypes.Add(typeof(DesktopInputController));
        }
        
        // Try to reconnect each expected script type
        foreach (System.Type scriptType in expectedScriptTypes)
        {
            // Only add if the component doesn't already exist
            if (obj.GetComponent(scriptType) == null)
            {
                string scriptGuid = GetScriptGuidByType(scriptType);
                if (!string.IsNullOrEmpty(scriptGuid) && scriptLookup.ContainsKey(scriptGuid))
                {
                    MonoScript foundScript = scriptLookup[scriptGuid];
                    if (TryReconnectScript(obj, foundScript, scriptGuid))
                    {
                        Debug.Log($"AutoSceneValidator: Reconnected missing script '{scriptType.Name}' on '{obj.name}'");
                    }
                    else
                    {
                        Debug.Log($"AutoSceneValidator: Script '{scriptType.Name}' already exists on '{obj.name}'");
                    }
                }
            }
        }
    }
    
    private static void TryBuiltInCleanup()
    {
        if (!Application.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            int totalCleaned = 0;
            
            foreach (GameObject obj in allObjects)
            {
                try
                {
                    int before = obj.GetComponents<Component>().Length;
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                    int after = obj.GetComponents<Component>().Length;
                    
                    int cleaned = before - after;
                    if (cleaned > 0)
                    {
                        totalCleaned += cleaned;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"AutoSceneValidator: Cleanup failed for '{obj.name}': {e.Message}");
                }
            }
            
            if (totalCleaned > 0)
            {
                Debug.Log($"AutoSceneValidator: Cleaned up {totalCleaned} missing script references");
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }
    }
    
    private static System.Collections.Generic.Dictionary<string, MonoScript> BuildScriptGuidLookup()
    {
        var lookup = new System.Collections.Generic.Dictionary<string, MonoScript>();
        
        string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
        foreach (string guid in scriptGuids)
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
            if (script != null && script.GetClass() != null)
            {
                lookup[guid] = script;
            }
        }
        
        return lookup;
    }
    
    private static string TryGetMissingScriptGuid(GameObject obj, int componentIndex)
    {
        try
        {
            // Dynamic approach - find GUIDs by script type names based on object patterns
            string objName = obj.name.ToLower();
            
            // Map common object names to expected script types
            System.Type expectedScriptType = null;
            
            if (objName.Contains("exitcube") || objName.Contains("exit"))
                expectedScriptType = typeof(ExitButton);
            else if (objName.Contains("resetcube") || objName.Contains("reset"))
                expectedScriptType = typeof(ResetButton);
            else if (objName.Contains("reauthcube") || objName.Contains("reauth"))
                expectedScriptType = typeof(ReAuthenticateButton);
            else if (objName.Contains("player") || objName.Contains("xr"))
            {
                // Player can have multiple scripts - check which ones are missing
                if (obj.GetComponent<PlayerOrientationFix>() == null)
                    expectedScriptType = typeof(PlayerOrientationFix);
                else if (obj.GetComponent<DesktopInputController>() == null)
                    expectedScriptType = typeof(DesktopInputController);
            }
            
            // If we found an expected script type, get its GUID dynamically
            if (expectedScriptType != null)
            {
                return GetScriptGuidByType(expectedScriptType);
            }
                
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Dynamically finds the GUID of a MonoScript by its Type
    /// This works even if .meta files are regenerated
    /// </summary>
    private static string GetScriptGuidByType(System.Type scriptType)
    {
        try
        {
            // Search for the MonoScript asset by type name
            string[] scriptGuids = AssetDatabase.FindAssets($"t:MonoScript {scriptType.Name}");
            
            foreach (string guid in scriptGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                
                if (script != null && script.GetClass() == scriptType)
                {
                    return guid;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"AutoSceneValidator: Could not find GUID for script type {scriptType?.Name}: {e.Message}");
        }
        
        return null;
    }
    
    private static bool TryReconnectScript(GameObject obj, MonoScript script, string guid)
    {
        try
        {
            System.Type scriptType = script.GetClass();
            if (scriptType != null)
            {
                // Check if the component already exists
                Component existing = obj.GetComponent(scriptType);
                if (existing == null)
                {
                    // Add the component
                    obj.AddComponent(scriptType);
                    return true;
                }
                else
                {
                    // Component already exists - don't add duplicate
                    return false;
                }
            }
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AutoSceneValidator: Failed to reconnect script {script.name}: {e.Message}");
            return false;
        }
    }

    // Manual validation menu item for troubleshooting
    [MenuItem("Tools/Validate Current Scene (Auto)")]
    public static void ManualValidation()
    {
        Debug.Log("AutoSceneValidator: Manual validation requested...");
        ValidateCurrentScene();
    }
    
    // Aggressive cleanup that uses Unity's built-in missing script removal
    [MenuItem("Tools/Force Clean Missing Scripts")]
    public static void ForceCleanMissingScripts()
    {
        Debug.Log("AutoSceneValidator: Force cleaning all missing script references...");
        
        // Get all GameObjects in the scene
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        int totalCleaned = 0;
        
        foreach (GameObject obj in allObjects)
        {
            // Use Unity's built-in method to remove missing scripts
            int before = obj.GetComponents<Component>().Length;
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            int after = obj.GetComponents<Component>().Length;
            
            int cleaned = before - after;
            if (cleaned > 0)
            {
                Debug.Log($"AutoSceneValidator: Removed {cleaned} missing script(s) from '{obj.name}'");
                totalCleaned += cleaned;
            }
        }
        
        if (totalCleaned > 0)
        {
            Debug.Log($"AutoSceneValidator: Force cleanup complete - removed {totalCleaned} missing script references total");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        else
        {
            Debug.Log("AutoSceneValidator: No missing script references found to clean");
        }
    }
}