using UnityEngine;
using System;

public class DeepLinkHandler : MonoBehaviour
    {
        public static event Action<string> OnDeepLinkReceived;
        
        private static DeepLinkHandler _instance;
        
        public static DeepLinkHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("DeepLinkHandler");
                    _instance = go.AddComponent<DeepLinkHandler>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Initialize the deep link handler
            InitializeDeepLinkHandler();
        }
        
        private void InitializeDeepLinkHandler()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Get the intent data from Android
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");
            
            if (intent != null)
            {
                string action = intent.Call<string>("getAction");
                string data = intent.Call<string>("getDataString");
                
                if (action == "android.intent.action.VIEW" && !string.IsNullOrEmpty(data))
                {
                    Debug.Log($"DeepLinkHandler: Received deep link on startup: {data}");
                    ProcessDeepLink(data);
                }
            }
            #else
            Debug.Log("DeepLinkHandler: Running in editor - deep links will be simulated");
            #endif
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus) // App is resuming
            {
                #if UNITY_ANDROID && !UNITY_EDITOR
                CheckForNewIntent();
                #endif
            }
        }
        
        private void CheckForNewIntent()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");
                
                if (intent != null)
                {
                    string action = intent.Call<string>("getAction");
                    string data = intent.Call<string>("getDataString");
                    
                    if (action == "android.intent.action.VIEW" && !string.IsNullOrEmpty(data))
                    {
                        Debug.Log($"DeepLinkHandler: Received deep link on resume: {data}");
                        ProcessDeepLink(data);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"DeepLinkHandler: Error checking for new intent: {e.Message}");
            }
            #endif
        }
        
        private void ProcessDeepLink(string deepLink)
        {
            Debug.Log($"DeepLinkHandler: Processing deep link: {deepLink}");
            
            try
            {
                // Parse the deep link URL
                Uri uri = new Uri(deepLink);
                string moduleName = null;
                
                // Handle custom scheme: demoapp://module/b787_baggage_load
                if (uri.Scheme == "demoapp" && uri.Host == "module")
                {
                    string path = uri.AbsolutePath.TrimStart('/');
                    moduleName = path;
                }
                // Handle HTTP/HTTPS scheme: https://your-domain.com/demoapp/b787_baggage_load
                else if ((uri.Scheme == "http" || uri.Scheme == "https") && uri.AbsolutePath.StartsWith("/demoapp/"))
                {
                    string path = uri.AbsolutePath.Substring(6); // Remove "/demoapp/"
                    moduleName = path;
                }
                
                if (!string.IsNullOrEmpty(moduleName))
                {
                    Debug.Log($"DeepLinkHandler: Extracted module name: {moduleName}");
                    OnDeepLinkReceived?.Invoke(moduleName);
                }
                else
                {
                    Debug.LogWarning($"DeepLinkHandler: Could not extract module name from: {deepLink}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"DeepLinkHandler: Error processing deep link: {e.Message}");
            }
        }
        
        // Public method to simulate deep links in editor
        public void SimulateDeepLink(string moduleName)
        {
            #if UNITY_EDITOR
            Debug.Log($"DeepLinkHandler: Simulating deep link for module: {moduleName}");
            OnDeepLinkReceived?.Invoke(moduleName);
            #endif
        }
    }
