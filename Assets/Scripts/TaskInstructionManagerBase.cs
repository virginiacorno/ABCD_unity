using UnityEngine;
using TMPro;

//V: script creates and manages instruction, new seqeunces and end panels used in both task variants + creates button object
//V: task-specific scripts manage what should follow the button presses
public abstract class TaskInstructionManagerBase : MonoBehaviour
{
    public GameObject instructionPanel;
    public GameObject movementPanel;
    public GameObject feedbackPanel;
    public TMP_Text feedbackText;
    public GameObject newSeqPanel;
    public rewardManager rewardManager;
    public moveplayer player;

    public void ShowInstruction()
    {
        instructionPanel.SetActive(true);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 0f;
        DataLogger.Instance.LogScreen("instruction", "onset");
    }

    public void ShowMovementInstruction()
    {
        movementPanel.SetActive(true);
        instructionPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 0f;
        DataLogger.Instance.LogScreen("movement", "onset");
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
        DataLogger.Instance.LogScreen("feedback", "onset");
    }

    public void NewSequenceInstructions()
    {
        newSeqPanel.SetActive(true);
        movementPanel.SetActive(false);
        instructionPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        Time.timeScale = 0f;
        DataLogger.Instance.LogScreen("new_sequence", "onset");
    }

    public abstract void OnInstructionButton();
    public abstract void OnMovementButton();
    public abstract void OnFeedbackButton();
    public abstract void OnContinueButton();
    public abstract void EndScreen();
}