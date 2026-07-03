using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InactivityManager : MonoBehaviour
{
    public GameObject inactivityPanel;
    public TMP_Text countdownText;

    public float inactivityThreshold = 120f; //V: how much inactivity time do we tolerate?
    public float dismissThreshold = 60f; //V: how much time participant has to confirm they are still playing
    private float lastInputTime;
    private float panelShownTime; //V: later used to keep track of whether 1 min has passed since panel shown
    private bool panelVisible = false; 
    private float savedTimeScale = 1f;
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
            ShowPanel(); //V: function to show panel and keep track of how long panel is visible for
        }

        //V: if the panel has been visible longer than the dimiss threshold, then end the game
        if (panelVisible && Time.unscaledTime - panelShownTime >= dismissThreshold)
        {
            EndForInactivity();
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
        savedTimeScale = Time.timeScale;
        panelVisible = true;
        panelShownTime = Time.unscaledTime;
        inactivityPanel.SetActive(true);
        Time.timeScale = 0f;
        float t = Time.time - DataLogger.Instance.T0;
        DataLogger.Instance.LogScreen("inactivity_warning", t, t);
    }

    public void OnContinueButton()
    {
        panelVisible = false;
        lastInputTime = Time.unscaledTime;
        inactivityPanel.SetActive(false);
        Time.timeScale = savedTimeScale;   // restore time scale so we don't lose data
    }

    void EndForInactivity()
    {
        float t = Time.time - DataLogger.Instance.T0;
        DataLogger.Instance.LogScreen("inactivity_timeout", t, t);
        Debug.Log("ABCD_TIMEOUT");
        if (countdownText != null)
            countdownText.text = "Session ended.";
        enabled = false;
    }

}
