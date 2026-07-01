using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; 
using UnityEngine.SceneManagement;

public class rewardManager : MonoBehaviour
{
    
    [Header("Reward Prefab")]
    public GameObject rewardPrefab;

    [Header("UI References")]
    public InstructionManager instructionManager;

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

    //V: variables related to jitters
    private float _trialT = 1.75f;
    private float _trialRotDuration;
    private const float _rotFraction = 0.4f;
    private const float _rewardFraction = 0.8f;
    private bool _lastMoveWasRotation = false;
    private float _TRdur = 1.078f;



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

            SampleTrialJitter();

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
        if (keyboard != null && keyboard.digit4Key.wasPressedThisFrame)
        {
            bool atRewardLocation = (distance < 0.01f);
            
            // log all space bar presses
            float tStart = Time.time - TRPulse.Instance.t0;
            float tStartCurrRun = Time.time - player.repStartTime;
            float rewardDelay = atRewardLocation ? GetRewardDisplayDuration() : 0f;
            DataLogger.Instance.LogRewardCheck(
                playerPosition.x, playerPosition.z,
                currReward.transform.position.x, currReward.transform.position.z,
                distance,
                GetCurrentState(),
                config.IsBackw ? "backw" : "forw",
                repsCompleted,
                GetCurrentConfigName(),
                atRewardLocation,
                tStart,
                tStartCurrRun,
                tStart + rewardDelay,
                tStartCurrRun + rewardDelay,
                rewardDelay
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

                    Invoke("CompleteTrial", GetRewardDisplayDuration()); //V: have player wait at reward for the amount of time specified by the jitter
                    return true;
                }

                Debug.Log("spacebar was pressed at reward location");
                int rewardCount = config.rewardPositions.Count;
                Debug.Log($"Reward {nextRewardIdx + 1}/{rewardCount} found!");
                
                ShowReward(nextRewardIdx);
                player.inputEnabled = false;
                Invoke("ReEnableInput", GetRewardDisplayDuration());

                lastShownRewardIdx = nextRewardIdx;
                
                nextRewardIdx += config.IsBackw ? -1 : 1; //V: if it's a backward trial, subtract 1 (otherwise add 1)

                //V: calculate optimal steps for next subpath (if there is a reward)
                if (nextRewardIdx >= 0 && nextRewardIdx < rewardsToCollect)
                {
                    shortestPath = CalculateShortestPath(
                        player.transform.position,
                        config.rewardPositions[nextRewardIdx].ToVector3()
                    );

                    _lastMoveWasRotation = false;

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

                    _lastMoveWasRotation = false;
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
        if (lastShownRewardIdx >= 0)
        {
            HideReward(lastShownRewardIdx);
            lastShownRewardIdx = -1;
        }

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
        player.repStartTime = Time.time;

        shortestPath = CalculateShortestPath(
            player.transform.position,
            config.rewardPositions[nextRewardIdx].ToVector3()
        );
        player.stepCount = 0;

        SampleTrialJitter();

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

    //V: helper function to calculate the optimal number of rotations for a given shortest path
    int CalculateOptimalRotations(Vector3 from, Vector3 to, Vector3 facingDir)
    {
        //V: calculate how much the player would need to move horizontally and vertically to reach the target form the current location
        float dx = to.x - from.x;
        float dz = to.z - from.z;

        // 0 mid-path turns if straight line, 1 if L-shaped
        int midPathTurn = (Mathf.Abs(dx) > 0.1f && Mathf.Abs(dz) > 0.1f) ? 1 : 0;

        //V: find optimal first direction to move in based on current facing
        Vector3 firstDir;
        if (Mathf.Abs(dx) < 0.1f) //V: if parth is only horizontal (player is already facing the correct direction)
            firstDir = new Vector3(0, 0, Mathf.Sign(dz));
        else if (Mathf.Abs(dz) < 0.1f) //V: if path is only vertical
            firstDir = new Vector3(Mathf.Sign(dx), 0, 0);
        else //V: if rotations are needed, then first pick the rotation that is closest to current player facing
        {
            Vector3 xDir = new Vector3(Mathf.Sign(dx), 0, 0); //V: vector pointing in the right horizontal direction (East or West)
            Vector3 zDir = new Vector3(0, 0, Mathf.Sign(dz)); //V: vector pointing in the right vertical direction (North or South)

            //V: measure the angle between the current and and X/Z facing direction
            float angleToX = Mathf.Abs(Vector3.SignedAngle(facingDir, xDir, Vector3.up));
            float angleToZ = Mathf.Abs(Vector3.SignedAngle(facingDir, zDir, Vector3.up));

            //V: pick first move that requires the least rotations
            firstDir = (angleToX <= angleToZ) ? xDir : zDir;
        }

        // how many 90 degree turns to align with firstDir (0, 1, or 2)
        float angle = Mathf.Abs(Vector3.SignedAngle(facingDir, firstDir, Vector3.up));
        int turnsToAlign = Mathf.RoundToInt(angle / 90f);

        return turnsToAlign + midPathTurn;
    }


    //V: helper functions for sampling step velocities (reproduces Marsaglia-Tsan method, which underlies np.gamma function)
    float SampleNormal()
    {
        float u1 = Mathf.Max(1e-10f, Random.value);
        float u2 = Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
    }

    float SampleGamma(float shape)
    {
        if (shape < 1f)
            return SampleGamma(shape + 1f) * Mathf.Pow(Random.value, 1f / shape);

        float d = shape - 1f / 3f;
        float c = 1f / Mathf.Sqrt(9f * d);
        while (true)
        {
            float x, v;
            do { x = SampleNormal(); v = 1f + c * x; } while (v <= 0f);
            v = v * v * v;
            float u = Random.value;
            if (u < 1f - 0.0331f * x * x * x * x) return d * v;
            if (Mathf.Log(u) < 0.5f * x * x + d * (1f - v + Mathf.Log(v))) return d * v;
        }
    }

    //V: samples T once per trial from a truncated gamma; T is the duration of every step
    //V: rotation takes _rotFraction of T; post-rotation step takes the remainder so rotation+step = T
    //V: tMin ensures at least 2 TRs of walking on the shortest subpath; tMax keeps trials under 15s
    public void SampleTrialJitter()
    {
        float shape = 3f;
        float total;
        do { total = SampleGamma(shape); }
        while (total < _TRdur || total > 7.5f);

        _trialT = total;
        _trialRotDuration = _trialT * _rotFraction;
        _lastMoveWasRotation = false;
    }

    public float GetStepDuration()
    {
        float duration = _lastMoveWasRotation ? _trialT - _trialRotDuration : _trialT;
        _lastMoveWasRotation = false;
        return duration;
    }

    public float GetRotationDuration()
    {
        _lastMoveWasRotation = true;
        return _trialRotDuration;
    }

    //V: reward waiting time is proportional to the trial's step duration, floored at 2 TRs 
    public float GetRewardDisplayDuration()
    {
        return Mathf.Max(2f * _TRdur, _rewardFraction * _trialT);
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

    //V: helper function to re enable input 
    void ReEnableInput()
    {
        if (lastShownRewardIdx >= 0)
        {
            HideReward(lastShownRewardIdx);
            lastShownRewardIdx = -1;
        }
        player.inputEnabled = true;
    }
}