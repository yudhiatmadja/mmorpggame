using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JournalEntryUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI previewText;
    public TextMeshProUGUI characterText;
    public Button entryButton;
    public Button deleteButton;
    public Image characterIcon;
    public Image backgroundImage;
    
    [Header("Visual Settings")]
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;
    public int previewTextLength = 50;
    
    // Events
    public System.Action<JournalEntry> OnEntrySelected;
    public System.Action<JournalEntry> OnEntryDeleted;
    
    private JournalEntry journalEntry;
    private bool isSelected = false;
    
    private void Start()
    {
        SetupEventListeners();
    }
    
    // Helper methods to handle different icon property names
    private bool HasProperty(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName) != null;
    }
    
    private Sprite GetIconSprite(object character)
    {
        var property = character.GetType().GetProperty("icon");
        return property?.GetValue(character) as Sprite;
    }
    
    private Sprite GetAvatarSprite(object character)
    {
        var property = character.GetType().GetProperty("avatar");
        return property?.GetValue(character) as Sprite;
    }
    
    private Sprite GetPortraitSprite(object character)
    {
        var property = character.GetType().GetProperty("portrait");
        return property?.GetValue(character) as Sprite;
    }
    
    private void SetupEventListeners()
    {
        if (entryButton != null)
        {
            entryButton.onClick.AddListener(SelectEntry);
        }
        
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(DeleteEntry);
        }
    }
    
    public void SetupEntry(JournalEntry entry)
    {
        journalEntry = entry;
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (journalEntry == null) return;
        
        // Set title (use topic or generate from user message)
        if (titleText != null)
        {
            string title = !string.IsNullOrEmpty(journalEntry.topic) ? journalEntry.topic : "Journal Entry";
            if (string.IsNullOrEmpty(journalEntry.topic) && !string.IsNullOrEmpty(journalEntry.userMessage))
            {
                title = journalEntry.userMessage.Length > 20 ? 
                    journalEntry.userMessage.Substring(0, 20) + "..." : 
                    journalEntry.userMessage;
            }
            titleText.text = title;
        }
        
        // Set date
        if (dateText != null)
        {
            dateText.text = journalEntry.timestamp.ToString("MMM dd, yyyy HH:mm");
        }
        
        // Set preview text
        if (previewText != null)
        {
            string preview = "";
            if (!string.IsNullOrEmpty(journalEntry.userMessage))
            {
                preview = journalEntry.userMessage.Length > previewTextLength ? 
                    journalEntry.userMessage.Substring(0, previewTextLength) + "..." : 
                    journalEntry.userMessage;
            }
            previewText.text = preview;
        }
        
        // Set character info
        if (characterText != null)
        {
            var aiManager = FindObjectOfType<CharacterAIManager>();
            if (aiManager != null && !string.IsNullOrEmpty(journalEntry.characterId))
            {
                var character = aiManager.GetCharacterInfo(journalEntry.characterId);
                if (character != null)
                {
                    characterText.text = $"With: {character.name}";
                    
                    // Hide character icon for now since we don't know the exact property name
                    if (characterIcon != null)
                    {
                        characterIcon.gameObject.SetActive(false);
                    }
                }
                else
                {
                    characterText.text = "Unknown Character";
                }
            }
            else
            {
                characterText.text = "No Character";
            }
        }
        
        // Set background color
        SetSelectionState(false);
    }
    
    private void SelectEntry()
    {
        if (journalEntry != null)
        {
            // Deselect all other entries
            var allEntries = FindObjectsOfType<JournalEntryUI>();
            foreach (var entry in allEntries)
            {
                if (entry != this)
                {
                    entry.SetSelectionState(false);
                }
            }
            
            // Select this entry
            SetSelectionState(true);
            
            // Trigger event
            OnEntrySelected?.Invoke(journalEntry);
        }
    }
    
    private void DeleteEntry()
    {
        if (journalEntry != null)
        {
            // Show confirmation dialog (you can implement this)
            if (ShowDeleteConfirmation())
            {
                OnEntryDeleted?.Invoke(journalEntry);
                Destroy(gameObject);
            }
        }
    }
    
    private bool ShowDeleteConfirmation()
    {
        // Simple confirmation for now - you can implement a proper dialog
        return true;
    }
    
    public void SetSelectionState(bool selected)
    {
        isSelected = selected;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedColor : normalColor;
        }
        
        // You can add more visual feedback here
        if (entryButton != null)
        {
            var colors = entryButton.colors;
            colors.normalColor = selected ? selectedColor : normalColor;
            entryButton.colors = colors;
        }
    }
    
    public JournalEntry GetJournalEntry()
    {
        return journalEntry;
    }
    
    public bool IsSelected()
    {
        return isSelected;
    }
    
    // Helper method to format date in different ways
    public void SetDateFormat(string format)
    {
        if (dateText != null && journalEntry != null)
        {
            dateText.text = journalEntry.timestamp.ToString(format);
        }
    }
    
    // Helper method to set custom preview length
    public void SetPreviewLength(int length)
    {
        previewTextLength = length;
        if (journalEntry != null)
        {
            UpdateUI();
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup event listeners
        if (entryButton != null)
        {
            entryButton.onClick.RemoveAllListeners();
        }
        
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
        }
    }
}