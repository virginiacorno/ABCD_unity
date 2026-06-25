using UnityEngine;

public class ClassicTaskInstructionManager : TaskInstructionManagerBase
{
    public CameraManager cameraManager;
    public GameObject endScreenPanel;
    public GameObject backwardWarningPanel;

    void Start()
    {
        instructionPanel.SetActive(false);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        endScreenPanel?.SetActive(false);
        backwardWarningPanel?.SetActive(false);
    }

    //V: update the behaviour of buttons
    public override void OnInstructionButton()
    {
        DataLogger.Instance.LogScreen("instruction", _screenOnset, Time.time - TRPulse.Instance.t0);
        movementPanel.SetActive(false);
        instructionPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;

        ShowMovementInstruction();
    }

    public override void OnMovementButton()
    {
        DataLogger.Instance.LogScreen("movement", _screenOnset, Time.time - TRPulse.Instance.t0);
        movementPanel.SetActive(false);
        instructionPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;
        cameraManager.StartNewConfiguration(rewardManager.GetCurrentConfigIndex());
    }

    public override void OnFeedbackButton()
    {
        DataLogger.Instance.LogScreen("feedback", _screenOnset, Time.time - TRPulse.Instance.t0);
        movementPanel.SetActive(false);
        instructionPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;
        NewSequenceInstructions();
    }

    public override void OnContinueButton()
    {
        DataLogger.Instance.LogScreen("new_sequence", _screenOnset, Time.time - TRPulse.Instance.t0);
        instructionPanel.SetActive(false);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;
        rewardManager.StartNextConfiguration();
    }

    public override void EndScreen()
    {
        endScreenPanel?.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnEndScreenButton()
    {
        SceneSequenceManager.Instance.GoToTask2();
    }
}
