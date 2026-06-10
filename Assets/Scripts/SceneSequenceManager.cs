using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class SceneSequenceManager : MonoBehaviour
{
     public static SceneSequenceManager Instance { get; private set; }

    private const string practicePhase = "PracticeTrial";
    private const string taskPhase = "Part 1";
    private const string cuePhase = "Part 2";

    [Header("Player State")]
    public int instructionCorrectStreak = 0;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[SceneSequence] Manager initialized");
    }

    public void GoToPracticePhase()
    {
        Debug.Log("[SceneSequence] Loading PracticePhase");
        LoadScene(practicePhase);
    }

    public void GoToTask()
    {
        Debug.Log("[SceneSequence] Loading TaskPhase");
        LoadScene(taskPhase);  
    }

    public void GoToCueTask()
    {
        Debug.Log("[SceneSequence] Loading CuePhase");
        LoadScene(cuePhase); 
    }

    void LoadScene(string sceneName)
    {
#if UNITY_EDITOR
        // In Editor: use EditorSceneManager (works without Build Settings)
        EditorSceneManager.LoadSceneInPlayMode(
            $"Assets/Scenes/{sceneName}.unity",
            new LoadSceneParameters(LoadSceneMode.Single)
        );
#else
        // In Build: use standard SceneManager (requires Build Settings)
        SceneManager.LoadScene(sceneName);
#endif
    }

    public void IncrementInstructionStreak()
    {
        instructionCorrectStreak++;
        Debug.Log($"[SceneSequence] Instruction streak: {instructionCorrectStreak}");
    }
}
