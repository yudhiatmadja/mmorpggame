using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[System.Serializable]
public class JournalEntry
{
    public string id;
    public string userId;
    public string userMessage;
    public string aiResponse;
    public string characterId;
    public DateTime timestamp;
    public string topic;
    public Vector3 objectPosition;
    
    public JournalEntry()
    {
        id = Guid.NewGuid().ToString();
        timestamp = DateTime.Now;
    }
}

public class CharacterAIJournalUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject journalPanel;
    public GameObject chatPanel;
    public GameObject characterSelectionPanel;
    public ScrollRect journalScrollRect;
    public ScrollRect chatScrollRect;
    public Transform journalContent;
    public Transform chatContent;
    
    [Header("Input Elements")]
    public TMP_InputField chatInput;
    public TMP_Dropdown characterDropdown;
    public Button sendButton;
    public Button clearChatButton;
    public Button newJournalButton;
    
    [Header("Prefabs")]
    public GameObject journalEntryPrefab;
    public GameObject chatMessagePrefab;
    public GameObject aiResponsePrefab;
    
    [Header("UI States")]
    public GameObject loadingIndicator;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI currentCharacterText;
    
    // References
    private CharacterAIManager aiManager;
    private JournalPersistence journalPersistence;
    private List<JournalEntry> currentJournalEntries = new List<JournalEntry>();
    private JournalEntry currentEntry;
    
    private void Start()
    {
        InitializeComponents();
        SetupEventListeners();
        SetupUI();
    }
    
    private void InitializeComponents()
    {
        aiManager = FindObjectOfType<CharacterAIManager>();
        journalPersistence = GetComponent<JournalPersistence>();
        
        if (aiManager == null)
        {
            Debug.LogError("CharacterAIManager not found!");
            return;
        }
        
        if (journalPersistence == null)
        {
            journalPersistence = gameObject.AddComponent<JournalPersistence>();
        }
    }
    
    private void SetupEventListeners()
    {
        // AI Manager events
        aiManager.OnResponseReceived += HandleAIResponse;
        aiManager.OnErrorOccurred += HandleAIError;
        aiManager.OnRequestStarted += ShowLoadingState;
        aiManager.OnRequestCompleted += HideLoadingState;
        
        // UI events
        sendButton.onClick.AddListener(SendMessage);
        clearChatButton.onClick.AddListener(ClearCurrentChat);
        newJournalButton.onClick.AddListener(CreateNewJournal);
        chatInput.onSubmit.AddListener(OnChatInputSubmit);
        characterDropdown.onValueChanged.AddListener(OnCharacterChanged);
    }
    
    private void SetupUI()
    {
        PopulateCharacterDropdown();
        LoadJournalEntries();
        
        // Set default states
        journalPanel.SetActive(true);
        chatPanel.SetActive(false);
        characterSelectionPanel.SetActive(false);
        loadingIndicator.SetActive(false);
        
        statusText.text = "Ready to chat!";
    }
    
    private void PopulateCharacterDropdown()
    {
        characterDropdown.ClearOptions();
        List<string> characterNames = new List<string>();
        
        foreach (var character in aiManager.availableCharacters)
        {
            characterNames.Add(character.name);
        }
        
        characterDropdown.AddOptions(characterNames);
        
        // Set default character
        if (aiManager.availableCharacters.Count > 0)
        {
            OnCharacterChanged(0);
        }
    }
    
    private void OnCharacterChanged(int index)
    {
        if (index < aiManager.availableCharacters.Count)
        {
            var character = aiManager.availableCharacters[index];
            aiManager.SetCharacter(character.character_id);
            currentCharacterText.text = $"Talking with: {character.name}";
            
            // Show character greeting
            if (!string.IsNullOrEmpty(character.greeting))
            {
                CreateChatMessage(character.greeting, false);
            }
        }
    }
    
    private void OnChatInputSubmit(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            SendMessage();
        }
    }
    
    public void SendMessage()
    {
        string message = chatInput.text.Trim();
        
        if (string.IsNullOrEmpty(message))
        {
            statusText.text = "Please enter a message";
            return;
        }
        
        // Content safety check
        if (!aiManager.IsMessageSafe(message))
        {
            statusText.text = "Message contains inappropriate content";
            return;
        }
        
        // Create user message in chat
        CreateChatMessage(message, true);
        
        // Clear input
        chatInput.text = "";
        
        // Create or update journal entry
        if (currentEntry == null)
        {
            CreateNewJournal();
        }
        
        currentEntry.userMessage = message;
        currentEntry.characterId = aiManager.currentCharacterId;
        
        // Send to AI
        aiManager.SendMessage(message);
    }
    
    private void HandleAIResponse(string response)
    {
        // Create AI response in chat
        CreateChatMessage(response, false);
        
        // Update journal entry
        if (currentEntry != null)
        {
            currentEntry.aiResponse = response;
            currentEntry.timestamp = DateTime.Now;
            
            // Save to journal
            SaveCurrentJournalEntry();
        }
        
        statusText.text = "Response received";
    }
    
    private void HandleAIError(string error)
    {
        statusText.text = $"Error: {error}";
        CreateChatMessage($"Error: {error}", false);
    }
    
    private void ShowLoadingState()
    {
        loadingIndicator.SetActive(true);
        sendButton.interactable = false;
        statusText.text = "Thinking...";
    }
    
    private void HideLoadingState()
    {
        loadingIndicator.SetActive(false);
        sendButton.interactable = true;
    }
    
    private void CreateChatMessage(string message, bool isUser)
    {
        GameObject messagePrefab = isUser ? chatMessagePrefab : aiResponsePrefab;
        GameObject messageObj = Instantiate(messagePrefab, chatContent);
        
        TextMeshProUGUI messageText = messageObj.GetComponentInChildren<TextMeshProUGUI>();
        messageText.text = message;
        
        // Auto-scroll to bottom
        StartCoroutine(ScrollToBottom(chatScrollRect));
    }
    
    private IEnumerator ScrollToBottom(ScrollRect scrollRect)
    {
        yield return new WaitForEndOfFrame();
        scrollRect.normalizedPosition = new Vector2(0, 0);
    }
    
    public void CreateNewJournal()
    {
        currentEntry = new JournalEntry();
        currentEntry.userId = SystemInfo.deviceUniqueIdentifier;
        
        // Get current character info
        if (!string.IsNullOrEmpty(aiManager.currentCharacterId))
        {
            var character = aiManager.GetCharacterInfo(aiManager.currentCharacterId);
            if (character != null)
            {
                currentEntry.topic = character.topic;
            }
        }
        
        statusText.text = "New journal entry created";
        
        // Switch to chat panel
        ShowChatPanel();
    }
    
    private void SaveCurrentJournalEntry()
    {
        if (currentEntry != null)
        {
            // Add to current entries if not already there
            if (!currentJournalEntries.Contains(currentEntry))
            {
                currentJournalEntries.Add(currentEntry);
            }
            
            // Save to persistent storage
            journalPersistence.SaveJournalEntry(currentEntry);
            
            // Update journal UI
            UpdateJournalUI();
        }
    }
    
    private void LoadJournalEntries()
    {
        currentJournalEntries = journalPersistence.LoadAllJournalEntries();
        UpdateJournalUI();
    }
    
    private void UpdateJournalUI()
    {
        // Clear existing entries
        foreach (Transform child in journalContent)
        {
            Destroy(child.gameObject);
        }
        
        // Create UI for each entry
        foreach (var entry in currentJournalEntries)
        {
            CreateJournalEntryUI(entry);
        }
    }
    
    private void CreateJournalEntryUI(JournalEntry entry)
    {
        GameObject entryObj = Instantiate(journalEntryPrefab, journalContent);
        JournalEntryUI entryUI = entryObj.GetComponent<JournalEntryUI>();
        
        if (entryUI != null)
        {
            entryUI.SetupEntry(entry);
            entryUI.OnEntrySelected += LoadJournalEntry;
        }
    }
    
    private void LoadJournalEntry(JournalEntry entry)
    {
        currentEntry = entry;
        
        // Clear current chat
        foreach (Transform child in chatContent)
        {
            Destroy(child.gameObject);
        }
        
        // Load conversation
        if (!string.IsNullOrEmpty(entry.userMessage))
        {
            CreateChatMessage(entry.userMessage, true);
        }
        
        if (!string.IsNullOrEmpty(entry.aiResponse))
        {
            CreateChatMessage(entry.aiResponse, false);
        }
        
        // Set character
        if (!string.IsNullOrEmpty(entry.characterId))
        {
            aiManager.SetCharacter(entry.characterId);
            
            // Update dropdown
            var character = aiManager.GetCharacterInfo(entry.characterId);
            if (character != null)
            {
                int index = aiManager.availableCharacters.IndexOf(character);
                if (index >= 0)
                {
                    characterDropdown.value = index;
                    currentCharacterText.text = $"Talking with: {character.name}";
                }
            }
        }
        
        ShowChatPanel();
    }
    
    public void ClearCurrentChat()
    {
        // Clear chat UI
        foreach (Transform child in chatContent)
        {
            Destroy(child.gameObject);
        }
        
        // Clear AI conversation
        aiManager.ClearConversation();
        
        // Reset current entry
        currentEntry = null;
        
        statusText.text = "Chat cleared";
    }
    
    public void ShowJournalPanel()
    {
        journalPanel.SetActive(true);
        chatPanel.SetActive(false);
        characterSelectionPanel.SetActive(false);
    }
    
    public void ShowChatPanel()
    {
        journalPanel.SetActive(false);
        chatPanel.SetActive(true);
        characterSelectionPanel.SetActive(false);
    }
    
    public void ShowCharacterSelectionPanel()
    {
        journalPanel.SetActive(false);
        chatPanel.SetActive(false);
        characterSelectionPanel.SetActive(true);
    }
    
    private void OnDestroy()
    {
        // Cleanup event listeners
        if (aiManager != null)
        {
            aiManager.OnResponseReceived -= HandleAIResponse;
            aiManager.OnErrorOccurred -= HandleAIError;
            aiManager.OnRequestStarted -= ShowLoadingState;
            aiManager.OnRequestCompleted -= HideLoadingState;
        }
    }
}