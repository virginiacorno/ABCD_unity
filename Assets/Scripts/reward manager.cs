using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; 

public class rewardManager : MonoBehaviour
{
    [System.Serializable]
    public class GridPosition
    {
        public float x;  // Unity X (left/right)
        public float y;  // Unity Y (height)
        public float z;  // Unity Z (forward/back)
        
        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
    
    [System.Serializable]
    public class RewardConfiguration
    {
        public string configName;
        public List<GridPosition> rewardPositions;

        //V: determine seqeunce length for the ABCD to ABC variant
        public int SequenceLength => configName.StartsWith("ABC") && !configName.StartsWith("ABCD") ? 3 : 4; //V: if the name starts with ABC and not ABCD, then length = 3, otherwise = 4
        public bool IsABCType => SequenceLength == 3;
    }
    
    [System.Serializable]
    public class ConfigurationData
    {
        public List<RewardConfiguration> configurations;
        public int trialsPerConfig;
    }
    
    [Header("Configuration File")]
    public TextAsset configurationFile;
    
    [Header("Reward Prefab")]
    public GameObject rewardPrefab;

    [Header("UI References")]
    public InstructionScreenManager instructionManager;
    
    private ConfigurationData configData;
    private GameObject[] currentRewardObjects; //V: array containing sequence of rewards
    private int currentConfigIdx = 0;
    private int nextRewardIdx = 0;
    private int repsCompleted = 0;
    private int lastShownRewardIdx = -1;
    private bool isFirstRepofConfig = true;
    public GameObject cueObject;

    public bool NewRewLocations() //V: checks if the reward locations have changed to call show rewards (needed in ABC version)
    {
        if (currentConfigIdx == 0)
            return true;  // first config always shows
        
        var currentPositions = configData.configurations[currentConfigIdx].rewardPositions;
        var previousPositions = configData.configurations[currentConfigIdx - 1].rewardPositions;
        
        // Compare each position
        for (int i = 0; i < 4; i++)
        {
            if (Mathf.Abs(currentPositions[i].x - previousPositions[i].x) > 0.01f ||
                Mathf.Abs(currentPositions[i].z - previousPositions[i].z) > 0.01f)
            {
                return true;  // Positions are different
            }
        }
        
        return false;  // Positions are the same
    }
    
    void Awake() //V: Awake() takes precedence over any Start() in any of the scripts, so we make sure all rewards are hidden before starting 
    {
        LoadConfigurationsFromFile();
        
        if (configData != null && configData.configurations.Count > 0)
        {
            LoadConfiguration(0);
            HideCue();
            Debug.Log("Awake complete - rewards created and hidden");
        }
        else
        {
            Debug.LogError("No configurations loaded!");
        }
    }

    void Start()
    {
        if (configData != null && configData.configurations.Count > 0)
        {
            Debug.Log($"Starting {configData.configurations[currentConfigIdx].configName}");
            Debug.Log($"Total configurations loaded: {configData.configurations.Count}");
        }
    }

    void LoadConfigurationsFromFile()
    {
        if (configurationFile == null)
        {
            Debug.LogError("Configuration file not assigned!");
            return;
        }
        
        try
        {
            configData = JsonUtility.FromJson<ConfigurationData>(configurationFile.text);
            Debug.Log($"Loaded {configData.configurations.Count} configurations from file");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load configuration file: {e.Message}");
        }
    }

    //V: need repetition to ensure some delay between end of previous trial and loading new configurations
    public void LoadConfiguration()
    {
        LoadConfiguration(currentConfigIdx);
    }

    public void LoadConfiguration(int index)
    {
        if (index >= 0 && index < configData.configurations.Count)
        {
            currentConfigIdx = index;
            
            // Destroy old rewards
            if (currentRewardObjects != null)
            {
                foreach (GameObject reward in currentRewardObjects)
                {
                    if (reward != null)
                        Destroy(reward);
                }
            }
            
            List<GridPosition> positions = configData.configurations[index].rewardPositions;
            
            // Create new rewards at specified positions
            currentRewardObjects = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 worldPos = positions[i].ToVector3();
                currentRewardObjects[i] = Instantiate(rewardPrefab, worldPos, Quaternion.identity);
                currentRewardObjects[i].name = $"Reward_{(char)('A' + i)}_{configData.configurations[index].configName}";
                currentRewardObjects[i].GetComponent<Renderer>().enabled = false;
                
                Debug.Log($"Reward {(char)('A' + i)} at world position: {worldPos}");
            }
            
            Debug.Log($"Loaded {configData.configurations[index].configName}");
        }
    }
    
    public int GetTotalConfigurations()
    {
        return configData.configurations.Count;
    }
    
    public string GetCurrentConfigName()
    {
        return configData.configurations[currentConfigIdx].configName;
    }
    
    public bool RewardFound(Vector3 playerPosition) //V: public so can be accessed by player movement script
    {
        Debug.Log($"Player position: {playerPosition}");
        Debug.Log($"nextRewardIdx: {nextRewardIdx}");
        int rewardsToCollect = configData.configurations[currentConfigIdx].SequenceLength;
        if (nextRewardIdx >= rewardsToCollect) //V: stop checking if we have already found the last reward
        {
            return false;
        }
        
        GameObject currReward = currentRewardObjects[nextRewardIdx];
        float distance = Vector3.Distance(playerPosition, currReward.transform.position);

        //V: Debug - show reward position and distance 
        Debug.Log($"Reward {nextRewardIdx} position: {currReward.transform.position}, Distance: {distance}");

        if (distance < 0.01f)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                Debug.Log("spacebar was pressed");
                var config = configData.configurations[currentConfigIdx];
                int rewardCount = config.SequenceLength;
                Debug.Log($"Reward {nextRewardIdx + 1}/{rewardCount} found!");
                ShowReward(nextRewardIdx);
                lastShownRewardIdx = nextRewardIdx;

                nextRewardIdx++;
                if (config.IsABCType && nextRewardIdx == 3 && repsCompleted < configData.trialsPerConfig - 1) //V: if we are in the ABC configuration and we have just found reward C and are not in the last repetition
                {
                    if (cueObject != null)
                    {
                        cueObject.SetActive(true);
                        Debug.Log("Cue displayed - ABC sequence at C");
                    }
                }
                
                if (nextRewardIdx >= rewardsToCollect) //V: check if we have just found the last reward
                {
                    repsCompleted++;
                    Debug.Log($"Last reward found! Trial {repsCompleted}/3 complete");
                    CompleteTrial();
                }
                
                return true;  
            }
            return false;
        }
        else
        {
            if (lastShownRewardIdx >= 0)
            {
                GameObject lastReward = currentRewardObjects[lastShownRewardIdx];
                float distanceToLast = Vector3.Distance(playerPosition, lastReward.transform.position);

                // hide reward only once player has actually left it
                if (distanceToLast > 0.05f) // adjust threshold if needed
                {
                    HideReward(lastShownRewardIdx);
                    lastShownRewardIdx = -1;
                }
            }
        }
        return false;
    }
        
    void CompleteTrial() //V: check if we have completed all repetitions of the current trial and switch to next configuration if appropriate
    {
        // Cue is hidden in ResetTrial() or StartNextConfigForFreeNav() after delay

        if (repsCompleted >= configData.trialsPerConfig)  
        {
            if (currentConfigIdx < configData.configurations.Count - 1)
            {
                Debug.Log($"{configData.configurations[currentConfigIdx].configName} complete!");
                currentConfigIdx++;
                repsCompleted = 0;
                isFirstRepofConfig = true;

                CameraManager camManager = FindFirstObjectByType<CameraManager>();
                FreeNavigationCamera freeNavCam = FindFirstObjectByType<FreeNavigationCamera>();

                if (camManager != null && camManager.enabled)
                {
                    if (NewRewLocations())
                    {
                        Invoke("StartNextConfiguration", 2f); //V: have top down view of the next configuration start a few seconds after trial is completed
                    }
                    else
                    {
                        Invoke("LoadConfiguration", 2f);
                        Invoke("ResetTrial", 2f);
                    }
                } 
                else if (freeNavCam != null && freeNavCam.enabled)
                {
                    LoadConfiguration(currentConfigIdx);
                    instructionManager.NewSequenceInstructions();
                }
                
  
            }
            else
            {
                Debug.Log("All configurations completed!");
                instructionManager.EndScreen();
            }
        }
        else
        {
            Debug.Log($"Moving on to repetition {repsCompleted + 1}/3");
            Invoke("ResetTrial", 2f);
        }
    }


    void StartNextConfiguration()
    {
        FindFirstObjectByType<CameraManager>().StartNewConfiguration(currentConfigIdx);
    }

    public void StartNextConfigForFreeNav()
    {
        // Reset for the new configuration
        HideAllRewards();
        nextRewardIdx = 0;
        lastShownRewardIdx = -1;
        
        Debug.Log($"Starting {configData.configurations[currentConfigIdx].configName}");
    }
    
    void ResetTrial()
    {
        HideAllRewards();
        HideCue();
        nextRewardIdx = 0;
        lastShownRewardIdx = -1;
        isFirstRepofConfig = false;

        // Player stays where they are (at last reward D or C) - no teleportation needed

        Debug.Log($"Starting trial {repsCompleted + 1}/{configData.trialsPerConfig} of Config {currentConfigIdx}");
    }

    public void ShowReward(int index)
    {
        Debug.Log($"ShowReward called with index: {index}");
        
        if (index >= 0 && index < currentRewardObjects.Length && currentRewardObjects[index] != null)
        {
            Debug.Log($"Showing reward at index {index}, name: {currentRewardObjects[index].name}");
            Debug.Log($"Renderer before: {currentRewardObjects[index].GetComponent<Renderer>().enabled}");
            
            currentRewardObjects[index].GetComponent<Renderer>().enabled = true;
            
            Debug.Log($"Renderer after: {currentRewardObjects[index].GetComponent<Renderer>().enabled}");
        }
        else
        {
            Debug.LogError($"Cannot show reward at index {index}!");
        }
    }

    public void HideReward(int index)
    {
        if (index >= 0 && index < currentRewardObjects.Length && currentRewardObjects[index] != null)
        {
            currentRewardObjects[index].GetComponent<Renderer>().enabled = false;
        }
    }
    
    void HideAllRewards()
    {
        if (currentRewardObjects != null)
        {
            foreach (GameObject reward in currentRewardObjects)
            {
                if (reward != null)
                {
                    reward.GetComponent<Renderer>().enabled = false;
                }
            }
        }
    }

    public void HideCue()
    {
        if (cueObject != null)
        {
            cueObject.SetActive(false);
        }
    }

    public Vector3 GetStartPosition()
    {
        var config = configData.configurations[currentConfigIdx];

        if (isFirstRepofConfig)
        {
            return config.rewardPositions[3].ToVector3(); //V: get the position of reward D
        }
        else
        {
            int lastRewardIdx = config.SequenceLength - 1;
            return config.rewardPositions[lastRewardIdx].ToVector3(); //V: get the position of the last visited reward
        }
    }
}