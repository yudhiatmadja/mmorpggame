using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;

[System.Serializable]
public class CharacterAIResponse
{
    public string text;
    public bool is_final_chunk;
    public string last_user_msg_id;
    public string src_char;
}

[System.Serializable]
public class CharacterAIRequest
{
    public string message;
    public string character_id;
    public string conversation_id;
    public bool stream = false;
}

[System.Serializable]
public class CharacterInfo
{
    public string character_id;
    public string name;
    public string description;
    public string greeting;
    public string topic;
}

public class CharacterAIManager : MonoBehaviour
{
    [Header("API Configuration")]
    public string apiToken = "YOUR_CHARACTER_AI_TOKEN";
    public string baseUrl = "https://beta.character.ai/chat";
    
    [Header("Character Settings")]
    public List<CharacterInfo> availableCharacters = new List<CharacterInfo>();
    public string currentCharacterId;
    public string currentConversationId;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Events
    public event Action<string> OnResponseReceived;
    public event Action<string> OnErrorOccurred;
    public event Action OnRequestStarted;
    public event Action OnRequestCompleted;
    
    private Dictionary<string, string> conversationHistory = new Dictionary<string, string>();
    private bool isProcessingRequest = false;
    
    private void Start()
    {
        InitializeDefaultCharacters();
    }
    
    private void InitializeDefaultCharacters()
    {
        // Add some default safe characters for roleplay
        availableCharacters.Add(new CharacterInfo
        {
            character_id = "educational_tutor",
            name = "Educational Tutor",
            description = "A friendly tutor who helps with learning",
            greeting = "Hello! I'm here to help you learn. What would you like to explore today?",
            topic = "Education"
        });
        
        availableCharacters.Add(new CharacterInfo
        {
            character_id = "story_companion",
            name = "Story Companion",
            description = "A creative companion for storytelling adventures",
            greeting = "Welcome, adventurer! What story shall we create together?",
            topic = "Creative Writing"
        });
        
        availableCharacters.Add(new CharacterInfo
        {
            character_id = "science_explorer",
            name = "Science Explorer",
            description = "A curious scientist who loves to explore and explain",
            greeting = "Greetings! Ready to explore the wonders of science?",
            topic = "Science"
        });
    }
    
    public void SetCharacter(string characterId)
    {
        currentCharacterId = characterId;
        currentConversationId = GenerateConversationId();
        
        if (enableDebugLogs)
            Debug.Log($"Character set to: {characterId}");
    }
    
    public void SendMessage(string message)
    {
        if (isProcessingRequest)
        {
            Debug.LogWarning("Already processing a request. Please wait.");
            return;
        }
        
        if (string.IsNullOrEmpty(currentCharacterId))
        {
            OnErrorOccurred?.Invoke("No character selected");
            return;
        }
        
        StartCoroutine(SendMessageCoroutine(message));
    }
    
    private IEnumerator SendMessageCoroutine(string message)
    {
        isProcessingRequest = true;
        OnRequestStarted?.Invoke();
        
        // Create request data
        CharacterAIRequest request = new CharacterAIRequest
        {
            message = message,
            character_id = currentCharacterId,
            conversation_id = currentConversationId,
            stream = false
        };
        
        string jsonData = JsonConvert.SerializeObject(request);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        
        // Create Unity Web Request
        UnityWebRequest webRequest = new UnityWebRequest($"{baseUrl}/streaming/", "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        
        // Set headers
        webRequest.SetRequestHeader("Content-Type", "application/json");
        webRequest.SetRequestHeader("Authorization", $"Token {apiToken}");
        
        if (enableDebugLogs)
            Debug.Log($"Sending message: {message}");
        
        // Send request
        yield return webRequest.SendWebRequest();
        
        // Handle response
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            try
            {
                string responseText = webRequest.downloadHandler.text;
                CharacterAIResponse response = JsonConvert.DeserializeObject<CharacterAIResponse>(responseText);
                
                if (response != null && !string.IsNullOrEmpty(response.text))
                {
                    // Store in conversation history
                    conversationHistory[message] = response.text;
                    
                    if (enableDebugLogs)
                        Debug.Log($"Response received: {response.text}");
                    
                    OnResponseReceived?.Invoke(response.text);
                }
                else
                {
                    OnErrorOccurred?.Invoke("Empty response received");
                }
            }
            catch (Exception e)
            {
                OnErrorOccurred?.Invoke($"Error parsing response: {e.Message}");
            }
        }
        else
        {
            OnErrorOccurred?.Invoke($"Request failed: {webRequest.error}");
        }
        
        isProcessingRequest = false;
        OnRequestCompleted?.Invoke();
        webRequest.Dispose();
    }
    
    public void ClearConversation()
    {
        currentConversationId = GenerateConversationId();
        conversationHistory.Clear();
        
        if (enableDebugLogs)
            Debug.Log("Conversation cleared");
    }
    
    private string GenerateConversationId()
    {
        return Guid.NewGuid().ToString();
    }
    
    public CharacterInfo GetCharacterInfo(string characterId)
    {
        return availableCharacters.Find(c => c.character_id == characterId);
    }
    
    public List<CharacterInfo> GetCharactersByTopic(string topic)
    {
        return availableCharacters.FindAll(c => c.topic == topic);
    }
    
    public void AddCustomCharacter(CharacterInfo character)
    {
        availableCharacters.Add(character);
    }
    
    // Content Safety Filter
    public bool IsMessageSafe(string message)
    {
        // Basic content filtering
        string[] bannedWords = { "inappropriate", "adult", "violence" }; // Add your banned words
        string lowerMessage = message.ToLower();
        
        foreach (string word in bannedWords)
        {
            if (lowerMessage.Contains(word))
            {
                return false;
            }
        }
        
        return true;
    }
    
    public string SanitizeMessage(string message)
    {
        // Remove or replace inappropriate content
        // This is a basic implementation - expand as needed
        return message.Trim();
    }
}