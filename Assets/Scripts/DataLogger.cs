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
            "event_type,participant_id,task_half," +
            "t_start,t_start_curr_run,t_end,t_end_curr_run," +
            "screen_name," +
            "from_camera,to_camera," +
            "movement_type,length_step," +
            "curr_loc_x,curr_loc_z,to_loc_x,to_loc_z," +
            "curr_rew_x,curr_rew_z,state,type,trial,task," +
            "reward_delay,reward_found," +
            "from_rotation,to_rotation," +
            "distance"
        );
        
        writer.Flush();
        isInitialised = true;
    }

    private struct LogRow
    {
        public string eventType, participantId, taskHalf;
        public float tStart, tStartCurrRun, tEnd, tEndCurrRun;
        public string screenName;
        public string fromCamera, toCamera;
        public string movementType;
        public float lengthStep;
        public float currLocX, currLocZ, toLocX, toLocZ;
        public float currRewX, currRewZ;
        public string state, type;
        public int trial;
        public string task;
        public float rewardDelay;
        public bool rewardFound;
        public float fromRotation, toRotation;
        public float distance;
    }

    private void WriteRow(LogRow r)
    {
        if (!isInitialised) return;
        writer.WriteLine(
            $"{r.eventType},{r.participantId},{r.taskHalf}," +
            $"{r.tStart},{r.tStartCurrRun},{r.tEnd},{r.tEndCurrRun}," +
            $"{r.screenName}," +
            $"{r.fromCamera},{r.toCamera}," +
            $"{r.movementType},{r.lengthStep}," +
            $"{r.currLocX},{r.currLocZ},{r.toLocX},{r.toLocZ}," +
            $"{r.currRewX},{r.currRewZ},{r.state},{r.type},{r.trial},{r.task}," +
            $"{r.rewardDelay},{r.rewardFound}," +
            $"{r.fromRotation},{r.toRotation}," +
            $"{r.distance}"
        );
        writer.Flush();
    }

    public void LogScreen(string screenName, float tStart, float tEnd)
    {
        var row = new LogRow();
        row.eventType = "screen";
        row.participantId = participantId;
        row.taskHalf = taskHalf;
        row.tStart = tStart;
        row.tEnd = tEnd;
        row.screenName = screenName;
        WriteRow(row);
    }

    public void LogCameraTransition(string fromCamera, string toCamera, float tStart, float tEnd)
    {
        var row = new LogRow();
        row.eventType = "camera_transition";
        row.participantId = participantId;
        row.taskHalf = taskHalf;
        row.tStart = tStart;
        row.tEnd = tEnd;
        row.fromCamera = fromCamera;
        row.toCamera = toCamera;
        WriteRow(row);
    }

    public void LogStep(float tStart, float tStartCurrRun, float tEnd, float tEndCurrRun,
        float lengthStep,
        float currLocX, float currLocZ, float toLocX, float toLocZ,
        float currRewX, float currRewZ, string state, string type, int trial, string task)
    {
        var row = new LogRow();
        row.eventType = "movement";
        row.participantId = participantId;
        row.taskHalf = taskHalf;
        row.tStart = tStart;
        row.tStartCurrRun = tStartCurrRun;
        row.tEnd = tEnd;
        row.tEndCurrRun = tEndCurrRun;
        row.movementType = "step";
        row.lengthStep = lengthStep;
        row.currLocX = currLocX;
        row.currLocZ = currLocZ;
        row.toLocX = toLocX;
        row.toLocZ = toLocZ;
        row.currRewX = currRewX;
        row.currRewZ = currRewZ;
        row.state = state;
        row.type = type;
        row.trial = trial;
        row.task = task;
        WriteRow(row);
    }

    public void LogRotation(float tStart, float tStartCurrRun, float tEnd, float tEndCurrRun,
        float fromRotation, float toRotation,
        float currLocX, float currLocZ,
        float currRewX, float currRewZ,
        string state, string type, int trial, string task, float lengthStep)
    {
        var row = new LogRow();
        row.eventType = "movement";
        row.participantId = participantId;
        row.taskHalf = taskHalf;
        row.tStart = tStart;
        row.tStartCurrRun = tStartCurrRun;
        row.tEnd = tEnd;
        row.tEndCurrRun = tEndCurrRun;
        row.movementType = "rotation";
        row.lengthStep = lengthStep;
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
        bool rewardFound, float tStart = 0f, float tStartCurrRun = 0f,
        float tEnd = 0f, float tEndCurrRun = 0f, float rewardDelay = 0f)
    {
        var row = new LogRow();
        row.eventType = "reward_check";
        row.participantId = participantId;
        row.taskHalf = taskHalf;
        row.tStart = tStart;
        row.tStartCurrRun = tStartCurrRun;
        row.tEnd = tEnd;
        row.tEndCurrRun = tEndCurrRun;
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
        row.rewardDelay = rewardDelay;
        WriteRow(row);
    }

    public void LogPulse()
    {
        var row = new LogRow();
        row.eventType = "pulse";
        row.participantId = participantId;
        row.taskHalf = taskHalf;
        row.tStart = 0f;
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

    void OnDestroy()
    {
        if (writer != null)
        {
            writer.Flush();
            writer.Close();
        }
    }


}
