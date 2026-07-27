using UnityEngine;
using UnityEngine.InputSystem;

public class PracticeInstructionManager : MonoBehaviour
{
    public GameObject instructionPanel;
    public GameObject practicePanel;
    public moveplayer player;
    public PracticePhase practicePhase;

    private float _screenShownAt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instructionPanel.SetActive(true);
        practicePanel.SetActive(false);
        float t = Time.time - DataLogger.Instance.T0;
        DataLogger.Instance.LogScreen("practice_phase_start", t, t);
        Time.timeScale = 0f;
        _screenShownAt = Time.unscaledTime;
    }

    void Update() //V: advance current screen on any key press, mirrors InstructionManager's keyboard-driven progression
    {
        if (Time.unscaledTime - _screenShownAt < 0.5f) return;
        if (!Keyboard.current.anyKey.wasPressedThisFrame) return;

        if (instructionPanel.activeSelf) OnInstructionButton();
        else if (practicePanel.activeSelf) OnPracticeButton();
    }

    public void OnInstructionButton()
    {
        instructionPanel.SetActive(false);
        practicePanel.SetActive(false);
        Time.timeScale = 1f; //V: free exploration is real movement, not a static panel
        practicePhase.StartPractice();
    }

    //V: called by PracticePhase once free exploration ends (timer or space bar)
    public void ShowPracticePanel()
    {
        practicePanel.SetActive(true);
        instructionPanel.SetActive(false);
        Time.timeScale = 0f;
        _screenShownAt = Time.unscaledTime;
    }

    public void OnPracticeButton()
    {
        instructionPanel.SetActive(false);
        practicePanel.SetActive(false);
        Time.timeScale = 1f;

        practicePhase.StartCoroutine(practicePhase.RunPracticeLoop());
    }

}
