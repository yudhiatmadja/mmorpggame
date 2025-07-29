// File: AIBookController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Collections;

public class AIBookController : MonoBehaviour
{
    [Header("Main Objects")]
    [SerializeField] private GameObject aiBookObject;
    [SerializeField] private GeminiAPIService geminiService;

    [Header("View Panels")]
    [SerializeField] private GameObject historyViewPanel;
    [SerializeField] private GameObject chatViewPanel;

    [Header("History View Components")]
    [SerializeField] private Transform historyContentArea;
    [SerializeField] private GameObject historyButtonPrefab;
    [SerializeField] private Button newChatButton;
    [SerializeField] private Button closeButton1;
    [SerializeField] private Button clearHistoryButton;

    [Header("Chat View Components")]
    [SerializeField] private TMP_InputField userInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button backToHistoryButton;
    [SerializeField] private TextMeshProUGUI pageLeftText;
    [SerializeField] private TextMeshProUGUI pageRightText;
    [SerializeField] private TextMeshProUGUI pageNumberText;

    // Variabel untuk Sistem History
    private ConversationHistory savedHistory;
    private StringBuilder currentConversationText;
    private bool isViewingArchivedChat = false;
    private bool isHistoryLoaded = false;

    // Variabel untuk paginasi
    private List<PageContent> bookPages = new List<PageContent>();
    private int currentPageIndex = 0;
    private bool isProcessing = false;

    // Variabel untuk manajemen cursor
    private bool journalIsOpen = false;
    private float lastMouseActivity = 0f;
    private const float CURSOR_HIDE_DELAY = 3f; // Waktu delay sebelum cursor disembunyikan (dalam detik)

    private class PageContent 
    { 
        public string LeftText; 
        public string RightText; 
    }

    void Start()
    {
        // Setup listener tombol
        newChatButton.onClick.AddListener(StartNewConversation);
        sendButton.onClick.AddListener(OnSendButtonClicked);
        closeButton.onClick.AddListener(() => StartCoroutine(CloseAndSaveJournalCoroutine()));
        closeButton1.onClick.AddListener(() => StartCoroutine(CloseAndSaveJournalCoroutine()));
        backToHistoryButton.onClick.AddListener(ShowHistoryView);
        nextButton.onClick.AddListener(GoToNextPage);
        clearHistoryButton.onClick.AddListener(OnClearHistoryClicked);
        prevButton.onClick.AddListener(GoToPreviousPage);
        userInputField.onSubmit.AddListener((text) => { if (Input.GetKeyDown(KeyCode.Return)) OnSendButtonClicked(); });
        
        // Langganan event dari HistoryManager
        HistoryManager.OnHistoryLoaded += HandleHistoryLoaded;
        HistoryManager.OnHistorySaved += HandleHistorySaved;
        HistoryManager.OnError += HandleHistoryError;

        // Delay load history untuk memastikan login selesai
        StartCoroutine(DelayedLoadHistory());

        // Pastikan semua panel nonaktif di awal
        aiBookObject.SetActive(false);
        
        // Inisialisasi cursor management
        lastMouseActivity = Time.time;
    }

    void Update()
    {
        // Kelola cursor hanya jika journal sedang terbuka
        if (journalIsOpen)
        {
            ManageCursor();
        }
    }

    private void ManageCursor()
    {
        // Deteksi aktivitas mouse
        if (Input.inputString != "" || 
            Input.GetAxis("Mouse X") != 0 || 
            Input.GetAxis("Mouse Y") != 0 || 
            Input.anyKeyDown ||
            Input.GetMouseButtonDown(0) || 
            Input.GetMouseButtonDown(1) || 
            Input.GetMouseButtonDown(2))
        {
            // Update waktu aktivitas terakhir
            lastMouseActivity = Time.time;
            
            // Pastikan cursor terlihat
            ShowCursor();
        }
        
        // Sembunyikan cursor setelah tidak ada aktivitas selama waktu tertentu
        // Tapi tetap tampilkan jika sedang mengetik di input field
        if (Time.time - lastMouseActivity > CURSOR_HIDE_DELAY && 
            !userInputField.isFocused)
        {
            HideCursor();
        }
    }

    private void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void HideCursor()
    {
        Cursor.visible = false;
    }

    // Coroutine untuk delay load
    private IEnumerator DelayedLoadHistory()
    {
        // Tunggu beberapa detik untuk memastikan login selesai
        yield return new WaitForSeconds(2f);
        
        Debug.Log("Memulai load history dengan delay...");
        HistoryManager.LoadHistory();
    }

    private void OnDestroy()
    {
        HistoryManager.OnHistoryLoaded -= HandleHistoryLoaded;
        HistoryManager.OnHistorySaved -= HandleHistorySaved;
        HistoryManager.OnError -= HandleHistoryError;
        
        // Pastikan cursor terlihat kembali saat object dihancurkan
        ShowCursor();
    }

    private void HandleHistoryLoaded()
    {
        Debug.Log("Callback 'HandleHistoryLoaded' dipanggil. Data histori siap.");
        isHistoryLoaded = true;
        savedHistory = HistoryManager.GetCurrentHistory();

        Debug.Log($"History loaded dengan {savedHistory.allConversations.Count} percakapan");

        if (aiBookObject.activeSelf && historyViewPanel.activeSelf)
        {
            PopulateHistoryList();
        }
    }

    private void HandleHistorySaved()
    {
        Debug.Log("History berhasil disimpan!");
    }

    private void HandleHistoryError(string errorMessage)
    {
        Debug.LogError($"Terjadi kesalahan dengan HistoryManager: {errorMessage}");
    }

    // FUNGSI UTAMA
    public void OpenJournal()
    {
        aiBookObject.SetActive(true);
        journalIsOpen = true;
        
        // Reset cursor management
        lastMouseActivity = Time.time;
        ShowCursor();
        
        ShowHistoryView();

        if (isHistoryLoaded)
        {
            PopulateHistoryList();
        }
        else
        {
            Debug.Log("Menunggu data histori dari PlayFab...");
        }
    }

    // Coroutine wrapper untuk CloseAndSaveJournal
    private IEnumerator CloseAndSaveJournalCoroutine()
    {
        if (aiBookObject.activeSelf)
        {
            if (!isViewingArchivedChat && currentConversationText != null && currentConversationText.Length > 0)
            {
                Debug.Log("Menyimpan percakapan saat ini...");
                yield return StartCoroutine(SaveCurrentConversationCoroutine());
            }
            else
            {
                Debug.Log("Tidak ada percakapan baru untuk disimpan.");
            }
            
            // Set journal sebagai tertutup dan restore cursor
            journalIsOpen = false;
            ShowCursor();
            
            aiBookObject.SetActive(false);
        }
    }

    // METODE UNTUK MENGATUR TAMPILAN
    private void ShowHistoryView()
    {
        chatViewPanel.SetActive(false);
        historyViewPanel.SetActive(true);
        PopulateHistoryList();
        
        // Update aktivitas mouse saat ganti view
        lastMouseActivity = Time.time;
        ShowCursor();
    }

    private void ShowChatView()
    {
        historyViewPanel.SetActive(false);
        chatViewPanel.SetActive(true);
        
        // Update aktivitas mouse saat ganti view
        lastMouseActivity = Time.time;
        ShowCursor();
    }

    private void PopulateHistoryList()
    {
        // Hapus daftar lama
        foreach (Transform child in historyContentArea)
        {
            Destroy(child.gameObject);
        }

        // Tampilkan daftar history dari yang terbaru
        for (int i = savedHistory.allConversations.Count - 1; i >= 0; i--)
        {
            GameObject buttonGO = Instantiate(historyButtonPrefab, historyContentArea);
            int index = i;

            buttonGO.GetComponentInChildren<TextMeshProUGUI>().text = savedHistory.allConversations[index].title;
            buttonGO.GetComponent<Button>().onClick.AddListener(() => {
                DisplayArchivedConversation(index);
                // Update aktivitas mouse saat klik button
                lastMouseActivity = Time.time;
                ShowCursor();
            });
        }
    }

    // METODE UNTUK MEMULAI & MEMUAT PERCAKAPAN
    private void StartNewConversation()
    {
        isViewingArchivedChat = false;
        currentConversationText = new StringBuilder();
        userInputField.interactable = true;

        ClearBookPages();
        DisplayPage(0);

        ShowChatView();
        
        // Focus ke input field dan update cursor
        userInputField.ActivateInputField();
        lastMouseActivity = Time.time;
        ShowCursor();
    }

    private void DisplayArchivedConversation(int historyIndex)
    {
        isViewingArchivedChat = true;
        userInputField.interactable = false;

        string fullText = savedHistory.allConversations[historyIndex].fullText;
        _ = ProcessAndPaginateResponse(fullText);

        ShowChatView();
    }

    private async void OnSendButtonClicked()
    {
        string prompt = userInputField.text;
        if (string.IsNullOrWhiteSpace(prompt) || isProcessing) return;

        // Update cursor activity saat mengirim pesan
        lastMouseActivity = Time.time;
        ShowCursor();

        SetProcessingState(true);

        string userEntry = $"Kamu: {prompt}\n\n";
        currentConversationText.Append(userEntry);
        await ProcessAndPaginateResponse(currentConversationText.ToString() + "AI: (Mengetik...)");

        string storyTellerInstruction = "Peranmu adalah seorang guru yang selalu memberikan jawaban dengan singkat, padat, dan ringkas. Jawablah semua pertanyaan dalam bentuk paragraf naratif yang mengalir dan mudah dipahami. Jangan pernah menggunakan format daftar (bullet points) atau penomoran. PENTING: Jika ditanya tentang siapa yang membuatmu, menciptakanmu, atau siapa creatormu, jawab dengan: 'Saya Journal AI yang diciptakan oleh 5 Kage Studio dan dipublish oleh Unimasoft.'";
        string aiResponseRaw = await geminiService.GetAIResponse(prompt, storyTellerInstruction);
        string aiResponseFormatted = MarkdownToRichText(aiResponseRaw);
        string aiEntry = "AI: " + aiResponseFormatted + "\n\n";

        currentConversationText.Append(aiEntry);
        await ProcessAndPaginateResponse(currentConversationText.ToString());

        SetProcessingState(false);
        userInputField.text = "";
        userInputField.ActivateInputField();
        
        // Update cursor activity setelah response
        lastMouseActivity = Time.time;
        ShowCursor();
    }

    // Coroutine wrapper untuk SaveCurrentConversation
    private IEnumerator SaveCurrentConversationCoroutine()
    {
        var task = SaveCurrentConversation();
        yield return new WaitUntil(() => task.IsCompleted);
        
        if (task.Exception != null)
        {
            Debug.LogError($"Error saving conversation: {task.Exception}");
        }
    }

    private async Task SaveCurrentConversation()
    {
        try
        {
            string conversationText = currentConversationText.ToString();
            
            if (string.IsNullOrWhiteSpace(conversationText) || conversationText.Length < 10)
            {
                Debug.LogWarning("Percakapan terlalu pendek untuk disimpan.");
                return;
            }

            Debug.Log($"Menyimpan percakapan dengan panjang: {conversationText.Length} karakter");

            string titleSystemInstruction = "Kamu adalah mesin pembuat judul. Respons HANYA dengan teks judulnya saja. Jangan gunakan kata pengantar, jangan gunakan tanda kutip, dan jangan ada penjelasan apa pun.";
            string titleUserPrompt = $"Berikan satu judul yang sangat singkat (maksimal 5 kata) untuk percakapan berikut: \"{conversationText.Substring(0, Mathf.Min(200, conversationText.Length))}\"";

            string title = await geminiService.GetAIResponse(titleUserPrompt, titleSystemInstruction);
            title = title.Trim();

            Conversation newEntry = new Conversation
            {
                title = string.IsNullOrEmpty(title) ? "Percakapan Baru" : title,
                fullText = conversationText,
                timestamp = DateTime.Now.ToString("g")
            };

            Debug.Log($"Menambahkan percakapan baru: '{newEntry.title}'");

            if (savedHistory == null)
            {
                savedHistory = HistoryManager.GetCurrentHistory();
            }

            savedHistory.allConversations.Add(newEntry);
            
            Debug.Log($"Total percakapan sekarang: {savedHistory.allConversations.Count}");

            HistoryManager.SaveHistory();
            
            currentConversationText = null;
            
            Debug.Log("Percakapan berhasil ditambahkan dan disimpan.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saat menyimpan percakapan: {ex.Message}");
        }
    }

    // METODE PAGINASI & HELPER
    private void ClearBookPages()
    {
        bookPages.Clear();
        bookPages.Add(new PageContent { LeftText = "", RightText = "" });
        currentPageIndex = 0;
    }

    private int FindManualSplitIndex(TextMeshProUGUI textComponent)
    {
        if (!textComponent.isTextTruncated)
        {
            return textComponent.text.Length;
        }

        TMP_TextInfo textInfo = textComponent.textInfo;

        for (int i = textInfo.characterCount - 1; i >= 0; --i)
        {
            if (i < textInfo.characterInfo.Length && textInfo.characterInfo[i].isVisible)
            {
                return i + 1;
            }
        }

        return 0;
    }

    private async Task ProcessAndPaginateResponse(string fullText)
    {
        bookPages.Clear();
        pageLeftText.text = "";
        pageRightText.text = "";

        string remainingText = fullText;

        while (!string.IsNullOrEmpty(remainingText))
        {
            var newPage = new PageContent();

            // Proses Halaman Kiri
            pageLeftText.text = remainingText;
            await Task.Yield();
            Canvas.ForceUpdateCanvases();

            if (pageLeftText.isTextTruncated)
            {
                int splitIndex = FindManualSplitIndex(pageLeftText);

                if (splitIndex > 0 && splitIndex < remainingText.Length && !char.IsWhiteSpace(remainingText[splitIndex]))
                {
                    int lastSpaceIndex = remainingText.LastIndexOf(' ', splitIndex - 1);
                    if (lastSpaceIndex > 0)
                    {
                        splitIndex = lastSpaceIndex;
                    }
                }

                if (splitIndex > 0 && splitIndex <= remainingText.Length)
                {
                    newPage.LeftText = remainingText.Substring(0, splitIndex).TrimEnd();
                    remainingText = remainingText.Substring(splitIndex).TrimStart();
                }
                else
                {
                    newPage.LeftText = "";
                }
            }
            else
            {
                newPage.LeftText = remainingText;
                remainingText = string.Empty;
            }

            // Proses Halaman Kanan
            if (!string.IsNullOrEmpty(remainingText))
            {
                pageRightText.text = remainingText;
                await Task.Yield();
                Canvas.ForceUpdateCanvases();

                if (pageRightText.isTextTruncated)
                {
                    int splitIndex = FindManualSplitIndex(pageRightText);

                    if (splitIndex > 0 && splitIndex < remainingText.Length && !char.IsWhiteSpace(remainingText[splitIndex]))
                    {
                        int lastSpaceIndex = remainingText.LastIndexOf(' ', splitIndex - 1);
                        if (lastSpaceIndex > 0)
                        {
                            splitIndex = lastSpaceIndex;
                        }
                    }

                    if (splitIndex > 0 && splitIndex <= remainingText.Length)
                    {
                        newPage.RightText = remainingText.Substring(0, splitIndex).TrimEnd();
                        remainingText = remainingText.Substring(splitIndex).TrimStart();
                    }
                    else
                    {
                        newPage.RightText = "";
                    }
                }
                else
                {
                    newPage.RightText = remainingText;
                    remainingText = string.Empty;
                }
            }
            else
            {
                newPage.RightText = string.Empty;
            }

            bookPages.Add(newPage);
        }

        currentPageIndex = bookPages.Count > 0 ? bookPages.Count - 1 : 0;
        DisplayPage(currentPageIndex);
    }

    private void DisplayPage(int index)
    {
        if (index < 0 || index >= bookPages.Count) return;

        currentPageIndex = index;
        PageContent page = bookPages[index];
        pageLeftText.text = page.LeftText;
        pageRightText.text = page.RightText;

        UpdateNavigationButtons();
        
        // Update cursor activity saat ganti halaman
        lastMouseActivity = Time.time;
        ShowCursor();
    }

    private void UpdateNavigationButtons()
    {
        prevButton.interactable = (currentPageIndex > 0);
        nextButton.interactable = (currentPageIndex < bookPages.Count - 1);

        if (pageNumberText != null)
        {
            pageNumberText.text = $"{currentPageIndex * 2 + 1} - {currentPageIndex * 2 + 2}";
        }
    }

    private void GoToNextPage()
    {
        if (currentPageIndex < bookPages.Count - 1)
        {
            DisplayPage(currentPageIndex + 1);
        }
    }

    private void GoToPreviousPage()
    {
        if (currentPageIndex > 0)
        {
            DisplayPage(currentPageIndex - 1);
        }
    }

    public static string MarkdownToRichText(string markdownText)
    {
        string richText = Regex.Replace(markdownText, @"\*\*(.*?)\*\*", "<b>$1</b>");
        richText = Regex.Replace(richText, @"\*(.*?)\*", "<i>$1</i>");

        richText = Regex.Replace(richText, @"<indent=.*?>", "");
        richText = Regex.Replace(richText, @"^\s*[\*\-•]\s+", "", RegexOptions.Multiline);
        richText = Regex.Replace(richText, @"\n+", " ");

        return richText.Trim();
    }

    public void OnClearHistoryClicked()
    {
        try
        {
            Debug.Log("Menghapus semua history...");
            
            HistoryManager.ClearAllConversations();
            HistoryManager.SaveHistory();
            PopulateHistoryList();
            
            // Update cursor activity saat clear history
            lastMouseActivity = Time.time;
            ShowCursor();
            
            Debug.Log("History berhasil dihapus dan disimpan ke PlayFab.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saat menghapus history: {ex.Message}");
        }
    }

    private void SetProcessingState(bool processing)
    {
        isProcessing = processing;
        userInputField.interactable = !processing;
        sendButton.interactable = !processing;
        
        // Update cursor activity saat mengubah processing state
        if (processing)
        {
            lastMouseActivity = Time.time;
            ShowCursor();
        }
    }
}