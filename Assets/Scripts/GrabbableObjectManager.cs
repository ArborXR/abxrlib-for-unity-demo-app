using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GrabbableObjectManager : MonoBehaviour
{
    public GameObject grabbableObjectPrefab;
    [SerializeField]
    public List<GrabbableObjectData> grabbableObjectList;
    private static GrabbableObjectManager instance;

    [Serializable]
    public struct GrabbableObjectData
    {
        public GrabbableObjectType type;
        public GameObject model;
    }

    public enum GrabbableObjectType
    {
        Apple, Avocado, Bacon, Bag, Banana,
        Barrel, Beet, Ketchup, Mustard, Oil,
        Bowl, Bread, Broccoli, Burger, Cabbage,
        Cake, CakeSlicer, Can, CandyBar,
        SmallCan, Carrot, Carton, SmallCarton, Cauliflower,
        Celery, Cheese, CheseSlicer, Cherries, Chinese,
        Chocolate, Chopstick, Cocktail, Coconut, Cookie,
        Fork, Knife, Spatula, Spoon, Corn,
        Croissant, Cup, Cupcake, Saucer, Tea,
        Frappe, Fish, Fries, Loaf, Baguette,
        IceCream, RoundLoaf, Mug, Mortar, LollyPop,
        Plate, PizzaCutter, PizzaBox, Pineapple, Pan,
        Pancakes, SodaCan, SodaBottle, Whisk, Sundae
    }

    public static GrabbableObjectManager getInstance()
    {
        return instance;
    }

    public void Start()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            // Add any additional initialization here
        }
    }

    public GameObject CreateGrabbableObject(GrabbableObjectData grabbableObjectData) { return CreateGrabbableObject(grabbableObjectData, transform); }
    public GameObject CreateGrabbableObject(GrabbableObjectType grabbableObjectType) { return CreateGrabbableObject(grabbableObjectType, transform); }
    public GameObject CreateGrabbableObject(GrabbableObjectType grabbableObjectType, Transform transform) { return CreateGrabbableObject(getGrabbableObjectData(grabbableObjectType), transform); }
    public GameObject CreateGrabbableObject(GrabbableObjectData grabbableObjectData, Transform transform)
    {
        if (grabbableObjectPrefab == null)
        {
            Debug.LogError("GrabbableObjectManager: grabbableObjectPrefab is not assigned!");
            return null;
        }
        
        if (grabbableObjectData.model == null)
        {
            Debug.LogError($"GrabbableObjectManager: Model for {grabbableObjectData.type} is not assigned!");
            return null;
        }
        
        GameObject obj = Instantiate(grabbableObjectPrefab, transform);
        
        // Get mesh from the model (handles both .fbx assets and prefabs)
        Mesh modelMesh = GetMeshFromModel(grabbableObjectData.model);
        if (modelMesh == null)
        {
            Debug.LogError($"GrabbableObjectManager: Could not extract mesh from model {grabbableObjectData.type}");
            Destroy(obj);
            return null;
        }
        
        // Safely set MeshFilter
        MeshFilter objMeshFilter = obj.GetComponent<MeshFilter>();
        if (objMeshFilter != null)
        {
            objMeshFilter.mesh = modelMesh;
        }
        else
        {
            Debug.LogError($"GrabbableObjectManager: MeshFilter missing on prefab for {grabbableObjectData.type}");
            Destroy(obj);
            return null;
        }
        
        // Safely set MeshCollider
        MeshCollider objMeshCollider = obj.GetComponent<MeshCollider>();
        if (objMeshCollider != null)
        {
            objMeshCollider.sharedMesh = modelMesh;
        }
        else
        {
            Debug.LogWarning($"GrabbableObjectManager: MeshCollider missing on prefab for {grabbableObjectData.type}");
        }
        
        // Safely set GrabbableObject type
        GrabbableObject grabbableScript = obj.GetComponent<GrabbableObject>();
        if (grabbableScript != null)
        {
            grabbableScript.type = grabbableObjectData.type;
        }
        else
        {
            Debug.LogError($"GrabbableObjectManager: GrabbableObject script missing on prefab for {grabbableObjectData.type}");
        }
        
        // Get materials from the model
        Material[] modelMaterials = GetMaterialsFromModel(grabbableObjectData.model);
        MeshRenderer objMeshRenderer = obj.GetComponent<MeshRenderer>();
        if (objMeshRenderer != null && modelMaterials != null && modelMaterials.Length > 0)
        {
            objMeshRenderer.materials = modelMaterials;
        }
        else
        {
            Debug.LogWarning($"GrabbableObjectManager: Could not get materials from model {grabbableObjectData.type}, using default material");
        }
        // Get All Targets (runtime safety check for WebGL)
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            TargetLocation[] targetLocations = FindObjectsByType<TargetLocation>(FindObjectsSortMode.None);
            foreach (TargetLocation targetLocation in targetLocations)
            {
                var xrGrab = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>();
                if (xrGrab != null)
                {
                    xrGrab.selectExited.AddListener(interactable => targetLocation.OnRelease());
                }
            }
        }

        return obj;
    }
    
    /// <summary>
    /// Extracts mesh from a model GameObject (handles both prefabs and .fbx assets)
    /// </summary>
    private Mesh GetMeshFromModel(GameObject model)
    {
        if (model == null) return null;
        
        // Try to get MeshFilter from the model first (if it's a prefab)
        MeshFilter meshFilter = model.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return meshFilter.sharedMesh;
        }
        
        // If no MeshFilter, try to get it from children (common with .fbx imports)
        meshFilter = model.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return meshFilter.sharedMesh;
        }
        
        #if UNITY_EDITOR
        // Last resort: try to find mesh in the asset (for .fbx files) - Editor only
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(model);
        if (!string.IsNullOrEmpty(assetPath))
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        }
        #endif
        
        return null;
    }
    
    /// <summary>
    /// Extracts materials from a model GameObject
    /// </summary>
    private Material[] GetMaterialsFromModel(GameObject model)
    {
        if (model == null) return null;
        
        // Try to get MeshRenderer from the model first
        MeshRenderer meshRenderer = model.GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterials != null)
        {
            return meshRenderer.sharedMaterials;
        }
        
        // If no MeshRenderer, try to get it from children
        meshRenderer = model.GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterials != null)
        {
            return meshRenderer.sharedMaterials;
        }
        
        return null;
    }

    public GrabbableObjectData getGrabbableObjectData(GrabbableObjectType type)
    {
        foreach (GrabbableObjectData data in grabbableObjectList)
            if (data.type == type)
                return data;
        return new GrabbableObjectData();
    }
}
