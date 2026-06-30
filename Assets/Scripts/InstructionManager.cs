using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

//V: manages instruction, new sequence and end panels; handles screen transitions triggered by button presses
public class InstructionManager : MonoBehaviour
{
    public GameObject instructionPanel;
    public GameObject movementPanel;
    public GameObject feedbackPanel;
    public TMP_Text feedbackText;
    public GameObject newSeqPanel;
    public rewardManager rewardManager;
    public moveplayer player;
    public CameraManager cameraManager;
    public GameObject endScreenPanel;
    public GameObject backwardWarningPanel;

    private float _screenOnset;
    private bool _canAdvance; //V: set to false in the first instruction screen, becomes true after 5TRs detected
    private float _screenShownAt;

    void Start()
    {
        instructionPanel.SetActive(false);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        endScreenPanel?.SetActive(false);
        backwardWarningPanel?.SetActive(false);
    }

    void Update() //V: runs every frame checking for key presses (all but ey 5 because that's the scanner trigger)
    {
        if (!_canAdvance) return;
        if (Time.unscaledTime - _screenShownAt < 0.5f) return;
        if (!Keyboard.current.anyKey.wasPressedThisFrame) return;
        foreach (var key in Keyboard.current.allKeys)
            if (key.wasPressedThisFrame) Debug.Log($"[InstructionManager] Pressed: {key.keyCode}");
        if (Keyboard.current.digit5Key.wasPressedThisFrame) return;
        if (Keyboard.current.numpad5Key.wasPressedThisFrame) return;

        if (instructionPanel.activeSelf)   AdvanceInstruction();
        else if (movementPanel.activeSelf) AdvanceMovement();
        else if (feedbackPanel.activeSelf) AdvanceFeedback();
        else if (newSeqPanel.activeSelf)   AdvanceNewSequence();
        else if (endScreenPanel.activeSelf) AdvanceEndScreen();
    }

    public void ShowInstructionPanel()
    {
        instructionPanel.SetActive(true);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
    }

    public void ShowInstruction()
    {
        instructionPanel.SetActive(true);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 0f;
        _screenOnset = Time.time - TRPulse.Instance.t0;
        _canAdvance = true;
        _screenShownAt = Time.unscaledTime;
    }

    public void ShowMovementInstruction()
    {
        movementPanel.SetActive(true);
        instructionPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 0f;
        _screenOnset = Time.time - TRPulse.Instance.t0;
        _canAdvance = true;
        _screenShownAt = Time.unscaledTime;
    }

    public void ShowFeedback(int optimal, int total)
    {
        float percentage = total > 0 ? (float)optimal / total * 100f : 0f;
        feedbackText.text = $"This round you found {Mathf.RoundToInt(percentage)}% of gems using the smallest number of steps";
        feedbackPanel.SetActive(true);
        instructionPanel.SetActive(false);
        movementPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 0f;
        _screenOnset = Time.time - TRPulse.Instance.t0;
        _canAdvance = true;
        _screenShownAt = Time.unscaledTime;
    }

    public void NewSequenceInstructions()
    {
        newSeqPanel.SetActive(true);
        movementPanel.SetActive(false);
        instructionPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        Time.timeScale = 0f;
        _screenOnset = Time.time - TRPulse.Instance.t0;
        _canAdvance = true;
        _screenShownAt = Time.unscaledTime;
    }

    //V: button callbacks - define what happens after each screen is dismissed
    public void AdvanceInstruction()
    {
        DataLogger.Instance.LogScreen("instruction", _screenOnset, Time.time - TRPulse.Instance.t0);
        instructionPanel.SetActive(false);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;
        ShowMovementInstruction();
    }

    public void AdvanceMovement()
    {
        DataLogger.Instance.LogScreen("movement", _screenOnset, Time.time - TRPulse.Instance.t0);
        instructionPanel.SetActive(false);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;
        cameraManager.StartNewConfiguration(rewardManager.GetCurrentConfigIndex());
    }

    public void AdvanceFeedback()
    {
        DataLogger.Instance.LogScreen("feedback", _screenOnset, Time.time - TRPulse.Instance.t0);
        instructionPanel.SetActive(false);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;
        NewSequenceInstructions();
    }

    public void AdvanceNewSequence()
    {
        DataLogger.Instance.LogScreen("new_sequence", _screenOnset, Time.time - TRPulse.Instance.t0);
        instructionPanel.SetActive(false);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;
        rewardManager.StartNextConfiguration();
    }

    public void EndScreen()
    {
        endScreenPanel?.SetActive(true);
        Time.timeScale = 0f;
        _canAdvance = true;
        _screenShownAt = Time.unscaledTime;
    }

    public void AdvanceEndScreen()
    {
        if (SceneManager.GetActiveScene().name == "Part 1")
        {
            SceneSequenceManager.Instance.GoToTask2();
        }
    }
}
