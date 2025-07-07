using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractableJournalObject : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public KeyCode interactionKey = KeyCode.E;
    public LayerMask playerLayer = 1;
    
    [Header("Visual Feedback")]
    public GameObject interactionPrompt;
    public TextMeshProUGUI promptText;
    public GameObject highlightEffect;
    public Material highlightMaterial;
    public Material originalMaterial;
    
    [Header("Journal Configuration")]
    public string objectId;
    public string assignedCharacterId;
    public string objectTopic = "General";
    public bool allowCharacterChange = true;
    
    [Header("Audio")]
    public AudioSource interactionSound;
    public AudioClip interactClip;
    
    // References
    private CharacterAIJournalUI journalUI;
    private CharacterAIManager aiManager;
    private Camera playerCamera;
    private Renderer objectRenderer;
    private bool isPlayerNearby = false;
    private bool isInteracting = false;
    
    // TTS Integration
    private CharacterAITTS ttsManager;
    
    private void Start()
    {
        InitializeObject();
        SetupVisuals();
        FindReferences();
    }
    
    private void InitializeObject()
    {
        // Generate unique ID if not set
        if (string.IsNullOrEmpty(objectId))
        {
            objectId = System.Guid.NewGuid().ToString();
        }
        
        // Setup interaction prompt
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
            if (promptText != null)
            {
                promptText.text = $"Press {interactionKey} to open journal";
            }
        }
        
        // Setup highlight effect
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }
    }
    
    private void SetupVisuals()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null && originalMaterial == null)
        {
            originalMaterial = objectRenderer.material;
        }
    }
    
    private void FindReferences()
    {
        journalUI = FindObjectOfType<CharacterAIJournalUI>();
        aiManager = FindObjectOfType<CharacterAIManager>();
        playerCamera = Camera.main;
        ttsManager = FindObjectOfType<CharacterAITTS>();
        
        if (journalUI == null)
        {
            Debug.LogError("CharacterAIJournalUI not found in scene!");
        }
        
        if (aiManager == null)
        {
            Debug.LogError("CharacterAIManager not found in scene!");
        }
    }
    
    private void Update()
    {
        CheckPlayerDistance();
        HandleInteraction();
    }
    
    private void CheckPlayerDistance()
    {
        if (playerCamera == null) return;
        
        float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
        bool wasNearby = isPlayerNearby;
        isPlayerNearby = distance <= interactionRange;
        
        // State changed
        if (wasNearby != isPlayerNearby)
        {
            if (isPlayerNearby)
            {
                OnPlayerEnterRange();
            }
            else
            {
                OnPlayerExitRange();
            }
        }
    }
    
    private void OnPlayerEnterRange()
    {
        ShowInteractionPrompt();
        ShowHighlight();
        
        // Play interaction sound
        if (interactionSound != null && interactClip != null)
        {
            interactionSound.PlayOneShot(interactClip);
        }
    }
    
    private void OnPlayerExitRange()
    {
        HideInteractionPrompt();
        HideHighlight();
    }
    
    private void HandleInteraction()
    {
        if (isPlayerNearby && Input.GetKeyDown(interactionKey) && !isInteracting)
        {
            StartInteraction();
        }
    }
    
    private void StartInteraction()
    {
        isInteracting = true;
        
        // Set up AI character for this object
        if (!string.IsNullOrEmpty(assignedCharacterId))
        {
            aiManager.SetCharacter(assignedCharacterId);
        }
        
        // Create new journal entry for this object
        journalUI.CreateNewJournal();
        
        // Store object position in journal entry
        Vector3 objectPosition = transform.position;
        
        // Show journal UI
        journalUI.ShowChatPanel();
        
        Debug.Log($"Interacting with journal object: {objectId}");
    }
    
    public void EndInteraction()
    {
        isInteracting = false;
        journalUI.ShowJournalPanel();
    }
    
    private void ShowInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
            
            // Position prompt to face camera
            if (playerCamera != null)
            {
                interactionPrompt.transform.LookAt(playerCamera.transform);
                interactionPrompt.transform.Rotate(0, 180, 0);
            }
        }
    }
    
    private void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    private void ShowHighlight()
    {
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(true);
        }
        
        // Change material if highlight material is assigned
        if (objectRenderer != null && highlightMaterial != null)
        {
            objectRenderer.material = highlightMaterial;
        }
    }
    
    private void HideHighlight()
    {
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }
        
        // Restore original material
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }
    }
    
    // Configuration methods
    public void SetCharacter(string characterId)
    {
        assignedCharacterId = characterId;
    }
    
    public void SetTopic(string topic)
    {
        objectTopic = topic;
    }
    
    public void SetInteractionRange(float range)
    {
        interactionRange = range;
    }
    
    // TTS Integration
    public void SpeakLastResponse()
    {
        if (ttsManager != null)
        {
            ttsManager.SpeakLastResponse();
        }
    }
    
    // Gizmos for editor
    private void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Draw object info
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2, Vector3.one * 0.5f);
        
        #if UNITY_EDITOR
        // Show object ID in scene view
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, 
            $"ID: {objectId}\nTopic: {objectTopic}\nCharacter: {assignedCharacterId}");
        #endif
    }
    
    // Validation
    private void OnValidate()
    {
        if (interactionRange <= 0)
            interactionRange = 3f;
    }
}

// Extension class for easy object duplication and management
[System.Serializable]
public class JournalObjectManager : MonoBehaviour
{
    [Header("Object Management")]
    public InteractableJournalObject journalObjectPrefab;
    public List<InteractableJournalObject> spawnedObjects = new List<InteractableJournalObject>();
    
    [Header("Spawn Configuration")]
    public bool autoGenerateIds = true;
    public string baseObjectName = "JournalObject";
    
    public InteractableJournalObject SpawnJournalObject(Vector3 position, string characterId = "", string topic = "General")
    {
        if (journalObjectPrefab == null)
        {
            Debug.LogError("Journal object prefab not assigned!");
            return null;
        }
        
        GameObject newObject = Instantiate(journalObjectPrefab.gameObject, position, Quaternion.identity);
        InteractableJournalObject journalObj = newObject.GetComponent<InteractableJournalObject>();
        
        if (journalObj != null)
        {
            // Configure the object
            if (autoGenerateIds)
            {
                journalObj.objectId = System.Guid.NewGuid().ToString();
            }
            
            journalObj.assignedCharacterId = characterId;
            journalObj.objectTopic = topic;
            journalObj.name = $"{baseObjectName}_{spawnedObjects.Count + 1}";
            
            spawnedObjects.Add(journalObj);
            
            Debug.Log($"Spawned journal object at {position} with character: {characterId}");
        }
        
        return journalObj;
    }
    
    public void DuplicateObject(InteractableJournalObject original, Vector3 newPosition)
    {
        InteractableJournalObject duplicate = SpawnJournalObject(newPosition, 
            original.assignedCharacterId, original.objectTopic);
        
        if (duplicate != null)
        {
            // Copy settings from original
            duplicate.interactionRange = original.interactionRange;
            duplicate.interactionKey = original.interactionKey;
            duplicate.allowCharacterChange = original.allowCharacterChange;
        }
    }
    
    public void RemoveObject(InteractableJournalObject obj)
    {
        if (spawnedObjects.Contains(obj))
        {
            spawnedObjects.Remove(obj);
            DestroyImmediate(obj.gameObject);
        }
    }
    
    public void ClearAllObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj.gameObject);
            }
        }
        spawnedObjects.Clear();
    }
    
    public List<InteractableJournalObject> GetObjectsByTopic(string topic)
    {
        return spawnedObjects.FindAll(obj => obj.objectTopic == topic);
    }
    
}