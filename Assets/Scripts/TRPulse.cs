using UnityEngine;
using UnityEngine.InputSystem; 

public class TRPulse : MonoBehaviour
{
    public InstructionScreenManager instructionManager;
    public DataLogger dataLogger;

    private bool triggered = false;

    void Update()
    {
        if (triggered) return; //V: stop if trigger has already been detected

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit5Key.wasPressedThisFrame)
        {
            triggered = true;

            float triggerTime = dataLogger.GetCurrentRunTime();
            dataLogger.LogEvent(new System.Collections.Generic.Dictionary<string, object>
            {
                {"event_type", "scan_trigger"},
                {"t_curr_run", triggerTime}
            });

            instructionManager.ShowInstruction();
        }
    }
}
