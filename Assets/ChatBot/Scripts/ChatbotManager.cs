using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class ChatbotManager : MonoBehaviour
{
    [Header("Configuration")]
    public ChatbotConfig config;

    [Header("UI References")]
    public GameObject chatPanel;
    public Transform chatContainer;
    public GameObject messagePrefab;
    public TMP_InputField inputField;
    public Button sendButton;
    public ScrollRect scrollRect;
    public TextMeshProUGUI typingIndicator;

    [Header("Message Prefabs")]
    public GameObject userMessagePrefab;
    public GameObject botMessagePrefab;

    [Header("Book System")]
    public GameObject bookPanel;
    public GameObject pageTemplate;
    public Transform pagesContainer;
    public Button prevPageButton;
    public Button nextPageButton;
    public TextMeshProUGUI pageNumberText;
    public int charactersPerColumn = 500;
    public float columnWidth = 300f;
    public float columnSpacing = 20f;

    private List<ChatMessage> chatHistory = new List<ChatMessage>();
    private bool isWaitingForResponse = false;
    private Coroutine typingCoroutine;

    // Book system variables
    private List<GameObject> pages = new List<GameObject>();
    private int currentPageIndex = 0;
    private List<string> allText = new List<string>();
    private List<PageContent> pageContents = new List<PageContent>();

    [System.Serializable]
    public class PageContent
    {
        public string leftColumnText;
        public string rightColumnText;
    }

    void Start()
    {
        InitializeChatbot();
        InitializeBookSystem();
    }

    void InitializeChatbot()
    {
        // Setup UI events
        sendButton.onClick.AddListener(SendMessage);
        inputField.onEndEdit.AddListener(OnInputEndEdit);

        // Hide typing indicator
        typingIndicator.gameObject.SetActive(false);

        // Add system message if configured
        if (!string.IsNullOrEmpty(config.systemPrompt))
        {
            chatHistory.Add(new ChatMessage("system", config.systemPrompt));
        }

        // Welcome message
        string welcomeMessage = "Halo! Saya siap membantu Anda. Silakan ketik pesan Anda.";
        DisplayBotMessage(welcomeMessage);
    }

    void InitializeBookSystem()
    {
        // Setup book navigation
        prevPageButton.onClick.AddListener(PreviousPage);
        nextPageButton.onClick.AddListener(NextPage);

        // Create initial page
        CreateNewPage();
        UpdatePageNavigation();
    }

    void OnInputEndEdit(string input)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendMessage();
        }
    }

    public void SendMessage()
    {
        if (string.IsNullOrEmpty(inputField.text.Trim()) || isWaitingForResponse)
            return;

        string userMessage = inputField.text.Trim();
        inputField.text = "";

        // Display user message
        DisplayUserMessage(userMessage);

        // Add to chat history
        chatHistory.Add(new ChatMessage("user", userMessage));

        // Limit chat history
        if (chatHistory.Count > config.maxMessages)
        {
            chatHistory.RemoveAt(0);
        }

        // Send to API
        StartCoroutine(SendToAPI(userMessage));
    }

    void DisplayUserMessage(string message)
    {
        GameObject messageObj = Instantiate(userMessagePrefab, chatContainer);
        TextMeshProUGUI messageText = messageObj.GetComponentInChildren<TextMeshProUGUI>();
        messageText.text = $"{config.userName}: {message}";

        // Add to book system
        AddTextToBook($"{config.userName}: {message}");

        StartCoroutine(ScrollToBottom());
    }

    void DisplayBotMessage(string message)
    {
        GameObject messageObj = Instantiate(botMessagePrefab, chatContainer);
        TextMeshProUGUI messageText = messageObj.GetComponentInChildren<TextMeshProUGUI>();

        // Start typing animation
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeMessage(messageText, $"{config.botName}: {message}"));
    }

    IEnumerator TypeMessage(TextMeshProUGUI textComponent, string fullMessage)
    {
        textComponent.text = "";

        foreach (char c in fullMessage)
        {
            textComponent.text += c;
            yield return new WaitForSeconds(config.typingSpeed);
        }

        // Add to book system after typing is complete
        AddTextToBook(fullMessage);

        StartCoroutine(ScrollToBottom());
    }

    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    void AddTextToBook(string text)
    {
        allText.Add(text);
        RearrangeBookContent();
    }

    void RearrangeBookContent()
    {
        // Clear existing page contents
        pageContents.Clear();

        // Combine all text
        StringBuilder combinedText = new StringBuilder();
        foreach (string text in allText)
        {
            combinedText.AppendLine(text);
            combinedText.AppendLine(); // Add spacing between messages
        }

        string fullText = combinedText.ToString();

        // Split text into pages
        SplitTextIntoPages(fullText);

        // Update book display
        UpdateBookDisplay();
    }

    void SplitTextIntoPages(string fullText)
    {
        int currentIndex = 0;

        while (currentIndex < fullText.Length)
        {
            PageContent pageContent = new PageContent();

            // Fill left column
            string leftColumnText = "";
            int leftColumnLength = 0;

            while (currentIndex < fullText.Length && leftColumnLength < charactersPerColumn)
            {
                char currentChar = fullText[currentIndex];
                leftColumnText += currentChar;
                leftColumnLength++;
                currentIndex++;

                // Break at word boundaries when approaching limit
                if (leftColumnLength >= charactersPerColumn * 0.9f && char.IsWhiteSpace(currentChar))
                {
                    break;
                }
            }

            pageContent.leftColumnText = leftColumnText.TrimEnd();

            // Fill right column
            string rightColumnText = "";
            int rightColumnLength = 0;

            while (currentIndex < fullText.Length && rightColumnLength < charactersPerColumn)
            {
                char currentChar = fullText[currentIndex];
                rightColumnText += currentChar;
                rightColumnLength++;
                currentIndex++;

                // Break at word boundaries when approaching limit
                if (rightColumnLength >= charactersPerColumn * 0.9f && char.IsWhiteSpace(currentChar))
                {
                    break;
                }
            }

            pageContent.rightColumnText = rightColumnText.TrimEnd();

            pageContents.Add(pageContent);
        }

        // Ensure we have at least one page
        if (pageContents.Count == 0)
        {
            pageContents.Add(new PageContent { leftColumnText = "", rightColumnText = "" });
        }
    }

    void UpdateBookDisplay()
    {
        // Clear existing pages
        foreach (GameObject page in pages)
        {
            if (page != null)
                Destroy(page);
        }
        pages.Clear();

        // Create pages for each page content
        for (int i = 0; i < pageContents.Count; i++)
        {
            CreatePageWithContent(pageContents[i]);
        }

        // Show current page
        ShowPage(currentPageIndex);
        UpdatePageNavigation();
    }

    void CreateNewPage()
    {
        GameObject newPage = Instantiate(pageTemplate, pagesContainer);
        pages.Add(newPage);

        // Hide all pages except current
        for (int i = 0; i < pages.Count; i++)
        {
            pages[i].SetActive(i == currentPageIndex);
        }
    }

    void CreatePageWithContent(PageContent content)
    {
        GameObject newPage = Instantiate(pageTemplate, pagesContainer);

        // Find left and right column text components
        TextMeshProUGUI leftColumnText = newPage.transform.Find("LeftColumn").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI rightColumnText = newPage.transform.Find("RightColumn").GetComponent<TextMeshProUGUI>();

        // Set content
        leftColumnText.text = content.leftColumnText;
        rightColumnText.text = content.rightColumnText;

        // Configure text properties
        ConfigureColumnText(leftColumnText);
        ConfigureColumnText(rightColumnText);

        pages.Add(newPage);
        newPage.SetActive(false);
    }

    void ConfigureColumnText(TextMeshProUGUI textComponent)
    {
        textComponent.enableWordWrapping = true;
        textComponent.overflowMode = TextOverflowModes.Truncate;
        textComponent.alignment = TextAlignmentOptions.TopLeft;

        // Set column width
        RectTransform rectTransform = textComponent.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(columnWidth, rectTransform.sizeDelta.y);
    }

    public void NextPage()
    {
        if (currentPageIndex < pages.Count - 1)
        {
            currentPageIndex++;
            ShowPage(currentPageIndex);
            UpdatePageNavigation();
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowPage(currentPageIndex);
            UpdatePageNavigation();
        }
    }

    void ShowPage(int pageIndex)
    {
        // Hide all pages
        foreach (GameObject page in pages)
        {
            if (page != null)
                page.SetActive(false);
        }

        // Show current page
        if (pageIndex >= 0 && pageIndex < pages.Count && pages[pageIndex] != null)
        {
            pages[pageIndex].SetActive(true);
        }
    }

    void UpdatePageNavigation()
    {
        // Update page number text
        pageNumberText.text = $"{currentPageIndex + 1} / {pages.Count}";

        // Update button states
        prevPageButton.interactable = currentPageIndex > 0;
        nextPageButton.interactable = currentPageIndex < pages.Count - 1;
    }

    IEnumerator SendToAPI(string message)
    {
        isWaitingForResponse = true;
        typingIndicator.gameObject.SetActive(true);

        switch (config.provider)
        {
            case APIProvider.HuggingFace:
                yield return StartCoroutine(SendToHuggingFace(message));
                break;
            case APIProvider.OpenAI:
                yield return StartCoroutine(SendToOpenAI(message));
                break;
            default:
                yield return StartCoroutine(SendToOpenAI(message)); // Fallback
                break;
        }

        typingIndicator.gameObject.SetActive(false);
        isWaitingForResponse = false;
    }

    IEnumerator SendToHuggingFace(string message)
    {
        // Format prompt for roleplay model
        string formattedPrompt = FormatPromptForPeach(message);

        var requestData = new
        {
            inputs = formattedPrompt,
            parameters = new
            {
                max_new_tokens = config.maxTokens,
                temperature = config.temperature,
                top_p = config.topP,
                repetition_penalty = config.repetitionPenalty,
                return_full_text = false,
                do_sample = true,
                use_cache = config.useCache,
                wait_for_model = config.waitForModel > 0
            },
            options = new
            {
                wait_for_model = config.waitForModel > 0,
                use_cache = config.useCache
            }
        };

        string jsonData = JsonUtility.ToJson(requestData);
        Debug.Log($"Sending to HuggingFace: {jsonData}");

        UnityWebRequest request = new UnityWebRequest(config.apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // Set headers for Hugging Face
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"HuggingFace Response: {responseText}");

                // Handle different response formats
                if (responseText.StartsWith("["))
                {
                    // Array format response
                    var responses = JsonUtility.FromJson<HuggingFaceResponse>($"{{\"generated_text\":{responseText}}}");
                    if (responses.generated_text != null && responses.generated_text.Count > 0)
                    {
                        string botResponse = responses.generated_text[0].generated_text;
                        botResponse = CleanHuggingFaceResponse(botResponse);

                        chatHistory.Add(new ChatMessage("assistant", botResponse));
                        DisplayBotMessage(botResponse);
                    }
                    else
                    {
                        DisplayBotMessage("Maaf, saya tidak bisa memproses permintaan Anda saat ini.");
                    }
                }
                else
                {
                    // Try to parse as error response
                    try
                    {
                        var errorResponse = JsonUtility.FromJson<HuggingFaceResponse>(responseText);
                        if (!string.IsNullOrEmpty(errorResponse.error))
                        {
                            Debug.LogError($"HuggingFace API Error: {errorResponse.error}");
                            DisplayBotMessage("Model sedang loading, mohon tunggu sebentar dan coba lagi.");
                        }
                    }
                    catch
                    {
                        // Direct text response
                        string botResponse = CleanHuggingFaceResponse(responseText);
                        chatHistory.Add(new ChatMessage("assistant", botResponse));
                        DisplayBotMessage(botResponse);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing HuggingFace response: {e.Message}");
                DisplayBotMessage("Terjadi kesalahan dalam memproses respons.");
            }
        }
        else
        {
            Debug.LogError($"HuggingFace API Request failed: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");

            if (request.responseCode == 503)
            {
                DisplayBotMessage("Model sedang loading. Mohon tunggu 20-30 detik dan coba lagi.");
            }
            else if (request.responseCode == 401)
            {
                DisplayBotMessage("API token tidak valid. Silakan periksa konfigurasi.");
            }
            else
            {
                DisplayBotMessage("Terjadi kesalahan koneksi. Silakan coba lagi.");
            }
        }

        request.Dispose();
    }

    IEnumerator SendToOpenAI(string message)
    {
        // Original OpenAI implementation
        var requestData = new
        {
            model = config.model,
            messages = GetMessagesForAPI(),
            max_tokens = config.maxTokens,
            temperature = config.temperature
        };

        string jsonData = JsonUtility.ToJson(requestData);

        UnityWebRequest request = new UnityWebRequest(config.apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                APIResponse response = JsonUtility.FromJson<APIResponse>(request.downloadHandler.text);

                if (response.choices != null && response.choices.Count > 0)
                {
                    string botResponse = response.choices[0].message.content;
                    chatHistory.Add(new ChatMessage("assistant", botResponse));
                    DisplayBotMessage(botResponse);
                }
                else
                {
                    DisplayBotMessage("Maaf, saya tidak dapat memproses permintaan Anda saat ini.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing OpenAI response: {e.Message}");
                DisplayBotMessage("Terjadi kesalahan dalam memproses respons.");
            }
        }
        else
        {
            Debug.LogError($"OpenAI API Request failed: {request.error}");
            DisplayBotMessage("Maaf, terjadi kesalahan koneksi. Silakan coba lagi.");
        }

        request.Dispose();
    }

    string FormatPromptForPeach(string userMessage)
    {
        // Build conversation history for context
        StringBuilder prompt = new StringBuilder();

        // Add system prompt
        if (!string.IsNullOrEmpty(config.systemPrompt))
        {
            prompt.AppendLine($"<|system|>");
            prompt.AppendLine(config.systemPrompt);
            prompt.AppendLine("<|end|>");
        }

        // Add recent conversation history (last 5 messages)
        int startIndex = Mathf.Max(0, chatHistory.Count - 5);
        for (int i = startIndex; i < chatHistory.Count; i++)
        {
            var msg = chatHistory[i];
            if (msg.role == "user")
            {
                prompt.AppendLine($"<|user|>");
                prompt.AppendLine(msg.content);
                prompt.AppendLine("<|end|>");
            }
            else if (msg.role == "assistant")
            {
                prompt.AppendLine($"<|assistant|>");
                prompt.AppendLine(msg.content);
                prompt.AppendLine("<|end|>");
            }
        }

        // Add current user message
        prompt.AppendLine($"<|user|>");
        prompt.AppendLine(userMessage);
        prompt.AppendLine("<|end|>");
        prompt.AppendLine($"<|assistant|>");

        return prompt.ToString();
    }

    string CleanHuggingFaceResponse(string response)
    {
        // Remove any remaining prompt artifacts
        if (response.Contains("<|assistant|>"))
        {
            response = response.Substring(response.LastIndexOf("<|assistant|>") + "<|assistant|>".Length);
        }

        // Remove end tokens
        response = response.Replace("<|end|>", "");
        response = response.Replace("<|user|>", "");
        response = response.Replace("<|system|>", "");

        // Clean up whitespace
        response = response.Trim();

        // Remove any JSON artifacts if present
        if (response.StartsWith("\"") && response.EndsWith("\""))
        {
            response = response.Substring(1, response.Length - 2);
        }

        return response;
    }

    List<object> GetMessagesForAPI()
    {
        List<object> messages = new List<object>();

        foreach (ChatMessage msg in chatHistory)
        {
            messages.Add(new { role = msg.role, content = msg.content });
        }

        return messages;
    }

    public void ClearChat()
    {
        // Clear UI
        foreach (Transform child in chatContainer)
        {
            Destroy(child.gameObject);
        }

        // Clear history (keep system prompt)
        chatHistory.Clear();
        if (!string.IsNullOrEmpty(config.systemPrompt))
        {
            chatHistory.Add(new ChatMessage("system", config.systemPrompt));
        }

        // Clear book system
        allText.Clear();
        pageContents.Clear();
        foreach (GameObject page in pages)
        {
            if (page != null)
                Destroy(page);
        }
        pages.Clear();
        currentPageIndex = 0;

        // Create new initial page
        CreateNewPage();
        UpdatePageNavigation();

        // Welcome message
        DisplayBotMessage("Chat telah dibersihkan. Silakan mulai percakapan baru.");
    }

    public void ToggleChatPanel()
    {
        chatPanel.SetActive(!chatPanel.activeSelf);
    }

    public void ToggleBookPanel()
    {
        bookPanel.SetActive(!bookPanel.activeSelf);
    }

    public void GoToLastPage()
    {
        if (pages.Count > 0)
        {
            currentPageIndex = pages.Count - 1;
            ShowPage(currentPageIndex);
            UpdatePageNavigation();
        }
    }
}