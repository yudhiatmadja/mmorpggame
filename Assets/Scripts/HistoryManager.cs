// File: HistoryManager.cs
using UnityEngine;
using System.IO;

public static class HistoryManager
{
    private static string savePath = Path.Combine(Application.persistentDataPath, "aichathistory.json");

    public static void SaveHistory(ConversationHistory data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("History saved to: " + savePath);
    }

    public static ConversationHistory LoadHistory()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            Debug.Log("History loaded.");
            return JsonUtility.FromJson<ConversationHistory>(json);
        }

        Debug.Log("No history file found, creating a new one.");
        return new ConversationHistory();
    }
}