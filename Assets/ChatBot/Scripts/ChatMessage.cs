using System;

[Serializable]
public class ChatMessage
{
    public string role; // "user" atau "assistant"
    public string content;
    public DateTime timestamp;

    public ChatMessage(string role, string content)
    {
        this.role = role;
        this.content = content;
        this.timestamp = DateTime.Now;
    }
}