using UnityEngine;
using TMPro;
 using UnityEngine.UI;

public class ParticipantDialogue : MonoBehaviour
{
    public TMP_InputField participantIdInput;
    public TMP_InputField taskHalfInput;
    public Button confirmButton;

    void Start()
    {
        Time.timeScale = 0f;
    }

    public void OnConfirmButton()
    {
        //V: add inputs to datalogger so it knows how to complete our rows
        DataLogger.Instance.Initialise(participantIdInput.text, taskHalfInput.text);

        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }
}
