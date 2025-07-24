using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CharacterAITTS : MonoBehaviour
{
    [Header("TTS Configuration")]
    public bool enableTTS = true;
    public float speechRate = 1.0f;
    public float volume = 1.0f;
    public int voiceIndex = 0;
    
    [Header("UI Controls")]
    public Button speakButton;
    public Button pauseButton;
    public Button stopButton;
    public Slider volumeSlider;
    public Slider speedSlider;
    public TMP_Dropdown voiceDropdown;
    public Toggle autoSpeakToggle;
    
    [Header("Visual Feedback")]
    public GameObject speakingIndicator;
    public TextMeshProUGUI statusText;
    public Image speakingIcon;
    
    // Speech state
    private bool isSpeaking = false;
    private bool isPaused = false;
    private Queue<string> speechQueue = new Queue<string>();
    private string currentSpeechText = "";
    private string lastAIResponse = "";
    
    // References
    private CharacterAIManager aiManager;
    private CharacterAIJournalUI journalUI;
    
    // Events
    public event Action OnSpeechStarted;
    public event Action OnSpeechCompleted;
    public event Action OnSpeechPaused;
    public event Action OnSpeechResumed;
    
    private void Start()
    {
        InitializeTTS();
        SetupUIControls();
        FindReferences();
    }
    
    private void InitializeTTS()
    {
        // Initialize TTS system
        if (enableTTS)
        {
            StartCoroutine(InitializeSpeechSystem());
        }
        
        // Setup visual feedback
        if (speakingIndicator != null)
        {
            speakingIndicator.SetActive(false);
        }
        
        UpdateStatusText("TTS Ready");
    }
    
    private IEnumerator InitializeSpeechSystem()
    {
        // Wait for system to be ready
        yield return new WaitForSeconds(0.5f);
        
        // Get available voices (platform dependent)
        PopulateVoiceDropdown();
        
        UpdateStatusText("TTS Initialized");
    }
    
    private void PopulateVoiceDropdown()
    {
        if (voiceDropdown != null)
        {
            voiceDropdown.ClearOptions();
            
            // Add default voices (this would be platform-specific)
            List<string> voiceOptions = new List<string>
            {
                "Default Voice",
                "Female Voice 1",
                "Female Voice 2",
                "Male Voice 1",
                "Male Voice 2"
            };
            
            voiceDropdown.AddOptions(voiceOptions);
            voiceDropdown.value = voiceIndex;
        }
    }
    
    private void SetupUIControls()
    {
        // Setup button listeners
        if (speakButton != null)
            speakButton.onClick.AddListener(SpeakLastResponse);
        
        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);
        
        if (stopButton != null)
            stopButton.onClick.AddListener(StopSpeaking);
        
        // Setup sliders
        if (volumeSlider != null)
        {
            volumeSlider.value = volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        
        if (speedSlider != null)
        {
            speedSlider.value = speechRate;
            speedSlider.onValueChanged.AddListener(SetSpeechRate);
        }
        
        // Setup dropdown
        if (voiceDropdown != null)
        {
            voiceDropdown.onValueChanged.AddListener(SetVoice);
        }
        
        // Setup toggle
        if (autoSpeakToggle != null)
        {
            autoSpeakToggle.isOn = false;
        }
    }
    
    private void FindReferences()
    {
        aiManager = FindObjectOfType<CharacterAIManager>();
        journalUI = FindObjectOfType<CharacterAIJournalUI>();
        
        // Subscribe to AI response events
        if (aiManager != null)
        {
            aiManager.OnResponseReceived += OnAIResponseReceived;
        }
    }
    
    private void OnAIResponseReceived(string response)
    {
        lastAIResponse = response;
        
        // Auto-speak if enabled
        if (autoSpeakToggle != null && autoSpeakToggle.isOn)
        {
            SpeakText(response);
        }
    }
    
    public void SpeakText(string text)
    {
        if (!enableTTS || string.IsNullOrEmpty(text))
        {
            return;
        }
        
        // Add to queue
        speechQueue.Enqueue(text);
        
        // Start speaking if not already speaking
        if (!isSpeaking)
        {
            StartCoroutine(ProcessSpeechQueue());
        }
    }
    
    public void SpeakLastResponse()
    {
        if (!string.IsNullOrEmpty(lastAIResponse))
        {
            SpeakText(lastAIResponse);
        }
        else
        {
            UpdateStatusText("No response to speak");
        }
    }
    
    private IEnumerator ProcessSpeechQueue()
    {
        while (speechQueue.Count > 0)
        {
            string textToSpeak = speechQueue.Dequeue();
            yield return StartCoroutine(SpeakTextCoroutine(textToSpeak));
        }
    }
    
    private IEnumerator SpeakTextCoroutine(string text)
    {
        isSpeaking = true;
        currentSpeechText = text;
        
        // Visual feedback
        ShowSpeakingIndicator();
        OnSpeechStarted?.Invoke();
        
        // Platform-specific TTS implementation
        #if UNITY_ANDROID && !UNITY_EDITOR
        yield return StartCoroutine(SpeakAndroidTTS(text));
        #elif UNITY_IOS && !UNITY_EDITOR
        yield return StartCoroutine(SpeakIOSTTS(text));
        #else
        yield return StartCoroutine(SpeakWindowsTTS(text));
        #endif
        
        // Speech completed
        isSpeaking = false;
        isPaused = false;
        currentSpeechText = "";
        
        HideSpeakingIndicator();
        OnSpeechCompleted?.Invoke();
    }
    
    #if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator SpeakAndroidTTS(string text)
    {
        // Android TTS implementation
        AndroidJavaClass ttsClass = new AndroidJavaClass("android.speech.tts.TextToSpeech");
        AndroidJavaObject ttsObject = new AndroidJavaObject("android.speech.tts.TextToSpeech", 
            new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"),
            new AndroidJavaObject("android.speech.tts.TextToSpeech$OnInitListener"));
        
        // Set speech parameters
        ttsObject.Call("setSpeechRate", speechRate);
        ttsObject.Call("setVolume", volume);
        
        // Speak text
        ttsObject.Call<int>("speak", text, 0, null);
        
        // Wait for speech to complete (estimate based on text length)
        float estimatedDuration = text.Length * 0.1f / speechRate;
        yield return new WaitForSeconds(estimatedDuration);
        
        ttsObject.Call("shutdown");
    }
    #endif
    
    #if UNITY_IOS && !UNITY_EDITOR
    private IEnumerator SpeakIOSTTS(string text)
    {
        // iOS TTS implementation would go here
        // This is a placeholder - actual implementation requires native iOS code
        UpdateStatusText($"Speaking: {text.Substring(0, Mathf.Min(20, text.Length))}...");
        
        float estimatedDuration = text.Length * 0.1f / speechRate;
        yield return new WaitForSeconds(estimatedDuration);
    }
    #endif
    
    private IEnumerator SpeakWindowsTTS(string text)
    {
        // Windows/Editor TTS simulation
        UpdateStatusText($"Speaking: {text.Substring(0, Mathf.Min(30, text.Length))}...");
        
        // Simulate speaking duration
        float estimatedDuration = text.Length * 0.05f / speechRate;
        float elapsed = 0;
        
        while (elapsed < estimatedDuration && !isPaused)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Handle pause
        while (isPaused)
        {
            yield return null;
        }
    }
    
    public void TogglePause()
    {
        if (isSpeaking)
        {
            if (isPaused)
            {
                ResumeSpeaking();
            }
            else
            {
                PauseSpeaking();
            }
        }
    }
    
    public void PauseSpeaking()
    {
        if (isSpeaking && !isPaused)
        {
            isPaused = true;
            UpdateStatusText("Speech Paused");
            OnSpeechPaused?.Invoke();
        }
    }
    
    public void ResumeSpeaking()
    {
        if (isSpeaking && isPaused)
        {
            isPaused = false;
            UpdateStatusText("Speech Resumed");
            OnSpeechResumed?.Invoke();
        }
    }
    
    public void StopSpeaking()
    {
        if (isSpeaking)
        {
            StopAllCoroutines();
            speechQueue.Clear();
            isSpeaking = false;
            isPaused = false;
            currentSpeechText = "";
            
            HideSpeakingIndicator();
            UpdateStatusText("Speech Stopped");
        }
    }
    
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        UpdateStatusText($"Volume: {(volume * 100):F0}%");
    }
    
    public void SetSpeechRate(float newRate)
    {
        speechRate = Mathf.Clamp(newRate, 0.1f, 3.0f);
        UpdateStatusText($"Speed: {speechRate:F1}x");
    }
    
    public void SetVoice(int voiceIdx)
    {
        voiceIndex = voiceIdx;
        UpdateStatusText($"Voice changed to: {voiceIndex}");
    }
    
    private void ShowSpeakingIndicator()
    {
        if (speakingIndicator != null)
        {
            speakingIndicator.SetActive(true);
        }
        
        if (speakingIcon != null)
        {
            StartCoroutine(AnimateSpeakingIcon());
        }
    }
    
    private void HideSpeakingIndicator()
    {
        if (speakingIndicator != null)
        {
            speakingIndicator.SetActive(false);
        }
    }
    
    private IEnumerator AnimateSpeakingIcon()
    {
        while (isSpeaking && speakingIcon != null)
        {
            float alpha = Mathf.PingPong(Time.time * 2f, 1f);
            Color iconColor = speakingIcon.color;
            iconColor.a = alpha;
            speakingIcon.color = iconColor;
            yield return null;
        }
        
        // Reset alpha
        if (speakingIcon != null)
        {
            Color iconColor = speakingIcon.color;
            iconColor.a = 1f;
            speakingIcon.color = iconColor;
        }
    }
    
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        
        Debug.Log($"TTS: {message}");
    }
    
    // Public getters
    public bool IsSpeaking => isSpeaking;
    public bool IsPaused => isPaused;
    public string CurrentSpeechText => currentSpeechText;
    public int QueuedSpeechCount => speechQueue.Count;
    
    private void OnDestroy()
    {
        // Cleanup
        if (aiManager != null)
        {
            aiManager.OnResponseReceived -= OnAIResponseReceived;
        }
        
        StopSpeaking();
    }
}