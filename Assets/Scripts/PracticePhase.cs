using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PracticePhase : MonoBehaviour
{
    public moveplayer player;
    public rewardManager rewardManager;
    public CameraManager cameraManager;

    [Header("Practice Settings")]
    public int requiredStreak = 3;
    public float rewardDisplayTime = 2f;
    public float pauseBetweenTrials = 0.5f;
    private int currentStreak = 0;
    private List<int> _remainingTargets = new List<int>(); //V: which grid locations we haven't visited yet

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void StartPractice()
    {
        rewardManager.LoadConfiguration(0);
        //V: ensure inputs are enabled but only possible to rotate (vs also moving)
        player.inputEnabled = false;
        StartCoroutine(RunPracticeLoop());
    }


    //V: function ensuring each position ID in the Practice json file is only appearing once, only repeat indices if we have visited them all
    int GetNextTargetIdx()
    {
        if (_remainingTargets.Count == 0)
        {
            int count = rewardManager.GetCurrentRewardCount();
            for (int i = 0; i < count; i++) _remainingTargets.Add(i);
        }
        int randomIdx = Random.Range(0, _remainingTargets.Count);
        int targetIdx = _remainingTargets[randomIdx];
        _remainingTargets.RemoveAt(randomIdx);
        return targetIdx;
    }

    //V: random cell from the loaded config's own positions, excluding the current trial's target
    Vector3 GetRandomStartPosition(int excludeIdx)
    {
        List<GridPosition> positions = rewardManager.config.rewardPositions;
        List<int> candidates = new List<int>();
        for (int i = 0; i < positions.Count; i++)
        {
            if (i != excludeIdx) candidates.Add(i);
        }
        int idx = candidates[Random.Range(0, candidates.Count)];
        return positions[idx].ToVector3();
    }

    IEnumerator RunPracticeLoop()
    {
        while (currentStreak < requiredStreak)
        {
            //V: show location of the reward
            cameraManager.SetupMemorizationCamera();

            //V: pick the next target, cycling through every cell once before repeating
            int targetIdx = GetNextTargetIdx();

            //V: set the player at a random cell that isn't this trial's target, but keep it invisible
            player.transform.position = GetRandomStartPosition(targetIdx);
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            player.GetComponent<Renderer>().enabled = false;

            //V: show the reward and then start
            rewardManager.ShowReward(targetIdx);
            yield return new WaitForSeconds(rewardDisplayTime);
            rewardManager.HideReward(targetIdx);

            yield return StartCoroutine(cameraManager.TransitionToFirstPerson());


            //V: detect space bar press for inner loop, runs every frame
            bool pressDetected = false;
            while (!pressDetected)
            {
                var kb = Keyboard.current;
                if (kb != null && kb.digit4Key.wasPressedThisFrame) //V: if reward is checked for, check if we are at reward location and either reset or increment the streak
                {
                    pressDetected = true; //V: stop inner loop for constantly checking keyboard presses
                    float dist = Vector3.Distance(
                        player.transform.position,
                        rewardManager.GetRewardWorldPosition(targetIdx)
                    );

                    if (dist < 0.01f)
                    {
                        currentStreak++;
                        rewardManager.ShowReward(targetIdx);
                        yield return new WaitForSeconds(rewardDisplayTime);
                        rewardManager.HideReward(targetIdx);
                        Debug.Log($"[Practice] Correct! Streak: {currentStreak}/{requiredStreak}");
                    }

                    else
                    {
                        currentStreak = 0;
                        Debug.Log("[Practice] Incorrect — streak reset");
                    }
                }
                yield return null;
            }

            player.inputEnabled = false;
            yield return new WaitForSeconds(pauseBetweenTrials);
        }

        //V: while loop breaks once we complete all required streaks, so then we can proceed to task
        Debug.Log("[Practice] Streak complete — loading task scene");
        float t = Time.time - DataLogger.Instance.T0;
        DataLogger.Instance.LogScreen("practice_phase_end", t, t);
        SceneSequenceManager.Instance.GoToTask();
    }
}
