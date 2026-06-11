using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; 
using UnityEngine.SceneManagement;

public class rewardManager : MonoBehaviour
{
    
    [Header("Reward Prefab")]
    public GameObject rewardPrefab;

    [Header("UI References")]
    public TaskInstructionManagerBase instructionManager;

    private List<TaskConfig> _activeTasks;
    private int _trialsPerTask;

    private GameObject[] currentRewardObjects;
    private int currentConfigIdx = 0;
    private int nextRewardIdx = 0;
    public int repsCompleted = 0;
    private int lastShownRewardIdx = -1;
    public moveplayer player;
    private bool returnToA = false;
    public TaskConfig config => _activeTasks[currentConfigIdx];

    //V: variabke storing shortest path 
    private int shortestPath;
    private int totalSubpaths;
    private int optimalSubpaths;
    private int _currentPart;


    void Awake()
    {
        LoadTasks();

        if (_activeTasks != null && _activeTasks.Count > 0)
        {
            string firstConfig = TaskPackageManager.Instance.GetNextConfigName(_currentPart);
            currentConfigIdx = _activeTasks.FindIndex(t => t.configName == firstConfig);
        }
        else
        {
            Debug.LogError("No configurations loaded!");
        }
    }


    void Start()
    {
        if (_activeTasks != null && _activeTasks.Count > 0)
        {
            Debug.Log($"Starting {_activeTasks[currentConfigIdx].configName}");
            Debug.Log($"Total tasks loaded: {_activeTasks.Count}");
        }
    }

    void LoadTasks()
    {
        if (TaskPackageManager.Instance == null)
        {
            Debug.LogError("[rewardManager] No TaskPackageManager found!");
            return;
        }

        bool isPart2 = SceneManager.GetActiveScene().name == "Part 2";
        _activeTasks = isPart2
            ? TaskPackageManager.Instance.GetPart2Tasks()
            : TaskPackageManager.Instance.GetPart1Tasks();
        _currentPart = isPart2 ? 2 : 1;
        _trialsPerTask = TaskPackageManager.Instance.Data.trialsPerTask;
        Debug.Log($"[rewardManager] Loaded {_activeTasks.Count} tasks for {(isPart2 ? "Part 2" : "Part 1")}");
    }


    //V: need repetition to ensure some delay between end of previous trial and loading new configurations
    public void LoadConfiguration()
    {
        LoadConfiguration(currentConfigIdx);
    }

    public void LoadConfiguration(int index)
    {
        if (index >= 0 && index < _activeTasks.Count)
        {
            currentConfigIdx = index;
            nextRewardIdx = GetStartIndex();
            lastShownRewardIdx = -1;
            returnToA = false;

            // Destroy old rewards
            if (currentRewardObjects != null)
            {
                foreach (GameObject reward in currentRewardObjects)
                {
                    if (reward != null)
                        Destroy(reward);
                }
            }
            
            List<GridPosition> positions = _activeTasks[index].rewardPositions;
            
            // Create new rewards at specified positions
            currentRewardObjects = new GameObject[positions.Count];
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 worldPos = positions[i].ToVector3();
                currentRewardObjects[i] = Instantiate(rewardPrefab, worldPos, Quaternion.identity);
                currentRewardObjects[i].name = $"Reward_{(char)('A' + i)}_{_activeTasks[index].configName}";
                //currentRewardObjects[i].GetComponent<Renderer>().enabled = false;
                currentRewardObjects[i].SetActive(false);

                Debug.Log($"Reward {(char)('A' + i)} at world position: {worldPos}");
            }
            
            // Reposition player to the start position for this config
            player.SetPosition(GetStartPosition());

            //V: calculate shortest path to reward A 
            shortestPath = CalculateShortestPath(
                player.transform.position,
                config.rewardPositions[nextRewardIdx].ToVector3()
            );
            player.stepCount = 0; //V: reset it at the beginning of trials

            Debug.Log($"Loaded {_activeTasks[index].configName}");
        }
    }

    int GetStartIndex()
    {
        return config.IsBackw ? config.rewardPositions.Count - 1 : 0; //V: if the current config is a backward trial, return number corresponding to last reward index, otherwise return 0
    }
    
    public int GetTotalConfigurations()
    {
        return _activeTasks.Count;
    }
    
    public string GetCurrentConfigName()
    {
        return _activeTasks[currentConfigIdx].configName;
    }

    public int GetCurrentConfigIndex()
    {
        return currentConfigIdx;
    }
    
    public bool RewardFound(Vector3 playerPosition)
    {
        if (_activeTasks == null) return false;
        if (currentRewardObjects == null) return false;
        Debug.Log($"Player position: {playerPosition}");
        Debug.Log($"nextRewardIdx: {nextRewardIdx}");
        int rewardsToCollect = config.rewardPositions.Count;
        
        if (nextRewardIdx >= rewardsToCollect || nextRewardIdx < 0) //V: < 0 in case we are in backward trials
        {
            return false;
        }
        
        GameObject currReward = currentRewardObjects[nextRewardIdx];
        float distance = Vector3.Distance(playerPosition, currReward.transform.position);

        Debug.Log($"Reward {nextRewardIdx} position: {currReward.transform.position}, Distance: {distance}");

        //V: check for space bar presses
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            bool atRewardLocation = (distance < 0.01f);
            
            // log all space bar presses
            DataLogger.Instance.LogRewardCheck(
                playerPosition.x, playerPosition.z,
                currReward.transform.position.x, currReward.transform.position.z,
                distance,
                GetCurrentState(),
                config.IsBackw ? "backw" : "forw",
                repsCompleted,
                GetCurrentConfigName(),
                atRewardLocation,
                atRewardLocation ? Time.time - TRPulse.Instance.t0 : 0f,
                0f
            );
            
            // Only process reward if at correct location
            if (atRewardLocation)
            {
                //V: increase number of subpath completed and check if it was optimal, then reset player step count
                totalSubpaths++;
                if (player.stepCount == shortestPath)
                    optimalSubpaths++;
                player.stepCount = 0;

                if (returnToA) //V: if this was set true before, turn it off, show the reward and then increase reps completed
                {
                    returnToA = false;

                    int returnIdx = config.IsBackw ? config.rewardPositions.Count - 1 : 0;
                    ShowReward(returnIdx);
                    lastShownRewardIdx = returnIdx;

                    player.inputEnabled = false;
                    repsCompleted++;

                    Invoke("CompleteTrial", 0.5f);
                    return true;
                }

                Debug.Log("spacebar was pressed at reward location");
                int rewardCount = config.rewardPositions.Count;
                Debug.Log($"Reward {nextRewardIdx + 1}/{rewardCount} found!");
                
                ShowReward(nextRewardIdx);
                lastShownRewardIdx = nextRewardIdx;
                
                nextRewardIdx += config.IsBackw ? -1 : 1; //V: if it's a backward trial, subtract 1 (otherwise add 1)

                //V: calculate optimal steps for next subpath (if there is a reward)
                if (nextRewardIdx >= 0 && nextRewardIdx < rewardsToCollect)
                {
                    shortestPath = CalculateShortestPath(
                        player.transform.position,
                        config.rewardPositions[nextRewardIdx].ToVector3()
                    );
                    Debug.Log($"shortest path = {shortestPath}");
                }

                // V: if we have collected last reward (D/A), then return to A to complete the trial
                if (nextRewardIdx >= rewardsToCollect || nextRewardIdx < 0)
                {
                    Debug.Log("return to A");
                    returnToA = true;
                    nextRewardIdx = config.IsBackw ? config.rewardPositions.Count - 1 : 0;
                    shortestPath = CalculateShortestPath(
                        player.transform.position,
                        config.rewardPositions[nextRewardIdx].ToVector3()
                    );
                }
                
                return true;  
            }
            else
            {
                Debug.Log($"Space pressed but not at reward. Distance: {distance}");
                return false;
            }
        }
        
        // Handle hiding rewards when player moves away
        if (lastShownRewardIdx >= 0)
        {
            GameObject lastReward = currentRewardObjects[lastShownRewardIdx];
            float distanceToLast = Vector3.Distance(playerPosition, lastReward.transform.position);

            if (distanceToLast > 0.05f)
            {
                HideReward(lastShownRewardIdx);
                lastShownRewardIdx = -1;
            }
        }
        
        return false;
    }    

    public void CompleteTrial() //V: check if we have completed all repetitions of the current trial and switch to next configuration if appropriate
    {
        // Cue is hidden in ResetTrial() or StartNextConfigForFreeNav() after delay

        if (repsCompleted >= _trialsPerTask)  
        {
            if (TaskPackageManager.Instance.HasMoreTasks(_currentPart))
            {
                Debug.Log($"{_activeTasks[currentConfigIdx].configName} complete!");
                string nextConfig = TaskPackageManager.Instance.GetNextConfigName(_currentPart);
                currentConfigIdx = _activeTasks.FindIndex(t => t.configName == nextConfig);
                if (currentConfigIdx == -1)
                {
                    Debug.LogError($"[rewardManager] Could not find config in _activeTasks!");
                    return;
                }
                    
                repsCompleted = 0;

                CameraManager camManager = FindFirstObjectByType<CameraManager>();
                FreeNavigationCamera freeNavCam = FindFirstObjectByType<FreeNavigationCamera>();

                if (camManager != null && camManager.enabled)
                {
                    instructionManager.ShowFeedback(optimalSubpaths, totalSubpaths);
                    totalSubpaths = 0;
                    optimalSubpaths = 0;
                }
                else if (freeNavCam != null && freeNavCam.enabled)
                {
                    LoadConfiguration(currentConfigIdx);
                    Debug.Log("Calling new sequence");
                    instructionManager.ShowFeedback(optimalSubpaths, totalSubpaths);
                    totalSubpaths = 0;
                    optimalSubpaths = 0;
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
            Invoke("ResetTrial", 0.5f);
        }
    }


    public void StartNextConfiguration()
    {
        FindFirstObjectByType<CameraManager>().StartNewConfiguration(currentConfigIdx);
    }

    public void StartNextConfigForFreeNav()
    {
        HideAllRewards();
        nextRewardIdx = GetStartIndex();
        lastShownRewardIdx = -1;

        player.CameraController.SetupGameplayCameras();
        player.inputEnabled = true;

        Debug.Log($"Starting {config.configName}");
    }
    
    void ResetTrial()
    {
        HideAllRewards();
        nextRewardIdx = config.IsBackw ? config.rewardPositions.Count - 2 : 1; // V: next reward to find is B, so transition for zero-shot is included in each trial
        lastShownRewardIdx = -1;
        returnToA = false;

        player.inputEnabled = true;

        shortestPath = CalculateShortestPath(
            player.transform.position,
            config.rewardPositions[nextRewardIdx].ToVector3()
        );
        player.stepCount = 0;

        Debug.Log($"Starting trial {repsCompleted + 1}/{_trialsPerTask} of Config {currentConfigIdx}");
    }

    public void ShowReward(int index)
    {
        Debug.Log($"ShowReward called with index: {index}");
        
        if (index >= 0 && index < currentRewardObjects.Length && currentRewardObjects[index] != null)
        {
            Debug.Log($"Showing reward at index {index}, name: {currentRewardObjects[index].name}");
            //Debug.Log($"Renderer before: {currentRewardObjects[index].GetComponent<Renderer>().enabled}");

            //currentRewardObjects[index].GetComponent<Renderer>().enabled = true;
            currentRewardObjects[index].SetActive(true);
            Vector3 dir = -player.transform.forward;
            dir.y = 0;
            currentRewardObjects[index].transform.rotation = Quaternion.LookRotation(dir);
            
            //Debug.Log($"Renderer after: {currentRewardObjects[index].GetComponent<Renderer>().enabled}");
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
            //currentRewardObjects[index].GetComponent<Renderer>().enabled = false;
            currentRewardObjects[index].SetActive(false);
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
                    //reward.GetComponent<Renderer>().enabled = false;
                    reward.SetActive(false);
                }
            }
        }
    }

    public Vector3 GetStartPosition()
    {
        int lastRewardIdx = config.IsBackw ? 0 : config.rewardPositions.Count - 1;
        return config.rewardPositions[lastRewardIdx].ToVector3();
    }

    public int GetCurrentRewardCount()
    {
        return config.rewardPositions.Count;
    }

    public Vector3 GetRewardWorldPosition(int idx)
    {
        if (currentRewardObjects != null && idx >= 0 && idx < currentRewardObjects.Length && currentRewardObjects[idx] != null)
            return currentRewardObjects[idx].transform.position;
        return Vector3.zero;
    }

    //V: helper function to calculate shortest path (Manhattan distance)
    int CalculateShortestPath(Vector3 from, Vector3 to)
    {
        return Mathf.RoundToInt(
            (Mathf.Abs(from.x - to.x) + Mathf.Abs(from.z - to.z)) / 10.3f //V: divide by step size to get number of my steps needed
        );
    }

    //V; helper functions to determine current state and position of the reward to find for logging
    public string GetCurrentState()
    {
        int idx = config.IsBackw ? nextRewardIdx : nextRewardIdx;
        return ((char)('A' + nextRewardIdx)).ToString();
    }

    public Vector3 GetCurrentRewardPosition()
    {
        return GetRewardWorldPosition(nextRewardIdx);
    }
}