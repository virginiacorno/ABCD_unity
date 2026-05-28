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
    public int taskPart;
    public List<GridPosition> rewardPositions;
    public bool IsBackw => configName != null && configName.StartsWith("backw");
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
    public int AssignedPackageNumber { get; private set; }
    public PackageData Data { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        AssignAndLoadPackage();
    }

    void AssignAndLoadPackage()
    {
        AssignedPackageNumber = Random.Range(1, 4); //V: min and max values are integers so max value is exclusive
        string resourcePath = $"Tasks/p{AssignedPackageNumber:D2}";
        TextAsset packageFile = Resources.Load<TextAsset>(resourcePath);

        if (packageFile == null)
        {
            Debug.LogError($"[PackageManager] Could not load: Resources/{resourcePath}");
            return;
        }

        Data = JsonUtility.FromJson<PackageData>(packageFile.text);
        Debug.Log($"[PackageManager] Assigned package {AssignedPackageNumber}, loaded {Data.tasks.Count} tasks");
    }

    public List<TaskConfig> GetPart1Tasks() => Data.tasks.Where(t => t.taskPart == 1).ToList();
    public List<TaskConfig> GetPart2Tasks() => Data.tasks.Where(t => t.taskPart == 2).ToList();
    public string GetPackageId() => $"p{AssignedPackageNumber:D2}";
}
