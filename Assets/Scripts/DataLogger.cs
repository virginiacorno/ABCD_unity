using UnityEngine;
using System.IO;
using System.Text;

public class DataLogger : MonoBehaviour
{
    public static DataLogger Instance { get; private set; }

    private float t0;
    private StreamWriter writer;
    private string participantId;
    private string taskHalf;

    public string ParticipantId => participantId;
    public string TaskHalf => taskHalf;

    public bool isInitialised = false;
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; };
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetT0(float value)
    {
        // V: initialise t0 as 0, then call function from TRPulse to set it to actual value
        t0 = value;
    }

    public void Initialise(string participantId, string taskHalf)
    {
        this.participantId = participantId;
        this.taskHalf = taskHalf;

        string homeDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        string dir = Path.Combine(homeDir, "ABCD_data", participantId, "beh");
        Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, $"{participantId}_fmri_pt{taskHalf}.csv");
        writer = new StreamWriter(path, append: false, encoding: System.Text.Encoding.UTF8);

        writer.WriteLine(
            "event_type,participant_id,task_half,t_global," +
            "screen_name,phase," +
            "from_camera,to_camera," +
            "movement_type,t_step_press_global,t_step_press_curr_run,length_step," +
            "curr_loc_x,curr_loc_z,t_step_end_global," +
            "curr_rew_x,curr_rew_z,state,type,trial,task," +
            "t_reward_start,reward_delay,reward_found," +
            "from_rotation,to_rotation," +
            "distance," +
            "pulse_number"
        );
        
        writer.Flush();
        isInitialised = true;
    }

    private struct LogRow
    {
        public string eventType, participantId, taskHalf;
        public float tGlobal;
        public string screenName, phase;
        public string fromCamera, toCamera;
        public string movementType;
        public float tStepPressGlobal, tStepPressCurrRun, lengthStep;
        public float currLocX, currLocZ, tStepEndGlobal;
        public float currRewX, currRewZ;
        public string state, type;
        public int trial;
        public string task; //V: need to add some kind of task name/id to log here
        public float tRewardStart, rewardDelay;
        public bool rewardFound;
        public float fromRotation, toRotation;
        public float distance;
        public int pulseNumber;
    }

    private void WriteRow(LogRow r)
    {
        if (!isInitialised) return;
        writer.WriteLine(
            $"{r.eventType},{r.participantId},{r.taskHalf},{r.tGlobal}," +
            $"{r.screenName},{r.phase}," +
            $"{r.fromCamera},{r.toCamera}," +
            $"{r.movementType},{r.tStepPressGlobal},{r.tStepPressCurrRun},{r.lengthStep}," +
            $"{r.currLocX},{r.currLocZ},{r.tStepEndGlobal}," +
            $"{r.currRewX},{r.currRewZ},{r.state},{r.type},{r.trial},{r.task}," +
            $"{r.tRewardStart},{r.rewardDelay},{r.rewardFound}," +
            $"{r.fromRotation},{r.toRotation}," +
            $"{r.distance}," +
            $"{r.pulseNumber}"
        );
        writer.Flush();
    }

    public void LogScreen(string screenName, string phase)
    {
        var row = new LogRow();
        row.eventType = "screen";
        row.participantId = participantId;
        row.taskHalf = taskHalf;
        row.tGlobal = Time.time - t0;
        row.screenName = screenName;
        row.phase = phase;
        WriteRow(row);
    }

    public void LogCameraTransition(string phase, string fromCamera, string toCamera)
    {
        var row = new LogRow();
        row.eventType = "camera_transition";
        row.participantId = participantId;
        row.taskHalf = taskHalf;
        row.tGlobal = Time.time - t0;
        row.phase = phase;
        row.fromCamera = fromCamera;
        row.toCamera = toCamera;
        WriteRow(row);
    }

    public void LogStep(float tStepPressGlobal, float tStepPressCurrRun, float lengthStep,
        float currLocX, float currLocZ, float tStepEndGlobal,
        float currRewX, float currRewZ, string state, string type, int trial, string task)
    {
        var row = new LogRow();
        row.eventType = "movement";
        row.participantId = participantId;
        row.taskHalf = taskHalf;
        row.tGlobal = tStepPressGlobal;
        row.movementType = "step";
        row.tStepPressGlobal = tStepPressGlobal;
        row.tStepPressCurrRun = tStepPressCurrRun;
        row.lengthStep = lengthStep;
        row.currLocX = currLocX;
        row.currLocZ = currLocZ;
        row.tStepEndGlobal = tStepEndGlobal;
        row.currRewX = currRewX;
        row.currRewZ = currRewZ;
        row.state = state;
        row.type = type;
        row.trial = trial;
        row.task = task;
        WriteRow(row);
    }

    public void LogRotation(float rotationPressGlobal, float rotationPressCurrRun,
        float fromRotation, float toRotation,
        float currLocX, float currLocZ,
        float currRewX, float currRewZ,
        string state, string type, int trial, string task)
    {
        var row = new LogRow();
        row.eventType = "movement";
        row.participantId = participantId;
        row.taskHalf = taskHalf;
        row.tGlobal = rotationPressGlobal;
        row.movementType = "rotation";
        row.tStepPressGlobal = rotationPressGlobal;
        row.tStepPressCurrRun = rotationPressCurrRun;
        row.fromRotation = fromRotation;
        row.toRotation = toRotation;
        row.currLocX = currLocX;
        row.currLocZ = currLocZ;
        row.currRewX = currRewX;
        row.currRewZ = currRewZ;
        row.state = state;
        row.type = type;
        row.trial = trial;
        row.task = task;
        WriteRow(row);
    }

    public void LogRewardCheck(float currLocX, float currLocZ, float currRewX, float currRewZ,
        float distance, string state, string type, int trial, string task,
        bool rewardFound, float tRewardStart = 0f, float rewardDelay = 0f)
    {
        var row = new LogRow();
        row.eventType = "reward_check";
        row.participantId = participantId;
        row.taskHalf = taskHalf;
        row.tGlobal = Time.time - t0;
        row.currLocX = currLocX;
        row.currLocZ = currLocZ;
        row.currRewX = currRewX;
        row.currRewZ = currRewZ;
        row.distance = distance;
        row.state = state;
        row.type = type;
        row.trial = trial;
        row.task = task;
        row.rewardFound = rewardFound;
        row.tRewardStart = tRewardStart;
        row.rewardDelay = rewardDelay;
        WriteRow(row);
    }

    void OnApplicationQuit()
    {
        if (writer != null)
        {
            writer.Flush();
            writer.Close();
        }
    }

}
