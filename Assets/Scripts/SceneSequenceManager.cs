using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class SceneSequenceManager : MonoBehaviour
{
    public static SceneSequenceManager Instance { get; private set; }
    private const string part1 = "Part 1";
    private const string part2 = "Part 2";


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
    public void GoToTask()
    {
        Debug.Log("[SceneSequence] Loading TaskPhase");
        LoadScene(part1);  
    }

    public void GoToTask2()
    {
        Debug.Log("[SceneSequence] Loading CuePhase");
        LoadScene(part2); 
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


}
