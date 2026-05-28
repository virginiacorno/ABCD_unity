using UnityEngine;

public class ClassicTaskInstructionManager : TaskInstructionManagerBase
{
    public CameraManager cameraManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ShowInstruction(); //V: call this as it is bc working mechanism already accurate in other script
    }

    //V: update the behaviour of buttons
    public override void OnInstructionButton()
    {
        DataLogger.Instance.LogScreen("instruction", "button_press");
        DataLogger.Instance.LogScreen("instruction", "offset");
        movementPanel.SetActive(false);
        instructionPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;

        ShowMovementInstruction();
    }

    public override void OnMovementButton()
    {
        DataLogger.Instance.LogScreen("movement", "button_press");
        DataLogger.Instance.LogScreen("movement", "offset");
        movementPanel.SetActive(false);
        instructionPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;
        cameraManager.StartNewConfiguration(0);
    }

    public override void OnFeedbackButton()
    {
        DataLogger.Instance.LogScreen("feedback", "button_press");
        DataLogger.Instance.LogScreen("feedback", "offset");
        movementPanel.SetActive(false);
        instructionPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;
        NewSequenceInstructions();
    }

    public override void OnContinueButton()
    {
        DataLogger.Instance.LogScreen("new_sequence", "button_press");
        DataLogger.Instance.LogScreen("new_sequence", "offset");
        instructionPanel.SetActive(false);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;
        rewardManager.StartNextConfiguration();
    }

    //V: call loading of the next scene instead of displaying an end screen
    public override void EndScreen()
    {
        SceneSequenceManager.Instance.GoToCueTask();
    }
}
