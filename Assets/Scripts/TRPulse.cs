using UnityEngine;
using UnityEngine.InputSystem; 

public class TRPulse : MonoBehaviour
{
    public ClassicTaskInstructionManager instructionManager;

    public static TRPulse Instance { get; private set; }

    public float t0 = 0;

    public int counter = 0; //V: task shuold be triggered after 5th TR detected bc first 5 TRs should be excluded in FSL analyses

    // Update is called once per frame
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

    }

    void Update()
    {
        if (DataLogger.Instance != null && DataLogger.Instance.isInitialised)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.digit5Key.wasPressedThisFrame) //V: add log calls once updated the logger
            {
                if (t0 == 0f)
                {
                    if (counter < 4)
                    {
                        counter++;
                    }

                    else
                    {
                        t0 = Time.time;
                        DataLogger.Instance.SetT0(t0);
                        DataLogger.Instance.LogPulse();
                        instructionManager.ShowInstruction();
                    }
                }
            }
        }
    }
}
