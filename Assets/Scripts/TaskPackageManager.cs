using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class GridPosition
{
    public float x, y, z;
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[System.Serializable]
public class TaskConfig
{
    public string configName;
    public List<GridPosition> rewardPositions; //V: always traversed forward (A -> last); backward trials were removed
}

[System.Serializable]
public class PackageData
{
    public List<TaskConfig> tasks;
    public int trialsPerTask;
    public int tasksPerPart;
    public float gridStepSize;
}

//V: one entry per completed task
[System.Serializable]
public class PlayedTask
{
    public string configName;
    public int part;
}

[System.Serializable]
public class PlayedHistory
{
    public List<PlayedTask> played = new List<PlayedTask>();
}

[DefaultExecutionOrder(-10)] //V: ensures the Awake() in this script runs before any other Awake()

//V: online version of the fMRI randomisation: every part draws its tasks at random from the tasks this participant
//V: hasn't played yet (in any part). The played list lives in memory (survives scene changes) and is mirrored to
//V: browser storage (PlayerPrefs -> IndexedDB on WebGL), keyed by participant, so a page reload doesn't replay tasks.
public class TaskPackageManager : MonoBehaviour
{
    public static TaskPackageManager Instance { get; private set; } //V: { get; private set; } ensures anyone can read BUT only code in taskPackagemanager class can write
    public PackageData Data { get; private set; }
    public int CurrentPart { get; private set; }

    private PlayedHistory _history = new PlayedHistory();
    private string _historyKey = null; //V: PlayerPrefs key the history is saved under; null until the participant is known
    private const string _keyPrefix = "ABCD_played_";
    private const int _defaultTasksPerPart = 3;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (Display.displays.Length > 1) //V: activate second display
        {
            Display.displays[1].Activate(3840, 2160, 120);
            Application.targetFrameRate = 120;
        }

        LoadTasks();
    }

    void LoadTasks()
    {
        string resourcePath = "Tasks/fMRI_tasks_trial"; //V: TEMPORARY short test (1 task per part, 2 trials) - switch back to "Tasks/fMRI_tasks_new" for the real study
        TextAsset taskFile = Resources.Load<TextAsset>(resourcePath);

        if (taskFile == null)
        {
            Debug.LogError($"[PackageManager] Could not load: Resources/{resourcePath}");
            return;
        }

        Data = JsonUtility.FromJson<PackageData>(taskFile.text);
        if (Data.tasksPerPart <= 0)
        {
            Debug.LogWarning($"[PackageManager] tasksPerPart missing from JSON, using {_defaultTasksPerPart}");
            Data.tasksPerPart = _defaultTasksPerPart;
        }
        Debug.Log($"[PackageManager] Loaded {Data.tasks.Count} tasks, {Data.tasksPerPart} per part");
    }

    //V: called by rewardManager when a Part scene loads; also (re)loads the saved history once the participant is known
    public void BeginPart(int part)
    {
        CurrentPart = part;
        DataLogger.Instance?.SetPart(part);
        LoadHistoryForParticipant();

        Debug.Log($"[PackageManager] Part {part}, {TasksLeftInPart(part)} task(s) to play, already played: " +
                  string.Join(", ", _history.played.Select(p => $"{p.configName} (pt{p.part})")));
    }

    //V: the key uses the full info string from Pavlovia (participant|study|session) so a re-run of the same
    //V: participant ID in a new session starts fresh, while a reload within the same session resumes
    void LoadHistoryForParticipant()
    {
#if UNITY_EDITOR
        return; //V: in-memory only in the Editor, otherwise TEST_PARTICIPANT would run out of tasks after one play-through
#else
        string info = DataLogger.Instance != null ? DataLogger.Instance.ParticipantInfo : null;
        if (string.IsNullOrEmpty(info))
        {
            Debug.LogWarning("[PackageManager] Participant info not received yet - played tasks kept in memory only for now");
            return;
        }

        string key = _keyPrefix + info;
        if (key == _historyKey) return; //V: already loaded for this participant

        //V: merge what's saved with anything played before the participant info arrived, then save the union
        PlayedHistory saved = ReadHistory(key);
        foreach (PlayedTask p in _history.played)
            if (!saved.played.Any(s => s.configName == p.configName)) saved.played.Add(p);

        _history = saved;
        _historyKey = key;
        SaveHistory();
#endif
    }

    static PlayedHistory ReadHistory(string key)
    {
        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json)) return new PlayedHistory();
        return JsonUtility.FromJson<PlayedHistory>(json) ?? new PlayedHistory();
    }

    void SaveHistory()
    {
        if (_historyKey == null) return;
        PlayerPrefs.SetString(_historyKey, JsonUtility.ToJson(_history));
        PlayerPrefs.Save(); //V: on WebGL this flushes to IndexedDB straight away
    }

    //V: randomly pick among all tasks not yet played by this participant (in any part)
    public string PickNextConfigName()
    {
        List<string> remaining = UnplayedConfigNames();
        if (remaining.Count == 0) return null;

        string pick = remaining[Random.Range(0, remaining.Count)];
        Debug.Log($"[PackageManager] Picked {pick} from: {string.Join(", ", remaining)}");
        return pick;
    }

    //V: called when all trials of a task are done; saved straight away
    public void MarkPlayed(string configName)
    {
        _history.played.Add(new PlayedTask { configName = configName, part = CurrentPart });
        SaveHistory();
    }

    public bool HasMoreTasks() => TasksLeftInPart(CurrentPart) > 0;

    //V: 1-based number of the task currently being played in this part (for the Pavlovia progress display)
    public int CurrentTaskNumber() => _history.played.Count(p => p.part == CurrentPart) + 1;

    //V: tasks still to play in a part (counted from the history, so a reload mid-part doesn't reset it)
    int TasksLeftInPart(int part)
    {
        int playedThisPart = _history.played.Count(p => p.part == part);
        int left = Data.tasksPerPart - playedThisPart;
        return Mathf.Max(0, Mathf.Min(left, UnplayedConfigNames().Count));
    }

    List<string> UnplayedConfigNames() =>
        Data.tasks
            .Select(t => t.configName)
            .Distinct()
            .Where(n => !_history.played.Any(p => p.configName == n))
            .ToList();

    public List<TaskConfig> GetAllTasks() => Data.tasks.ToList();
}
