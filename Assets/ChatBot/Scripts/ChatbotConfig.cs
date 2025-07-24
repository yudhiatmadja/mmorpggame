using UnityEngine;

[CreateAssetMenu(fileName = "ChatbotConfig", menuName = "Chatbot/Config")]
public class ChatbotConfig : ScriptableObject
{
    [Header("API Configuration")]
    public APIProvider provider = APIProvider.HuggingFace;
    public string apiUrl = "https://api-inference.huggingface.co/models/QuantFactory/Peach-2.0-9B-8k-Roleplay-GGUF";
    public string apiKey = "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";
    public string model = "QuantFactory/Peach-2.0-9B-8k-Roleplay-GGUF";
    
    [Header("Hugging Face Settings")]
    public bool useInference = true; // Use HF Inference API
    public float waitForModel = 20f; // Wait time if model is loading
    public bool useCache = true;
    
    [Header("Chat Settings")]
    public int maxTokens = 512;
    public float temperature = 0.8f;
    public float topP = 0.9f;
    public float repetitionPenalty = 1.1f;
    public int maxMessages = 15;
    
    [Header("UI Settings")]
    public float typingSpeed = 0.03f;
    public string botName = "Peach";
    public string userName = "User";
    
    [Header("Roleplay Settings")]
    [TextArea(5, 15)]
    public string systemPrompt = @"You are Peach, a helpful and friendly AI assistant with a warm personality. You're designed for roleplay and conversation in a Unity game environment. 

Keep your responses engaging, natural, and moderately detailed. You can be playful and expressive while remaining helpful. Adapt your tone to match the conversation context.

Remember to:
- Be conversational and engaging
- Show personality in your responses  
- Keep responses reasonably concise but descriptive
- Stay in character as Peach";
    
    [Header("Advanced Settings")]
    public bool useStreaming = false;
    public int seed = -1; // -1 for random
}

public enum APIProvider
{
    HuggingFace,
    OpenAI,
    Custom
}