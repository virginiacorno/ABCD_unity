using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void StartPractice()
    {
        rewardManager.LoadConfiguration(0);
        //V: ensure inputs are enabled but only possible to rotate (vs also moving)
        player.inputEnabled = false;
        StartCoroutine(RunPracticeLoop());
    }


    IEnumerator RunPracticeLoop()
    {
        while (currentStreak < requiredStreak)
        {
            //V: show location of the reward
            cameraManager.SetupMemorizationCamera();
            //V: set the player in the centre of the grid but keep it invisible
            player.transform.position = new Vector3(15.3f, 1f, 15.3f);
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            player.GetComponent<Renderer>().enabled = false;

            //V; pick random reward from the configuration
            int rewardCount = rewardManager.GetCurrentRewardCount();
            int targetIdx = Random.Range(0, rewardCount);

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
