using System;
using System.Collections.Generic;

// Hugging Face API Response Format
[Serializable]
public class HuggingFaceResponse
{
    public List<GeneratedText> generated_text;
    public string error;
}

[Serializable]
public class GeneratedText
{
    public string generated_text;
}

// Alternative format for some HF endpoints
[Serializable]
public class HuggingFaceStreamResponse
{
    public List<HFChoice> choices;
    public string model;
    public HFUsage usage;
}

[Serializable]
public class HFChoice
{
    public HFMessage message;
    public string finish_reason;
}

[Serializable]
public class HFMessage
{
    public string role;
    public string content;
}

[Serializable]
public class HFUsage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}

// Legacy OpenAI format (kept for compatibility)
[Serializable]
public class APIResponse
{
    public string id;
    public string model;
    public List<Choice> choices;
    public Usage usage;
}

[Serializable]
public class Choice
{
    public int index;
    public Message message;
    public string finish_reason;
}

[Serializable]
public class Message
{
    public string role;
    public string content;
}

[Serializable]
public class Usage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}