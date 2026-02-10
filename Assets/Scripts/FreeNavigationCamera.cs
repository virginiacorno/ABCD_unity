using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class FreeNavigationCamera : MonoBehaviour
{
#if UNITY_WEBGL
    private void InitLogger(string p, string s, string sess) => WebDataLogger.Instance.InitializeWithInfo(p, s, sess);
#else
    private void InitLogger(string p, string s, string sess) => DataLogger.Instance.InitializeWithInfo(p, s, sess);
#endif

    public Camera firstPersonCamera;
    public Camera miniMapCamera;
    public GameObject player;
    public rewardManager rewardManager;
    
    public void StartNewConfiguration(int configIndex)
    {
        //V: test data logging
        InitLogger("TEST_P001", "pilot_study", "001");
        
        //V: Load the new configuration in reward manager
        rewardManager.LoadConfiguration(configIndex);
        SetupGameplayCameras();
    }

    void SetupGameplayCameras()
    {
        firstPersonCamera.enabled = true;
        miniMapCamera.enabled = true;
        
        //V: Mini-map in top-right corner
        miniMapCamera.rect = new Rect(0.75f, 0.75f, 0.25f, 0.25f);
        miniMapCamera.depth = 1; 
    }

    public void DisableMiniMap()
    {
        Debug.Log("DisableMiniMap() called");
        Debug.Log($"miniMapCamera is null: {miniMapCamera == null}");
        Debug.Log($"miniMapCamera.enabled: {miniMapCamera != null && miniMapCamera.enabled}");
        
        if (miniMapCamera != null && miniMapCamera.enabled)
        {
            miniMapCamera.enabled = false;
            Debug.Log("Minimap disabled");
        }
        else
        {
            Debug.Log("Minimap was already disabled or is null");
        }
    }
}
