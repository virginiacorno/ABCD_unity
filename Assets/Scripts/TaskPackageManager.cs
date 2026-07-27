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
    public string taskType;
    public int taskPart;
    public List<GridPosition> rewardPositions;
    public bool IsBackw => taskType != null && taskType.StartsWith("backw");
}

[System.Serializable]
public class PackageData
{
    public List<TaskConfig> tasks;
    public int trialsPerTask;
    public float gridStepSize;
}

[DefaultExecutionOrder(-10)] //V: ensures the Awake() in this script runs before any other Awake()

public class TaskPackageManager : MonoBehaviour
{
    public static TaskPackageManager Instance { get; private set; } //V: { get; private set; } ensures anyone can read BUT only code in taskPackagemanager class can write
    public PackageData Data { get; private set; }
    private List<string> _part1Order;
    private List<string> _part2Order;
    private int _part1Index = 0;
    private int _part2Index = 0;

    public int GetTasksDispensed(int part) => part == 1 ? _part1Index : _part2Index; //V: needed to display in pavlovia

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
        string resourcePath = "Tasks/fMRI_tasks_new";
        TextAsset taskFile = Resources.Load<TextAsset>(resourcePath);

        if (taskFile == null)
        {
            Debug.LogError($"[PackageManager] Could not load: Resources/{resourcePath}");
            return;
        }

        Data = JsonUtility.FromJson<PackageData>(taskFile.text);
        Debug.Log($"[PackageManager] Loaded {Data.tasks.Count} tasks");
        GenerateTaskOrders();
    }

    void GenerateTaskOrders()
    {
        _part1Order = Data.tasks
            .Where(t => t.taskPart == 1)
            .Select(t => t.configName)
            .Distinct()
            .OrderBy(_ => Random.value)
            .ToList();

        //V: check if first task is type forward, if not get first task in the shuffled order with type = "forw" and place it at index 0
        if (Data.tasks.First(t => t.taskPart == 1 && t.configName == _part1Order[0]).taskType != "forw")
        {
            string forwardConfig = _part1Order.First(name =>
                Data.tasks.First(t => t.taskPart == 1 && t.configName == name).taskType == "forw");
            _part1Order.Remove(forwardConfig);
            _part1Order.Insert(0, forwardConfig);
        }

        _part2Order = Data.tasks
            .Where(t => t.taskPart == 2)
            .Select(t => t.configName)
            .Distinct()
            .OrderBy(_ => Random.value)
            .ToList();
        
        if (Data.tasks.First(t => t.taskPart == 2 && t.configName == _part2Order[0]).taskType != "forw")
        {
            string forwardConfig = _part2Order.First(name =>
                Data.tasks.First(t => t.taskPart == 2 && t.configName == name).taskType == "forw");
            _part2Order.Remove(forwardConfig);
            _part2Order.Insert(0, forwardConfig);
        }

        Debug.Log($"[PackageManager] Part 1 order: {string.Join(", ", _part1Order)}");
        Debug.Log($"[PackageManager] Part 2 order: {string.Join(", ", _part2Order)}");
    }

    public string GetNextConfigName(int part)
    {
        Debug.Log($"[PackageManager] GetNextConfigName(part={part}) called from:\n{System.Environment.StackTrace}");
        if (part == 1)
        {
            if (_part1Index >= _part1Order.Count) return null;
            return _part1Order[_part1Index++];
        }
        else
        {
            if (_part2Index >= _part2Order.Count) return null;
            return _part2Order[_part2Index++];
        }
    }

    //V: check that all tasks for a given part have been played
    public bool HasMoreTasks(int part) =>
        part == 1 ? _part1Index < _part1Order.Count : _part2Index < _part2Order.Count;

    public List<TaskConfig> GetPart1Tasks() => Data.tasks.Where(t => t.taskPart == 1).ToList();
    public List<TaskConfig> GetPart2Tasks() => Data.tasks.Where(t => t.taskPart == 2).ToList();
}
