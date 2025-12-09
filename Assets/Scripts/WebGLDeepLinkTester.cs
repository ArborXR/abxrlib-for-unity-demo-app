using UnityEngine;

/// <summary>
/// Test script for WebGL deep link functionality
/// This script provides methods to test WebGL deep links in the browser
/// </summary>
public class WebGLDeepLinkTester : MonoBehaviour
{
    [Header("WebGL Deep Link Testing")]
    [SerializeField] private string testModuleName = "b787_baggage_load";
    
    private void Start()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("WebGLDeepLinkTester: WebGL deep link testing enabled");
        Debug.Log("WebGLDeepLinkTester: Test URLs:");
        Debug.Log($"  - Query parameter: {Application.absoluteURL}?module={testModuleName}");
        Debug.Log($"  - Path-based: {Application.absoluteURL.Replace("index.html", "demoapp/" + testModuleName)}");
        #endif
    }
    
    /// <summary>
    /// Test method to simulate a WebGL deep link
    /// Call this from browser console: gameInstance.SendMessage('WebGLDeepLinkTester', 'TestDeepLink', 'b787_refuel')
    /// </summary>
    public void TestDeepLink(string moduleName)
    {
        Debug.Log($"WebGLDeepLinkTester: Testing deep link for module: {moduleName}");
        
        // Simulate the deep link by directly calling the DeepLinkHandler
        if (DeepLinkHandler.Instance != null)
        {
            DeepLinkHandler.Instance.SimulateDeepLink(moduleName);
        }
        else
        {
            Debug.LogError("WebGLDeepLinkTester: DeepLinkHandler.Instance is null");
        }
    }
    
    /// <summary>
    /// Test method for the default test module
    /// </summary>
    public void TestDefaultDeepLink()
    {
        TestDeepLink(testModuleName);
    }
    
    /// <summary>
    /// Generate test URLs for WebGL deep links
    /// </summary>
    [ContextMenu("Generate Test URLs")]
    public void GenerateTestUrls()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        string baseUrl = Application.absoluteURL;
        if (baseUrl.Contains("?"))
        {
            baseUrl = baseUrl.Substring(0, baseUrl.IndexOf("?"));
        }
        
        Debug.Log("=== WebGL Deep Link Test URLs ===");
        Debug.Log($"Base URL: {baseUrl}");
        Debug.Log("");
        Debug.Log("Query Parameter Format:");
        Debug.Log($"  {baseUrl}?module=b787_baggage_load");
        Debug.Log($"  {baseUrl}?module=b787_refuel");
        Debug.Log($"  {baseUrl}?module=b787_baggage_unload");
        Debug.Log("");
        Debug.Log("Path-based Format:");
        Debug.Log($"  {baseUrl.Replace("index.html", "demoapp/b787_baggage_load")}");
        Debug.Log($"  {baseUrl.Replace("index.html", "demoapp/b787_refuel")}");
        Debug.Log($"  {baseUrl.Replace("index.html", "demoapp/b787_baggage_unload")}");
        Debug.Log("=================================");
        #else
        Debug.Log("WebGLDeepLinkTester: Test URL generation only available in WebGL builds");
        #endif
    }
}
