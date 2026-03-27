using System.Collections.Generic;
using UnityEngine;

public class Dropper : MonoBehaviour
{
    private List<GrabbableObjectManager.GrabbableObjectType> queue;
    public float delay = 10;
    public float delayRange = 3;
    public float downForce = 10;
    private float currentDelay = 0;
    private float totalTime = 0;
    private float lastTime = 0;

    /// <summary>
    /// When true, fruit spawning and level-start analytics wait until <see cref="BeginGameplayAfterAuth"/> is called
    /// (e.g. from LevelManager after Abxr.OnAuthCompleted). Disable in the Inspector for scenes that do not gate on auth.
    /// </summary>
    [Tooltip("If enabled, spawning stays off until BeginGameplayAfterAuth() runs (after authentication).")]
    public bool deferSpawningUntilAuth = true;

    private bool _gameplayStarted;

    // Start is called before the first frame update
    void Start()
    {
        queue = new List<GrabbableObjectManager.GrabbableObjectType>();
        TargetLocation[] targetLocations = GameObject.FindObjectsByType<TargetLocation>(FindObjectsSortMode.None);
        Abxr.LogInfo("Dropper started (Dropper)");
        Debug.Log($"Found {targetLocations.Length} target locations");
        Abxr.LogInfo(targetLocations.Length.ToString());
        foreach (TargetLocation targetLocation in targetLocations)
        {
            queue.Add(targetLocation.targetType);
            //Debug.Log(targetLocation.targetType);
        }

        SetDelay(2f);

        if (!deferSpawningUntilAuth)
        {
            Abxr.EventLevelStart("1", new Abxr.Dict { ["scriptName"] = "Dropper" });
            _gameplayStarted = true;
        }
    }

    /// <summary>
    /// Starts the drop timer and level-start event after authentication completes. No-op if already started or defer is off.
    /// </summary>
    public void BeginGameplayAfterAuth()
    {
        if (!deferSpawningUntilAuth || _gameplayStarted)
            return;

        _gameplayStarted = true;
        Abxr.EventLevelStart("1", new Abxr.Dict { ["scriptName"] = "Dropper" });
        lastTime = totalTime;
        SetDelay(2f);
    }

    // Update is called once per frame
    void Update()
    {
        if (deferSpawningUntilAuth && !_gameplayStarted)
            return;

        totalTime += Time.deltaTime;
        if (totalTime > lastTime + currentDelay)
        {
            SetDelay();
            lastTime = totalTime;
            SpawnRandom();
        }
    }

    private void SetDelay()
    {
        currentDelay = delay + Random.Range(-1f, 1f) * delayRange;
    }

    public void SetDelay(float fixedDelay)
    {
        currentDelay = fixedDelay;
    }

    public void SpawnRandom()
    {
        if (queue.Count == 0) return;

        List<GrabbableObjectManager.GrabbableObjectType> uniqueValues = GetUniqueValues(queue);
        int index = Random.Range(0, uniqueValues.Count);
        GrabbableObjectManager.GrabbableObjectType type = uniqueValues[index];
        Remove(type);
        // Debug.Log(type);
        GameObject obj = GrabbableObjectManager.getInstance().CreateGrabbableObject(type, this.transform);
        obj.GetComponent<Rigidbody>().AddForce(Vector3.down * downForce);
        obj.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    // Gets rid of A, adds B
    public void Replace(GrabbableObjectManager.GrabbableObjectType a, GrabbableObjectManager.GrabbableObjectType b)
    {
        Remove(a);
        Add(b);
    }

    public void Add(GrabbableObjectManager.GrabbableObjectType type)
    {
        queue.Add(type);
    }

    public void Remove(GrabbableObjectManager.GrabbableObjectType type)
    {
        queue.Remove(type);
    }

    private List<T> GetUniqueValues<T>(List<T> list)
    {
        List<T> uniqueValues = new List<T>();
        foreach (T entry in list)
            if (!uniqueValues.Contains(entry))
                uniqueValues.Add(entry);
        return uniqueValues;
    }

    public void ResetDropper()
    {
        queue.Clear();
        SetDelay();
    }
}
