using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class FreeNavigationCamera : MonoBehaviour, ICameraController
{

    public Camera firstPersonCamera;
    public Camera miniMapCamera;
    public GameObject player;
    public rewardManager rewardManager;

    [HideInInspector] public bool _startGameAfterTransition = false;
    
    [Header("Transition Settings")]
    public float transitionDuration = 2.5f;
    
    public void StartNewConfiguration(int configIndex)
    {
        _startGameAfterTransition = true;
        //V: Load the new configuration in reward manager
        rewardManager.LoadConfiguration(configIndex);
        //V: set cameras in position to begin the smooth transition
        firstPersonCamera.enabled = false;
        miniMapCamera.enabled = true;
        miniMapCamera.rect = new Rect(0, 0, 1, 1);
        miniMapCamera.depth = 0;

        StartCameraTransition();
    }

    public void StartCameraTransition()
    {
        firstPersonCamera.enabled = false;
        miniMapCamera.enabled = true;
        miniMapCamera.rect = new Rect(0, 0, 1, 1);
        miniMapCamera.depth = 0;
        StartCoroutine(TransitionToFirstPerson());
    }

    IEnumerator TransitionToFirstPerson()
    {
        //V: Show the player during the transition
        player.GetComponent<Renderer>().enabled = true;

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

        SetupGameplayCameras(); //V: call the setup gameplay cameras to start playing
    }


    public void SetupGameplayCameras()
    {
        firstPersonCamera.enabled = true;
        miniMapCamera.enabled = true;

        //V: Mini-map in top-right corner
        miniMapCamera.rect = new Rect(0.75f, 0.75f, 0.25f, 0.25f);
        miniMapCamera.depth = 1;

        if (_startGameAfterTransition)
        {
            _startGameAfterTransition = false;
            rewardManager.StartNextConfigForFreeNav();
        }
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
