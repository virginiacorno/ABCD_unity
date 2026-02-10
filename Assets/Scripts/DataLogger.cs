using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class DataLogger : MonoBehaviour
{
    private static DataLogger _instance;
    public static DataLogger Instance => _instance;

    [Header("Participant Info")]
    public string participantID;
    public string studyID;
    public string sessionID;

    private string dataFilePath;
    private StreamWriter csvWriter;
    private float taskStartTime;

    //V: create column headers
    private List<string> columnHeaders = new List<string>
    {
        "event_type",
        "participant",
        "study_id",
        "session_id",
        "session",
        "date",
        "round",
        "rep",
        "t_global",
        "t_curr_run",
        "key_pressed",
        "key_index",
        "movement_complete",
        "curr_loc_x",
        "curr_loc_y",
        "from_x",
        "from_y",
        "length_step",
        "direction",
        "curr_rew_x",
        "curr_rew_y",
        "type",
        "state",
        "found_reward",
        "movement_index",
        "reward_letter",
        "reward_index",
        "moves_to_find",
        "cue_displayed",
        "cue_time",
        "trial_type",
        "sequence",
        "start_loc_x",
        "start_loc_y",
        "reward_onset_time",
        "reward_offset_time",
        "memorization_phase",
        "repetition_number",
        "ISI", //V: not currently needed but maybe later
        "TR_received", 
        "TR_time"
    };

    void Awake()
    {
        if (_instance != null && _instance != this) //V: if a DataLogger already exists and it's not this one, then destroy it so only 1 data logger exists
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void InitializeWithInfo(string participant, string study, string session) //V: create csv file populated with info from participant dialog
    {
        participantID = participant;
        studyID = study;
        sessionID = session;

        taskStartTime = Time.realtimeSinceStartup;

#if !UNITY_WEBGL
        // Create data directory (file I/O only works outside the browser)
        string dataDir = Path.Combine(Application.dataPath, "..", "data");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        // Create filename with timestamp
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        dataFilePath = Path.Combine(dataDir,
            $"{participantID}_{studyID}_{timestamp}_results.csv");

        // Initialize CSV
        csvWriter = new StreamWriter(dataFilePath, false);
        csvWriter.WriteLine(string.Join(",", columnHeaders));
        csvWriter.Flush();

        Debug.Log($"Data logging initialized. File: {dataFilePath}");
#else
        Debug.Log("WebGL build: file logging disabled, data handled by Pavlovia");
#endif
    }

    public void LogEvent(Dictionary<string, object> data)
    {
        //V: add this data if not provided earlier
        if (!data.ContainsKey("participant")) data["participant"] = participantID;
        if (!data.ContainsKey("study_id")) data["study_id"] = studyID;
        if (!data.ContainsKey("session_id")) data["session_id"] = sessionID;
        if (!data.ContainsKey("session")) data["session"] = "001";
        if (!data.ContainsKey("date")) data["date"] = DateTime.Now.ToString("yyyy-MM-dd");
        if (!data.ContainsKey("t_global")) data["t_global"] = Time.realtimeSinceStartup;

#if !UNITY_WEBGL
        if (csvWriter == null)
        {
            Debug.LogError("DataLogger not initialized!");
            return;
        }

        // Build row
        List<string> rowValues = new List<string>();
        foreach (string header in columnHeaders)
        {
            if (data.ContainsKey(header)) //V: check if there is row value for each data type in the header, othrwise add empty
            {
                rowValues.Add(EscapeCSV(data[header].ToString()));
            }
            else
            {
                rowValues.Add("");
            }
        }

        csvWriter.WriteLine(string.Join(",", rowValues)); //V: join all values comma-separated (csv format)
        csvWriter.Flush(); // Write immediately for fMRI safety
#endif
    }

    private string EscapeCSV(string value) //V: function to handle special characters
    {
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    public float GetCurrentRunTime() //V: get seconds since task started
    {
        return Time.realtimeSinceStartup - taskStartTime;
    }

    void OnApplicationQuit()
    {
#if !UNITY_WEBGL
        if (csvWriter != null)
        {
            csvWriter.Close();
            Debug.Log($"Data saved to: {dataFilePath}");
        }
#endif
    }
}
