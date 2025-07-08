using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatbotUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Button chatToggleButton;
    public GameObject chatWindow;
    public TextMeshProUGUI chatButtonText;
    
    [Header("Window Settings")]
    public bool startMinimized = true;
    
    private bool isChatOpen = false;
    
    void Start()
    {
        // Initialize UI state
        isChatOpen = !startMinimized;
        chatWindow.SetActive(isChatOpen);
        UpdateButtonText();
        
        // Setup button event
        chatToggleButton.onClick.AddListener(ToggleChat);
    }
    
    void ToggleChat()
    {
        isChatOpen = !isChatOpen;
        chatWindow.SetActive(isChatOpen);
        UpdateButtonText();
    }
    
    void UpdateButtonText()
    {
        if (chatButtonText != null)
        {
            chatButtonText.text = isChatOpen ? "Tutup Chat" : "Buka Chat";
        }
    }
}