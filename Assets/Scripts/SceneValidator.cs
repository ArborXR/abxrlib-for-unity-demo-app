using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEditor;

/// <summary>
/// Validates and fixes common scene issues automatically
/// </summary>
public class SceneValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    [SerializeField] private bool validateOnStart = true;
    [SerializeField] private bool autoFixIssues = true;
    [SerializeField] private bool logValidationResults = true;

    private void Start()
    {
        if (validateOnStart)
        {
            ValidateScene();
        }
    }

    [ContextMenu("Validate Scene")]
    public void ValidateScene()
    {
        if (logValidationResults)
            Debug.Log("SceneValidator: Starting scene validation...");

        bool hasIssues = false;

        // Check for missing script references
        hasIssues |= FixMissingScriptReferences();

        // Check for ExitCube conflicts
        hasIssues |= FixExitCubeIssues();

        // Check for required components
        hasIssues |= ValidateRequiredComponents();

        if (logValidationResults)
        {
            if (hasIssues)
                Debug.Log("SceneValidator: Scene validation completed with issues found and fixed.");
            else
                Debug.Log("SceneValidator: Scene validation completed - no issues found.");
        }
    }

    private bool FixMissingScriptReferences()
    {
        bool hasIssues = false;
        
        // Find all GameObjects with missing script components
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            Component[] components = obj.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null)
                {
                    if (logValidationResults)
                        Debug.LogWarning($"SceneValidator: Found missing script reference on {obj.name}");
                    
                    if (autoFixIssues)
                    {
                        // Remove the missing script component
                        #if UNITY_EDITOR
                        SerializedObject serializedObject = new SerializedObject(obj);
                        SerializedProperty property = serializedObject.FindProperty("m_Component");
                        
                        for (int i = 0; i < property.arraySize; i++)
                        {
                            SerializedProperty element = property.GetArrayElementAtIndex(i);
                            if (element.objectReferenceValue == null)
                            {
                                property.DeleteArrayElementAtIndex(i);
                                hasIssues = true;
                                break;
                            }
                        }
                        
                        serializedObject.ApplyModifiedProperties();
                        #endif
                    }
                    else
                    {
                        hasIssues = true;
                    }
                }
            }
        }
        
        return hasIssues;
    }

    private bool FixExitCubeIssues()
    {
        bool hasIssues = false;
        
        GameObject exitCube = GameObject.Find("ExitCube");
        if (exitCube == null)
        {
            if (logValidationResults)
                Debug.LogWarning("SceneValidator: ExitCube not found in scene");
            return false;
        }

        // Check for duplicate interactable components
        ExitButton exitButton = exitCube.GetComponent<ExitButton>();
        XRSimpleInteractable simpleInteractable = exitCube.GetComponent<XRSimpleInteractable>();

        if (exitButton != null && simpleInteractable != null)
        {
            if (logValidationResults)
                Debug.LogWarning("SceneValidator: ExitCube has both ExitButton and XRSimpleInteractable - removing XRSimpleInteractable");
            
            if (autoFixIssues)
            {
                DestroyImmediate(simpleInteractable);
                hasIssues = true;
            }
            else
            {
                hasIssues = true;
            }
        }

        // Ensure ExitCube has a collider
        Collider exitCollider = exitCube.GetComponent<Collider>();
        if (exitCollider == null)
        {
            if (logValidationResults)
                Debug.LogWarning("SceneValidator: ExitCube missing collider - adding BoxCollider");
            
            if (autoFixIssues)
            {
                BoxCollider boxCollider = exitCube.AddComponent<BoxCollider>();
                boxCollider.size = Vector3.one;
                boxCollider.isTrigger = false;
                hasIssues = true;
            }
            else
            {
                hasIssues = true;
            }
        }

        return hasIssues;
    }

    private bool ValidateRequiredComponents()
    {
        bool hasIssues = false;

        // Check for GrabbableObjectManager
        if (FindObjectOfType<GrabbableObjectManager>() == null)
        {
            if (logValidationResults)
                Debug.LogError("SceneValidator: GrabbableObjectManager not found in scene - this is required!");
            hasIssues = true;
        }

        // Check for LevelManager
        if (FindObjectOfType<LevelManager>() == null)
        {
            if (logValidationResults)
                Debug.LogError("SceneValidator: LevelManager not found in scene - this is required!");
            hasIssues = true;
        }

        return hasIssues;
    }

    [ContextMenu("Fix ExitCube Issues")]
    public void FixExitCubeIssuesOnly()
    {
        FixExitCubeIssues();
    }

    [ContextMenu("Fix Missing Script References")]
    public void FixMissingScriptReferencesOnly()
    {
        FixMissingScriptReferences();
    }
} 