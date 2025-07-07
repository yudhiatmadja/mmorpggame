using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Newtonsoft.Json;

[System.Serializable]
public class JournalDatabase
{
    public List<JournalEntry> entries = new List<JournalEntry>();
    public Dictionary<string, UserPreferences> userPreferences = new Dictionary<string, UserPreferences>();
    public DateTime lastSaved;
    
    public JournalDatabase()
    {
        entries = new List<JournalEntry>();
        userPreferences = new Dictionary<string, UserPreferences>();
        lastSaved = DateTime.Now;
    }
}

[System.Serializable]
public class UserPreferences
{
    public string userId;
    public string preferredCharacterId;
    public bool autoSaveEnabled = true;
    public bool ttsEnabled = false;
    public float ttsVolume = 1.0f;
    public float ttsSpeed = 1.0f;
    public int ttsVoiceIndex = 0;
    public List<string> favoriteTopics = new List<string>();
    public DateTime lastActiveTime;
    
    public UserPreferences()
    {
        userId = SystemInfo.deviceUniqueIdentifier;
        lastActiveTime = DateTime.Now;
    }
}

public class JournalPersistence : MonoBehaviour
{
    [Header("Save Configuration")]
    public bool enableAutoSave = true;
    public float autoSaveInterval = 30f; // seconds
    public int maxEntriesPerUser = 1000;
    public int maxBackupFiles = 5;
    
    [Header("File Settings")]
    public string journalFileName = "journal_data.json";
    public string backupPrefix = "journal_backup_";
    public bool useEncryption = false;
    public string encryptionKey = "YourEncryptionKey";
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool showSaveNotifications = true;
    
    // Data
    private JournalDatabase journalDatabase;
    private string currentUserId;
    private string saveFilePath;
    private Coroutine autoSaveCoroutine;
    
    // Events
    public event Action<JournalEntry> OnEntryAdded;
    public event Action<JournalEntry> OnEntryUpdated;
    public event Action<string> OnEntryDeleted;
    public event Action OnDatabaseSaved;
    public event Action OnDatabaseLoaded;
    
    private void Start()
    {
        InitializePersistence();
        LoadJournalData();
        
        if (enableAutoSave)
        {
            StartAutoSave();
        }
    }
    
    private void InitializePersistence()
    {
        currentUserId = SystemInfo.deviceUniqueIdentifier;
        saveFilePath = Path.Combine(Application.persistentDataPath, journalFileName);
        
        if (journalDatabase == null)
        {
            journalDatabase = new JournalDatabase();
        }
        
        // Create user preferences if doesn't exist
        if (!journalDatabase.userPreferences.ContainsKey(currentUserId))
        {
            journalDatabase.userPreferences[currentUserId] = new UserPreferences();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Journal persistence initialized. Save path: {saveFilePath}");
        }
    }
    
    public void SaveJournalEntry(JournalEntry entry)
    {
        if (entry == null)
        {
            Debug.LogError("Cannot save null journal entry");
            return;
        }
        
        // Set user ID if not set
        if (string.IsNullOrEmpty(entry.userId))
        {
            entry.userId = currentUserId;
        }
        
        // Update timestamp
        entry.timestamp = DateTime.Now;
        
        // Find existing entry or add new one
        int existingIndex = journalDatabase.entries.FindIndex(e => e.id == entry.id);
        
        if (existingIndex >= 0)
        {
            // Update existing entry
            journalDatabase.entries[existingIndex] = entry;
            OnEntryUpdated?.Invoke(entry);
            
            if (enableDebugLogs)
                Debug.Log($"Updated journal entry: {entry.id}");
        }
        else
        {
            // Add new entry
            journalDatabase.entries.Add(entry);
            OnEntryAdded?.Invoke(entry);
            
            if (enableDebugLogs)
                Debug.Log($"Added new journal entry: {entry.id}");
        }
        
        // Maintain max entries limit
        CleanupOldEntries();
        
        // Save to file
        SaveToFile();
    }
    
    public void DeleteJournalEntry(string entryId)
    {
        int index = journalDatabase.entries.FindIndex(e => e.id == entryId);
        
        if (index >= 0)
        {
            journalDatabase.entries.RemoveAt(index);
            OnEntryDeleted?.Invoke(entryId);
            SaveToFile();
            
            if (enableDebugLogs)
                Debug.Log($"Deleted journal entry: {entryId}");
        }
    }
    
    public List<JournalEntry> LoadAllJournalEntries()
    {
        return journalDatabase.entries.FindAll(e => e.userId == currentUserId);
    }
    
    public List<JournalEntry> LoadEntriesByTopic(string topic)
    {
        return journalDatabase.entries.FindAll(e => e.userId == currentUserId && e.topic == topic);
    }
    
    public List<JournalEntry> LoadEntriesByCharacter(string characterId)
    {
        return journalDatabase.entries.FindAll(e => e.userId == currentUserId && e.characterId == characterId);
    }
    
    public List<JournalEntry> LoadEntriesByDateRange(DateTime startDate, DateTime endDate)
    {
        return journalDatabase.entries.FindAll(e => 
            e.userId == currentUserId && 
            e.timestamp >= startDate && 
            e.timestamp <= endDate);
    }
    
    public JournalEntry LoadJournalEntry(string entryId)
    {
        return journalDatabase.entries.Find(e => e.id == entryId && e.userId == currentUserId);
    }
    
    private void SaveToFile()
    {
        try
        {
            journalDatabase.lastSaved = DateTime.Now;
            string jsonData = JsonConvert.SerializeObject(journalDatabase, Formatting.Indented);
            
            // Encrypt if enabled
            if (useEncryption)
            {
                jsonData = EncryptData(jsonData);
            }
            
            // Create backup before saving
            CreateBackup();
            
            // Write to file
            File.WriteAllText(saveFilePath, jsonData);
            
            OnDatabaseSaved?.Invoke();
            
            if (enableDebugLogs)
                Debug.Log($"Journal data saved successfully. Entries: {journalDatabase.entries.Count}");
                
            if (showSaveNotifications)
                ShowSaveNotification();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save journal data: {e.Message}");
        }
    }
    
    private void LoadJournalData()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                string jsonData = File.ReadAllText(saveFilePath);
                
                // Decrypt if enabled
                if (useEncryption)
                {
                    jsonData = DecryptData(jsonData);
                }
                
                journalDatabase = JsonConvert.DeserializeObject<JournalDatabase>(jsonData);
                
                if (journalDatabase == null)
                {
                    journalDatabase = new JournalDatabase();
                }
                
                // Ensure user preferences exist
                if (!journalDatabase.userPreferences.ContainsKey(currentUserId))
                {
                    journalDatabase.userPreferences[currentUserId] = new UserPreferences();
                }
                
                OnDatabaseLoaded?.Invoke();
                
                if (enableDebugLogs)
                    Debug.Log($"Journal data loaded successfully. Entries: {journalDatabase.entries.Count}");
            }
            else
            {
                journalDatabase = new JournalDatabase();
                journalDatabase.userPreferences[currentUserId] = new UserPreferences();
                
                if (enableDebugLogs)
                    Debug.Log("No existing journal data found. Created new database.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load journal data: {e.Message}");
            journalDatabase = new JournalDatabase();
            journalDatabase.userPreferences[currentUserId] = new UserPreferences();
        }
    }
    
    private void CreateBackup()
    {
        if (!File.Exists(saveFilePath)) return;
        
        try
        {
            string backupFileName = $"{backupPrefix}{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string backupPath = Path.Combine(Application.persistentDataPath, backupFileName);
            
            File.Copy(saveFilePath, backupPath, true);
            
            // Clean up old backups
            CleanupOldBackups();
            
            if (enableDebugLogs)
                Debug.Log($"Backup created: {backupFileName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create backup: {e.Message}");
        }
    }
    
    private void CleanupOldBackups()
    {
        try
        {
            string[] backupFiles = Directory.GetFiles(Application.persistentDataPath, $"{backupPrefix}*.json");
            
            if (backupFiles.Length > maxBackupFiles)
            {
                Array.Sort(backupFiles);
                
                for (int i = 0; i < backupFiles.Length - maxBackupFiles; i++)
                {
                    File.Delete(backupFiles[i]);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to cleanup old backups: {e.Message}");
        }
    }
    
    private void CleanupOldEntries()
    {
        var userEntries = journalDatabase.entries.FindAll(e => e.userId == currentUserId);
        
        if (userEntries.Count > maxEntriesPerUser)
        {
            // Sort by timestamp (oldest first)
            userEntries.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
            
            // Remove oldest entries
            int entriesToRemove = userEntries.Count - maxEntriesPerUser;
            for (int i = 0; i < entriesToRemove; i++)
            {
                journalDatabase.entries.Remove(userEntries[i]);
            }
            
            if (enableDebugLogs)
                Debug.Log($"Cleaned up {entriesToRemove} old journal entries");
        }
    }
    
    private void StartAutoSave()
    {
        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
        }
        
        autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
    }
    
    private IEnumerator AutoSaveRoutine()
    {
        while (enableAutoSave)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            
            // Only save if there are changes
            if (journalDatabase != null && journalDatabase.entries.Count > 0)
            {
                SaveToFile();
            }
        }
    }
    
    // User Preferences
    public void SaveUserPreferences(UserPreferences preferences)
    {
        if (preferences == null) return;
        
        preferences.userId = currentUserId;
        preferences.lastActiveTime = DateTime.Now;
        
        journalDatabase.userPreferences[currentUserId] = preferences;
        SaveToFile();
    }
    
    public UserPreferences LoadUserPreferences()
    {
        if (journalDatabase.userPreferences.ContainsKey(currentUserId))
        {
            return journalDatabase.userPreferences[currentUserId];
        }
        
        return new UserPreferences();
    }
    
    // Encryption (basic implementation)
    private string EncryptData(string data)
    {
        // Simple XOR encryption (use a proper encryption library in production)
        byte[] dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
        byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(encryptionKey);
        
        for (int i = 0; i < dataBytes.Length; i++)
        {
            dataBytes[i] = (byte)(dataBytes[i] ^ keyBytes[i % keyBytes.Length]);
        }
        
        return System.Convert.ToBase64String(dataBytes);
    }
    
    private string DecryptData(string encryptedData)
    {
        // Simple XOR decryption
        byte[] dataBytes = System.Convert.FromBase64String(encryptedData);
        byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(encryptionKey);
        
        for (int i = 0; i < dataBytes.Length; i++)
        {
            dataBytes[i] = (byte)(dataBytes[i] ^ keyBytes[i % keyBytes.Length]);
        }
        
        return System.Text.Encoding.UTF8.GetString(dataBytes);
    }
    
    private void ShowSaveNotification()
    {
        // You can implement a UI notification here
        Debug.Log("Journal saved successfully!");
    }
    
    // Public utility methods
    public void ExportJournalData(string exportPath)
    {
        try
        {
            string jsonData = JsonConvert.SerializeObject(journalDatabase, Formatting.Indented);
            File.WriteAllText(exportPath, jsonData);
            
            if (enableDebugLogs)
                Debug.Log($"Journal data exported to: {exportPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to export journal data: {e.Message}");
        }
    }
    
    public void ImportJournalData(string importPath)
    {
        try
        {
            if (File.Exists(importPath))
            {
                string jsonData = File.ReadAllText(importPath);
                JournalDatabase importedData = JsonConvert.DeserializeObject<JournalDatabase>(jsonData);
                
                if (importedData != null)
                {
                    // Merge with existing data
                    foreach (var entry in importedData.entries)
                    {
                        if (!journalDatabase.entries.Exists(e => e.id == entry.id))
                        {
                            journalDatabase.entries.Add(entry);
                        }
                    }
                    
                    SaveToFile();
                    
                    if (enableDebugLogs)
                        Debug.Log("Journal data imported successfully");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to import journal data: {e.Message}");
        }
    }
    
    public void ClearAllData()
    {
        journalDatabase = new JournalDatabase();
        journalDatabase.userPreferences[currentUserId] = new UserPreferences();
        SaveToFile();
        
        if (enableDebugLogs)
            Debug.Log("All journal data cleared");
    }
    
    public int GetTotalEntryCount()
    {
        return journalDatabase.entries.FindAll(e => e.userId == currentUserId).Count;
    }
    
    public long GetDatabaseSize()
    {
        if (File.Exists(saveFilePath))
        {
            return new FileInfo(saveFilePath).Length;
        }
        return 0;
    }
    
    private void OnDestroy()
    {
        if (enableAutoSave)
        {
            SaveToFile();
        }
        
        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && enableAutoSave)
        {
            SaveToFile();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && enableAutoSave)
        {
            SaveToFile();
        }
    }
}