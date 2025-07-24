using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessageBubble : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI messageText;
    public Image backgroundImage;
    public LayoutElement layoutElement;
    
    [Header("Colors")]
    public Color userColor = new Color(0.2f, 0.6f, 1f, 0.8f);
    public Color botColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);
    
    public void SetMessage(string message, bool isUser)
    {
        messageText.text = message;
        backgroundImage.color = isUser ? userColor : botColor;
        
        // Adjust layout based on content
        Canvas.ForceUpdateCanvases();
        float textHeight = messageText.preferredHeight;
        layoutElement.preferredHeight = textHeight + 20f; // Add padding
    }
}