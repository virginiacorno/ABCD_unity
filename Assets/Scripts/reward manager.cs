using UnityEngine;
using System.Collections.Generic;

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
    
    private ConfigurationData configData;
    private GameObject[] currentRewardObjects; //V: array containing sequence of rewards
    private int currentConfigIdx = 0;
    private int nextRewardIdx = 0;
    private int repsCompleted = 0;
    private int lastShownRewardIdx = -1;
    
    void Awake() //V: Awake() takes precedence over any Start() in any of the scripts, so we make sure all rewards are hidden before starting 
    {
        LoadConfigurationsFromFile();
        
        if (configData != null && configData.configurations.Count > 0)
        {
            LoadConfiguration(0);
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
        Debug.Log($"=== RewardFound called ===");
        Debug.Log($"Player position: {playerPosition}");
        Debug.Log($"nextRewardIdx: {nextRewardIdx}");
        if (nextRewardIdx > 3) //V: stop checking if we have already found the last reward
        {
            return false;
        }
        
        GameObject currReward = currentRewardObjects[nextRewardIdx];
        float distance = Vector3.Distance(playerPosition, currReward.transform.position);
        
        if (distance < 0.01f)
        {
            Debug.Log($"Reward {nextRewardIdx + 1}/4 found!");
            ShowReward(nextRewardIdx);  
            lastShownRewardIdx = nextRewardIdx;  
            
            nextRewardIdx++;
            
            if (nextRewardIdx > 3) //V: check if we have just found the last reward
            {
                repsCompleted++;
                Debug.Log($"Last reward found! Trial {repsCompleted}/3 complete");
                CompleteTrial();
            }
            
            return true;
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
        if (repsCompleted >= configData.trialsPerConfig)  
        {
            if (currentConfigIdx < configData.configurations.Count - 1)
            {
                Debug.Log($"{configData.configurations[currentConfigIdx].configName} complete!");
                currentConfigIdx++;
                repsCompleted = 0;

                Invoke("StartNextConfiguration", 2f); //V: have top down view of the next configuration start a few seconds after trial is completed
  
            }
            else
            {
                Debug.Log("All configurations completed!");
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
    
    void ResetTrial()
    {
        HideAllRewards();
        nextRewardIdx = 0;
        lastShownRewardIdx = -1;
        
        Debug.Log($"Starting trial {repsCompleted + 1}/3 of Config {currentConfigIdx}");
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
}