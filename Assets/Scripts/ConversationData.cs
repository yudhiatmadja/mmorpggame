// File: ConversationData.cs
using System;
using System.Collections.Generic;

[Serializable]
public class Conversation
{
    public string title;
    public string fullText;
    public string timestamp;
}

[Serializable]
public class ConversationHistory
{
    public List<Conversation> allConversations = new List<Conversation>();
}