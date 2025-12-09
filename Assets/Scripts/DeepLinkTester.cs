using UnityEngine;

public class DeepLinkTester : MonoBehaviour
    {
        [Header("Deep Link Testing")]
        [SerializeField] private bool enableTesting = true;
        
        private void OnGUI()
        {
            if (!enableTesting) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Deep Link Tester", GUI.skin.box);
            
            if (GUILayout.Button("Test: b787_baggage_load"))
            {
                DeepLinkHandler.Instance.SimulateDeepLink("b787_baggage_load");
            }
            
            if (GUILayout.Button("Test: b787_refuel"))
            {
                DeepLinkHandler.Instance.SimulateDeepLink("b787_refuel");
            }
            
            if (GUILayout.Button("Test: b787_baggage_unload"))
            {
                DeepLinkHandler.Instance.SimulateDeepLink("b787_baggage_unload");
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Real Deep Links:", GUI.skin.box);
            GUILayout.Label("demoapp://module/b787_baggage_load");
            GUILayout.Label("demoapp://module/b787_refuel");
            GUILayout.Label("demoapp://module/b787_baggage_unload");
            
            GUILayout.EndArea();
        }
    }
