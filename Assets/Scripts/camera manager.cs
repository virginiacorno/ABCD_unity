using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    //V: logger helpers — pick the right logger based on build target
#if UNITY_WEBGL
    private void LogData(System.Collections.Generic.Dictionary<string, object> data) => WebDataLogger.Instance.LogEvent(data);
    private float CurrentRunTime() => WebDataLogger.Instance.GetCurrentRunTime();
    private void InitLogger(string p, string s, string sess) => WebDataLogger.Instance.InitializeWithInfo(p, s, sess);
#else
    private void LogData(System.Collections.Generic.Dictionary<string, object> data) => DataLogger.Instance.LogEvent(data);
    private float CurrentRunTime() => DataLogger.Instance.GetCurrentRunTime();
    private void InitLogger(string p, string s, string sess) => DataLogger.Instance.InitializeWithInfo(p, s, sess);
#endif

    //V: create all necessary cameras
    public Camera firstPersonCamera;
    public Camera miniMapCamera;
    
    //V: create reward manager object for showing rewards
    public rewardManager rewardManager;
    
    //V: create player object
    public GameObject player;
    
    //V: specify timing variables
    public float[] rewardDisplayTime;
    public float[] pauseBetweenRewards;
    public float pauseBetweenSeq = 1f;
    
    [Header("Memorization Settings")]
    public int memorizationRepetitions = 2;  //V: how many times to show the sequence
    
    void Start()
    {
        //V: test data logging
        InitLogger("TEST_P001", "pilot_study", "001");

        //V: Initialize timing arrays
        rewardDisplayTime = new float[] {1.5f, 0.75f};
        pauseBetweenRewards = new float[] {0.5f, 0.25f};
        
        //V: Start with first configuration (index 0)
        StartNewConfiguration(0);
    }
    
    //V: Called when starting a new configuration (at start and after completing trials)
    public void StartNewConfiguration(int configIndex)
    {
        //V: Load the new configuration in reward manager
        rewardManager.LoadConfiguration(configIndex);

        //V: log start of the configuration
        LogData(new System.Collections.Generic.Dictionary<string, object>
        {
            {"event_type", "configuration_start"},
            {"config_index", configIndex},
            {"config_name", rewardManager.GetCurrentConfigName()},
            {"t_curr_run", CurrentRunTime()}
        });
        
        //V: Hide player and disable movement initially
        player.GetComponent<Renderer>().enabled = false;
        player.GetComponent<moveplayer>().enabled = false;
        
        //V: Setup camera for memorization phase
        SetupMemorizationCamera();
        
        Debug.Log($"Memorizing {rewardManager.GetCurrentConfigName()}: Watch the reward sequence!");
        
        //V: Start the coroutine to show rewards
        StartCoroutine(ShowRewardSequence());
    }
    
    void SetupMemorizationCamera()
    {
        rewardManager.HideCue();
        firstPersonCamera.enabled = false;
        miniMapCamera.enabled = true;
        
        //V: Put camera as full screen to show rewards
        miniMapCamera.rect = new Rect(0, 0, 1, 1);
        miniMapCamera.depth = 0;
    }
    
    void SetupGameplayCameras()
    {
        firstPersonCamera.enabled = true;
        miniMapCamera.enabled = true;
        
        //V: Mini-map in top-right corner
        miniMapCamera.rect = new Rect(0.75f, 0.75f, 0.25f, 0.25f);
        miniMapCamera.depth = 1; 
    }
    
    IEnumerator ShowRewardSequence()
    {
        LogData(new System.Collections.Generic.Dictionary<string, object>
        {
            {"event_type", "memorization_start"},
            {"config_name", rewardManager.GetCurrentConfigName()},
            {"repetitions", memorizationRepetitions},
            {"t_curr_run", CurrentRunTime()}
        });

        //V: Show sequence multiple times
        for (int repetition = 0; repetition < memorizationRepetitions; repetition++)
        {
            Debug.Log($"Showing sequence {repetition + 1}/{memorizationRepetitions}");
            LogData(new System.Collections.Generic.Dictionary<string, object>
            {
                {"event_type", "repetition_start"},
                {"repetition_number", repetition},
                {"display_time", rewardDisplayTime[repetition]},
                {"pause_time", pauseBetweenRewards[repetition]},
                {"t_curr_run", CurrentRunTime()}
            });

            //V: Show each of the 4 rewards in order
            for (int i = 0; i < 4; i++)
            {
                // Log when reward appears
                LogData(new System.Collections.Generic.Dictionary<string, object>
                {
                    {"event_type", "reward_onset"},
                    {"reward_letter", (char)('A' + i)},
                    {"reward_index", i},
                    {"repetition_number", repetition},
                    {"t_curr_run", CurrentRunTime()}
                });

                rewardManager.ShowReward(i);
                Debug.Log($"Reward {i + 1}/4");

                yield return new WaitForSeconds(rewardDisplayTime[repetition]);

                // Log when reward disappears
                LogData(new System.Collections.Generic.Dictionary<string, object>
                {
                    {"event_type", "reward_offset"},
                    {"reward_letter", (char)('A' + i)},
                    {"reward_index", i},
                    {"repetition_number", repetition},
                    {"t_curr_run", CurrentRunTime()}
                });

                rewardManager.HideReward(i);

                yield return new WaitForSeconds(pauseBetweenRewards[repetition]);
            }

            //V: Pause between repetitions (but not after the last one)
            if (repetition < memorizationRepetitions - 1)
            {
                yield return new WaitForSeconds(pauseBetweenSeq);
            }
        }

        Debug.Log("Memorization complete! Starting game...");

        LogData(new System.Collections.Generic.Dictionary<string, object>
        {
            {"event_type", "memorization_complete"},
            {"t_curr_run", CurrentRunTime()}
        });

        yield return new WaitForSeconds(1f);

        //V: Start gameplay phase
        StartGamePhase();
    }
    
    void StartGamePhase()
    {
        SetupGameplayCameras();
        
        player.GetComponent<Renderer>().enabled = true;
        player.GetComponent<moveplayer>().enabled = true;
        player.GetComponent<moveplayer>().inputEnabled = true;

        LogData(new System.Collections.Generic.Dictionary<string, object>
        {
            {"event_type", "game_phase_start"},
            {"start_loc_x", player.transform.position.x},
            {"start_loc_y", player.transform.position.z},
            {"config_name", rewardManager.GetCurrentConfigName()},
            {"t_curr_run", CurrentRunTime()}
        });
        
        Debug.Log("Find the rewards in order: A → B → C → D");
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