using UnityEngine;
using TMPro;
 using UnityEngine.UI;

public class ParticipantDialogue : MonoBehaviour
{
    public TMP_InputField participantIdInput;
    public TMP_InputField taskHalfInput;
    public Button confirmButton;
    public GameObject dialogPanel;
    public TaskInstructionManagerBase instructionManager;

    void Start()
    {
        Time.timeScale = 0f;
    }

    public void OnConfirmButton()
    {
        DataLogger.Instance.Initialise(participantIdInput.text, taskHalfInput.text);
        dialogPanel.SetActive(false);
        instructionManager?.ShowInstructionPanel();
    }
}
