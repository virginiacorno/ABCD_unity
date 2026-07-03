using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CameraManager : MonoBehaviour, ICameraController
{

    //V: create all necessary cameras
    public Camera firstPersonCamera;
    public Camera miniMapCamera;
    
    //V: create reward manager object for showing rewards
    public rewardManager rewardManager;
    
    //V: create player object
    public GameObject player;

    //V: backward warning text
    public GameObject backwWarning;
    private float _cameraTransitionStart;
    
    //V: specify timing variables
    public float[] rewardDisplayTime;
    public float[] pauseBetweenRewards;
    public float pauseBetweenSeq = 1f;
    
    [Header("Memorization Settings")]
    public int memorizationRepetitions = 2;  //V: how many times to show the sequence

    [Header("Transition Settings")]
    public float transitionDuration = 2.5f;  //V: seconds for the smooth camera transition

    public bool isPractice = false;

    void Start()
    {
        //V: Initialize timing arrays
        rewardDisplayTime = new float[] {1.5f, 0.75f};
        pauseBetweenRewards = new float[] {0.5f, 0.25f};
        
        if (!isPractice)
        {
            //V: Start with first configuration
            StartNewConfiguration(rewardManager.GetCurrentConfigIndex());
        }
    }
    
    //V: Called when starting a new configuration (at start and after completing trials)
    public void StartNewConfiguration(int configIndex)
    {
        //V: Load the new configuration in reward manager
        rewardManager.LoadConfiguration(configIndex);

        //V: Hide player and disable movement initially
        player.GetComponent<Renderer>().enabled = false;
        player.GetComponent<moveplayer>().enabled = false;
        
        //V: Setup camera for memorization phase
        SetupMemorizationCamera();
        
        Debug.Log($"Memorizing {rewardManager.GetCurrentConfigName()}: Watch the reward sequence!");
        
        //V: Start the coroutine to show rewards
        StartCoroutine(ShowRewardSequence());
    }
    
    public void SetupMemorizationCamera()
    {
        backwWarning.SetActive(false);
        firstPersonCamera.enabled = false;
        miniMapCamera.enabled = true;
        
        //V: Put camera as full screen to show rewards
        miniMapCamera.rect = new Rect(0, 0, 1, 1);
        miniMapCamera.depth = 0;
    }
    
    public void SetupGameplayCameras()
    {
        firstPersonCamera.enabled = true;
        miniMapCamera.enabled = true;

        //V: Mini-map in top-right corner
        miniMapCamera.rect = new Rect(0.85f, 0.75f, 0.20f, 0.25f); //V: (x, y, width, height)
        miniMapCamera.depth = 1;
    }
    
    IEnumerator ShowRewardSequence()
    {
        //V: Show sequence multiple times
        for (int repetition = 0; repetition < memorizationRepetitions; repetition++)
        {
            Debug.Log($"Showing sequence {repetition + 1}/{memorizationRepetitions}");

            //V: Show each reward in order
            for (int i = 0; i < rewardManager.GetCurrentRewardCount(); i++)
            {
                //V: check if reward warning should be displayed
                if (rewardManager.config.IsBackw)
                {
                    backwWarning.SetActive(true);
                }

                rewardManager.ShowReward(i);
                Debug.Log($"Reward {i + 1}/4");

                yield return new WaitForSeconds(rewardDisplayTime[repetition]);

                rewardManager.HideReward(i);

                yield return new WaitForSeconds(pauseBetweenRewards[repetition]);
            }

            //V: Pause between repetitions (but not after the last one)
            if (repetition < memorizationRepetitions - 1)
            {
                yield return new WaitForSeconds(pauseBetweenSeq);
            }
        }

        Debug.Log("Memorization complete! Transitioning to first-person view...");

        yield return new WaitForSeconds(1f);

        //V: Smooth transition instead of instant swap
        StartCoroutine(TransitionToFirstPerson());
    }

    public IEnumerator TransitionToFirstPerson()
    {
        firstPersonCamera.enabled = true;
        
        //V: disable tbackwarning warning and log it
        if (backwWarning.activeSelf)
        {
            backwWarning.SetActive(false);
        }

        //V: Show the player during the transition
        player.GetComponent<Renderer>().enabled = true;
        _cameraTransitionStart = Time.time - DataLogger.Instance.T0;

        //V: Read start position/rotation from the minimap camera (set in Inspector)
        Vector3 startPos = miniMapCamera.transform.position;
        Quaternion startRot = miniMapCamera.transform.rotation;

        //V: Target = the actual first-person camera world position (behind/above the player)
        Vector3 endPos = firstPersonCamera.transform.position;
        Quaternion endRot = firstPersonCamera.transform.rotation;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));

            miniMapCamera.transform.position = Vector3.Lerp(startPos, endPos, t); //V: function to gradually and smoothly animate
            miniMapCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            yield return null;
        }

        //V: Snap to exact final position
        miniMapCamera.transform.position = endPos;
        miniMapCamera.transform.rotation = endRot;

        //V: Restore minimap camera to its original top-down position/rotation before switching
        miniMapCamera.transform.position = startPos;
        miniMapCamera.transform.rotation = startRot;

        DataLogger.Instance.LogCameraTransition("top_down", "first_person", _cameraTransitionStart, Time.time - DataLogger.Instance.T0);
        StartGamePhase();
    }
    
    void StartGamePhase()
    {
        SetupGameplayCameras();
        
        player.GetComponent<Renderer>().enabled = true;
        player.GetComponent<moveplayer>().enabled = true;
        player.GetComponent<moveplayer>().inputEnabled = true;
        player.GetComponent<moveplayer>().repStartTime = Time.time;

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