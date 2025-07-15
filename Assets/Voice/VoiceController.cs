using UnityEngine;
using UnityEngine.UI;
using Photon.Voice.Unity;
using Photon.Voice.PUN;

public class VoiceChatController : MonoBehaviour
{
    [Header("Voice Components")]
    public Recorder voiceRecorder;
    public VoiceConnection voiceConnection;
    
    [Header("UI Elements")]
    public Text statusText;
    public GameObject statusPanel;
    
    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.T;
    public float statusDisplayTime = 2f;
    public bool enableDebugLogging = true;
    
    private bool isVoiceEnabled = true;
    private float statusTimer = 0f;
    private bool showingStatus = false;
    
    void Start()
    {
        LogMessage("VoiceChatController Starting...", LogType.Log);
        
        // Auto-find components if not assigned
        FindComponents();
        
        // Initialize voice state
        InitializeVoiceState();
        
        // Setup UI
        SetupUI();
        
        LogMessage("VoiceChatController initialization completed", LogType.Log);
    }
    
    private void FindComponents()
    {
        LogMessage("Finding voice components...", LogType.Log);
        
        if (voiceRecorder == null)
        {
            voiceRecorder = GetComponent<Recorder>();
            if (voiceRecorder == null)
            {
                voiceRecorder = FindObjectOfType<Recorder>();
            }
        }
        
        if (voiceConnection == null)
        {
            voiceConnection = FindObjectOfType<VoiceConnection>();
        }
        
        LogMessage($"Voice Recorder: {(voiceRecorder != null ? "Found" : "Not Found")}", 
                  voiceRecorder != null ? LogType.Log : LogType.Warning);
        LogMessage($"Voice Connection: {(voiceConnection != null ? "Found" : "Not Found")}", 
                  voiceConnection != null ? LogType.Log : LogType.Warning);
    }
    
    private void InitializeVoiceState()
    {
        LogMessage("Initializing voice state...", LogType.Log);
        
        if (voiceRecorder != null)
        {
            isVoiceEnabled = voiceRecorder.TransmitEnabled;
            LogMessage($"Initial voice state: {(isVoiceEnabled ? "Enabled" : "Disabled")}", LogType.Log);
        }
        else
        {
            LogMessage("Cannot initialize voice state - Recorder not found", LogType.Error);
        }
    }
    
    private void SetupUI()
    {
        LogMessage("Setting up UI...", LogType.Log);
        
        if (statusPanel != null)
        {
            statusPanel.SetActive(false);
            LogMessage("Status panel initialized", LogType.Log);
        }
        
        UpdateStatusText();
    }
    
    void Update()
    {
        // Check for toggle input
        if (Input.GetKeyDown(toggleKey))
        {
            LogMessage($"Toggle key ({toggleKey}) pressed", LogType.Log);
            ToggleVoiceChat();
        }
        
        // Handle status display timer
        if (showingStatus)
        {
            statusTimer -= Time.deltaTime;
            if (statusTimer <= 0f)
            {
                HideStatus();
            }
        }
    }
    
    public void ToggleVoiceChat()
    {
        LogMessage("Toggling voice chat...", LogType.Log);
        
        if (voiceRecorder == null)
        {
            LogMessage("Cannot toggle voice - Recorder not found", LogType.Error);
            return;
        }
        
        // Toggle voice state
        isVoiceEnabled = !isVoiceEnabled;
        voiceRecorder.TransmitEnabled = isVoiceEnabled;
        
        LogMessage($"Voice chat {(isVoiceEnabled ? "enabled" : "disabled")}", LogType.Log);
        
        // Update UI and show status
        UpdateStatusText();
        ShowStatus();
        
        // Optional: Play sound feedback
        PlayToggleSound();
    }
    
    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            string status = isVoiceEnabled ? "ON" : "OFF";
            string color = isVoiceEnabled ? "#00FF00" : "#FF0000";
            statusText.text = $"<color={color}>Voice Chat: {status}</color>";
            
            LogMessage($"Status text updated: {status}", LogType.Log);
        }
    }
    
    private void ShowStatus()
    {
        if (statusPanel != null)
        {
            statusPanel.SetActive(true);
            showingStatus = true;
            statusTimer = statusDisplayTime;
            
            LogMessage("Status panel shown", LogType.Log);
        }
    }
    
    private void HideStatus()
    {
        if (statusPanel != null)
        {
            statusPanel.SetActive(false);
            showingStatus = false;
            
            LogMessage("Status panel hidden", LogType.Log);
        }
    }
    
    private void PlayToggleSound()
    {
        LogMessage("Playing toggle sound...", LogType.Log);
        
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
            LogMessage("Toggle sound played", LogType.Log);
        }
        else
        {
            LogMessage("No audio source or clip found for toggle sound", LogType.Warning);
        }
    }
    
    // Public methods for external control
    public void EnableVoiceChat()
    {
        LogMessage("Enabling voice chat externally...", LogType.Log);
        
        if (voiceRecorder != null)
        {
            isVoiceEnabled = true;
            voiceRecorder.TransmitEnabled = true;
            UpdateStatusText();
            ShowStatus();
            
            LogMessage("Voice chat enabled externally", LogType.Log);
        }
        else
        {
            LogMessage("Cannot enable voice chat - Recorder not found", LogType.Error);
        }
    }
    
    public void DisableVoiceChat()
    {
        LogMessage("Disabling voice chat externally...", LogType.Log);
        
        if (voiceRecorder != null)
        {
            isVoiceEnabled = false;
            voiceRecorder.TransmitEnabled = false;
            UpdateStatusText();
            ShowStatus();
            
            LogMessage("Voice chat disabled externally", LogType.Log);
        }
        else
        {
            LogMessage("Cannot disable voice chat - Recorder not found", LogType.Error);
        }
    }
    
    public bool IsVoiceEnabled()
    {
        LogMessage($"Voice enabled state requested: {isVoiceEnabled}", LogType.Log);
        return isVoiceEnabled;
    }
    
    // Logging utility
    private void LogMessage(string message, LogType logType = LogType.Log)
    {
        if (!enableDebugLogging) return;
        
        string formattedMessage = $"[VoiceChatController] {message}";
        
        switch (logType)
        {
            case LogType.Log:
                Debug.Log(formattedMessage);
                break;
            case LogType.Warning:
                Debug.LogWarning(formattedMessage);
                break;
            case LogType.Error:
                Debug.LogError(formattedMessage);
                break;
        }
    }
    
    // Handle voice connection events
    void OnEnable()
    {
        LogMessage("VoiceChatController enabled", LogType.Log);
        
        // Subscribe to voice events if needed
        if (voiceConnection != null)
        {
            LogMessage("Voice connection available", LogType.Log);
        }
    }
    
    void OnDisable()
    {
        LogMessage("VoiceChatController disabled", LogType.Log);
        
        // Unsubscribe from voice events
        if (voiceConnection != null)
        {
            LogMessage("Cleaning up voice connection", LogType.Log);
        }
    }
    
    void OnDestroy()
    {
        LogMessage("VoiceChatController destroyed", LogType.Log);
    }
    
    // Debug methods
    public void PrintRecorderStatus()
    {
        if (voiceRecorder != null)
        {
            LogMessage($"Voice Recorder Status - Transmit Enabled: {voiceRecorder.TransmitEnabled}, " +
                      $"Is Recording: {voiceRecorder.IsCurrentlyTransmitting}, " +
                      $"Voice Detection: {voiceRecorder.VoiceDetection}", 
                      LogType.Log);
        }
        else
        {
            LogMessage("Voice Recorder is null", LogType.Error);
        }
    }
}