using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LeaderboardEntry
{
    public string name;
    public int score;

    public LeaderboardEntry(string name, int score)
    {
        this.name = name;
        this.score = score;
    }
}

[Serializable]
class LeaderboardData
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    const string SaveKey = "Leaderboard";
    const int MaxEntries = 5;

    public List<LeaderboardEntry> Entries { get; private set; } = new List<LeaderboardEntry>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void AddEntry(string playerName, int score)
    {
        Entries.Add(new LeaderboardEntry(playerName.ToUpper(), score));
        Entries.Sort((a, b) => b.score.CompareTo(a.score));
        if (Entries.Count > MaxEntries)
            Entries.RemoveRange(MaxEntries, Entries.Count - MaxEntries);
        Save();
    }

    void Save()
    {
        var data = new LeaderboardData { entries = Entries };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;
        var data = JsonUtility.FromJson<LeaderboardData>(json);
        if (data != null) Entries = data.entries;
    }
}