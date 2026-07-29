using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class InactivityManager : MonoBehaviour
{
    public GameObject inactivityPanel;
    public TMP_Text countdownText;
    public TMP_Text warningText;
    public Button continueButton;
    public moveplayer player;

    public float inactivityThreshold = 120f; //V: how much inactivity time do we tolerate?
    public float dismissThreshold = 60f; //V: how much time participant has to confirm they are still playing
    private float lastInputTime;
    private float panelShownTime; //V: later used to keep track of whether 1 min has passed since panel shown
    private bool panelVisible = false;
    private float savedTimeScale = 1f;
    public static bool Blocking = false; //V: true whenever the inactivity panel (warning or "session ended") is on screen, checked by other managers so they don't advance underneath it

    //V: escalation, mirrors the outer Pavlovia page's global inactivity monitor
    public int maxConsecutiveWarnings = 2; //V: redirect instead of showing a warning right after the previous one was resumed
    public int maxTotalWarnings = 4;       //V: redirect instead of showing one more warning than this in the whole session
    private int consecutiveResumedThenInactive = 0;
    private int totalWarnings = 0;
    private bool lastClickedContinue = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() 
    {
        lastInputTime = Time.unscaledTime;
        inactivityPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (AnyInput())
        {
            lastInputTime = Time.unscaledTime; //V: 
        }

        //V: if inactivity > inactivityThreshold then show the panel
        if (!panelVisible && Time.unscaledTime - lastInputTime >= inactivityThreshold)
        {
            ShowPanel(); //V: function to show panel and keep track of how long panel is visible for (or end the session if we've escalated past the limits)
        }

        //V: if the panel has been visible longer than the dimiss threshold, then end the game
        if (panelVisible && Time.unscaledTime - panelShownTime >= dismissThreshold)
        {
            EndForInactivity("timeout");
        }
        else if (panelVisible && countdownText != null)
        {
            float remaining = dismissThreshold - (Time.unscaledTime - panelShownTime);
            countdownText.text = Mathf.CeilToInt(remaining).ToString() + "s";
        }
    }

    bool AnyInput()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        return (kb != null && kb.anyKey.wasPressedThisFrame) ||
        (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }

    //V: show the panel, preserve the current timescale (so if participant is genuinely thinking and keeps playing we don't lose time data)
    // record what time panel was shown for later comparison and log this
    void ShowPanel()
    {
        //V: escalation bookkeeping, mirrors the outer page's global inactivity monitor
        totalWarnings++;
        if (lastClickedContinue) consecutiveResumedThenInactive++;
        lastClickedContinue = false;

        if (consecutiveResumedThenInactive >= maxConsecutiveWarnings || totalWarnings > maxTotalWarnings)
        {
            EndForInactivity(consecutiveResumedThenInactive >= maxConsecutiveWarnings ? "consecutive" : "total");
            return;
        }

        savedTimeScale = Time.timeScale;
        panelVisible = true;
        panelShownTime = Time.unscaledTime;
        inactivityPanel.SetActive(true);
        Blocking = true;
        Time.timeScale = 0f;
        player.inputEnabled = false;
        float t = Time.unscaledTime - DataLogger.Instance.T0;
        DataLogger.Instance.LogScreen("inactivity_warning", t, t);
    }

    public void OnContinueButton()
    {
        panelVisible = false;
        lastClickedContinue = true;
        lastInputTime = Time.unscaledTime;
        inactivityPanel.SetActive(false);
        Blocking = false;
        Time.timeScale = savedTimeScale;   // restore time scale so we don't lose data
        player.inputEnabled = true;
    }

    void EndForInactivity(string reason)
    {
        float t = Time.unscaledTime - DataLogger.Instance.T0;
        DataLogger.Instance.LogScreen("inactivity_" + reason, t, t);
        Debug.Log("ABCD_TIMEOUT");
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
        if (warningText != null)
            warningText.text = "Session ended due to inactivity";
        if (continueButton != null) continueButton.interactable = false;
        if (inactivityPanel != null) inactivityPanel.SetActive(true);
        Blocking = true; //V: also covers the escalation short-circuit path, where ShowPanel() never reaches its own Blocking = true
        panelVisible = false;
        Time.timeScale = 0f;
        player.inputEnabled = false;
        enabled = false;
    }

}
