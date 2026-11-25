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

    private float _screenOnset;
    private bool _canAdvance; //V: set to false in the first instruction screen, becomes true after 5TRs detected
    private float _screenShownAt;
    private const float _feedbackDuration = 2.5f; //V: feedback screen is fixed-length, not keypress-dismissed, to keep BOLD timing consistent

    void Start()
    {
        instructionPanel.SetActive(false);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        endScreenPanel?.SetActive(false);

        //V: after a page reload, a part may already be finished (played tasks are remembered) - skip straight past it
        if (!TaskPackageManager.Instance.HasMoreTasks())
        {
            Debug.Log($"[InstructionManager] No tasks left in {SceneManager.GetActiveScene().name}, skipping it");
            if (SceneManager.GetActiveScene().name == "Part 3") EndScreen();
            else GoToNextPart();
            return;
        }

        DataLogger.Instance.StartTrial();
        ShowInstruction();
    }

    void Update() //V: runs every frame checking for key presses (all but ey 5 because that's the scanner trigger)
    {
        if (InactivityManager.Blocking) return;
        if (!_canAdvance) return;
        if (Time.unscaledTime - _screenShownAt < 0.5f) return;
        if (!Keyboard.current.anyKey.wasPressedThisFrame) return;
        if (Keyboard.current.digit5Key.wasPressedThisFrame) return;
        if (Keyboard.current.numpad5Key.wasPressedThisFrame) return;

        if (instructionPanel.activeSelf)   AdvanceInstruction();
        else if (movementPanel.activeSelf) AdvanceMovement();
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
        player.inputEnabled = false;
        _screenOnset = Time.unscaledTime - DataLogger.Instance.T0;
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
        player.inputEnabled = false;
        _screenOnset = Time.unscaledTime - DataLogger.Instance.T0;
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
        player.inputEnabled = false;
        _screenOnset = Time.unscaledTime - DataLogger.Instance.T0;
        _screenShownAt = Time.unscaledTime;
        StartCoroutine(AutoAdvanceFeedback());
    }

    //V: feedback screen isn't keypress-dismissed; it always advances after a fixed real-time duration
    private System.Collections.IEnumerator AutoAdvanceFeedback()
    {
        yield return new WaitForSecondsRealtime(_feedbackDuration);
        if (feedbackPanel.activeSelf) AdvanceFeedback();
    }

    public void NewSequenceInstructions()
    {
        newSeqPanel.SetActive(true);
        movementPanel.SetActive(false);
        instructionPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        Time.timeScale = 0f;
        player.inputEnabled = false;
        _screenOnset = Time.unscaledTime - DataLogger.Instance.T0;
        _canAdvance = true;
        _screenShownAt = Time.unscaledTime;
    }

    //V: button callbacks - define what happens after each screen is dismissed
    public void AdvanceInstruction()
    {
        DataLogger.Instance.LogScreen("instruction", _screenOnset, Time.unscaledTime - DataLogger.Instance.T0);
        instructionPanel.SetActive(false);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;
        ShowMovementInstruction();
    }

    public void AdvanceMovement()
    {
        DataLogger.Instance.LogScreen("movement", _screenOnset, Time.unscaledTime - DataLogger.Instance.T0);
        instructionPanel.SetActive(false);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;

        //V: first task of the part is drawn here, from the tasks this participant hasn't played yet
        int firstConfig = rewardManager.PickNextConfig();
        if (firstConfig == -1)
        {
            Debug.LogError("[InstructionManager] No tasks left to play for this participant");
            EndScreen();
            return;
        }
        cameraManager.StartNewConfiguration(firstConfig);
    }

    public void AdvanceFeedback()
    {
        DataLogger.Instance.LogScreen("feedback", _screenOnset, Time.unscaledTime - DataLogger.Instance.T0);
        instructionPanel.SetActive(false);
        movementPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        newSeqPanel.SetActive(false);
        Time.timeScale = 1f;
        NewSequenceInstructions();
    }

    public void AdvanceNewSequence()
    {
        DataLogger.Instance.LogScreen("new_sequence", _screenOnset, Time.unscaledTime - DataLogger.Instance.T0);
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
        player.inputEnabled = false;
        _screenOnset = Time.unscaledTime - DataLogger.Instance.T0; //V: so the end screen gets logged with its onset like the other screens
        _canAdvance = true;
        _screenShownAt = Time.unscaledTime;
    }

    public void AdvanceEndScreen()
    {
        DataLogger.Instance.LogScreen("end_screen", _screenOnset, Time.unscaledTime - DataLogger.Instance.T0); //V: logged before GoToNextPart so in Part 3 it reaches Pavlovia before ABCD_DONE
        GoToNextPart();
    }

    //V: Part 1 -> Part 2 -> Part 3 -> tell Pavlovia the task is done
    void GoToNextPart()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "Part 1")      SceneSequenceManager.Instance.GoToTask2();
        else if (scene == "Part 2") SceneSequenceManager.Instance.GoToTask3();
        else if (scene == "Part 3") Debug.Log("ABCD_DONE");
    }
}
