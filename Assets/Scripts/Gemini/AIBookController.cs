using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class AIBookController : MonoBehaviour
{
    [Header("Main Objects")]
    [SerializeField] private GameObject aiBookObject;
    [SerializeField] private GeminiAPIService geminiService;

    [Header("View Panels")]
    [SerializeField] private GameObject historyViewPanel;
    [SerializeField] private GameObject chatViewPanel;

    [Header("History View Components")]
    [SerializeField] private Transform historyContentArea; // Object "Content" dari ScrollView
    [SerializeField] private GameObject historyButtonPrefab; // Prefab tombol yang kita buat
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

    // --- Variabel untuk Sistem History ---
    private ConversationHistory savedHistory;
    private StringBuilder currentConversationText;
    private bool isViewingArchivedChat = false;

    // --- Variabel untuk paginasi (sama seperti sebelumnya) ---
    private List<PageContent> bookPages = new List<PageContent>();
    private int currentPageIndex = 0;
    private bool isProcessing = false;

    private class PageContent { public string LeftText; public string RightText; }

    void Start()
    {
        // Setup listener tombol
        newChatButton.onClick.AddListener(StartNewConversation);
        sendButton.onClick.AddListener(OnSendButtonClicked);
        closeButton.onClick.AddListener(CloseAndSaveJournal);
        closeButton1.onClick.AddListener(CloseAndSaveJournal);
        backToHistoryButton.onClick.AddListener(ShowHistoryView);
        nextButton.onClick.AddListener(GoToNextPage);
        clearHistoryButton.onClick.AddListener(OnClearHistoryClicked);
        prevButton.onClick.AddListener(GoToPreviousPage);
        userInputField.onSubmit.AddListener((text) => { if (Input.GetKeyDown(KeyCode.Return)) OnSendButtonClicked(); });

        // Muat history saat game dimulai
        savedHistory = HistoryManager.LoadHistory();

        // Pastikan semua panel nonaktif di awal
        aiBookObject.SetActive(false);
    }

    // --- FUNGSI UTAMA BARU ---
    public void OpenJournal()
    {
        // Fungsi ini dipanggil oleh InputManager ('J')
        aiBookObject.SetActive(true);
        ShowHistoryView();
    }

    private void CloseAndSaveJournal()
    {
        // Fungsi ini dipanggil oleh tombol Close
        if (aiBookObject.activeSelf)
        {
            // Hanya simpan jika ini adalah chat baru dan ada isinya
            if (!isViewingArchivedChat && currentConversationText != null && currentConversationText.Length > 0)
            {
                // Kita panggil tanpa await agar UI bisa langsung tertutup
                _ = SaveCurrentConversation();
            }
            aiBookObject.SetActive(false);
        }
    }

    // --- METODE UNTUK MENGATUR TAMPILAN ---

    private void ShowHistoryView()
    {
        chatViewPanel.SetActive(false);
        historyViewPanel.SetActive(true);
        PopulateHistoryList();
    }

    private void ShowChatView()
    {
        historyViewPanel.SetActive(false);
        chatViewPanel.SetActive(true);
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
            int index = i; // Penting untuk ditangkap dalam scope lokal untuk listener

            // Atur teks tombol
            buttonGO.GetComponentInChildren<TextMeshProUGUI>().text = savedHistory.allConversations[index].title;

            // Atur listener
            buttonGO.GetComponent<Button>().onClick.AddListener(() => DisplayArchivedConversation(index));
        }
    }

    // --- METODE UNTUK MEMULAI & MEMUAT PERCAKAPAN ---

    private void StartNewConversation()
    {
        isViewingArchivedChat = false;
        currentConversationText = new StringBuilder();
        userInputField.interactable = true;

        // Tampilkan halaman kosong
        ClearBookPages();
        DisplayPage(0);

        ShowChatView();
    }

    private void DisplayArchivedConversation(int historyIndex)
    {
        isViewingArchivedChat = true;
        userInputField.interactable = false; // Mode baca saja

        string fullText = savedHistory.allConversations[historyIndex].fullText;
        ProcessAndPaginateResponse(fullText); // Gunakan fungsi lama untuk menampilkan

        ShowChatView();
    }

    // --- MODIFIKASI METODE LAMA ---

    private async void OnSendButtonClicked()
    {
        string prompt = userInputField.text;
        if (string.IsNullOrWhiteSpace(prompt) || isProcessing) return;

        SetProcessingState(true);

        string userEntry = $"Kamu: {prompt}\n\n";
        // Tampilkan prompt user di buku dan simpan ke history sementara
        currentConversationText.Append(userEntry);
        await ProcessAndPaginateResponse(currentConversationText.ToString() + "AI: (Mengetik...)");

        string storyTellerInstruction = "Peranmu adalah seorang guru yang selalu memberikan jawaban dengan singkat, padat, dan ringkas. Jawablah semua pertanyaan dalam bentuk paragraf naratif yang mengalir dan mudah dipahami. Jangan pernah menggunakan format daftar (bullet points) atau penomoran.";
        string aiResponseRaw = await geminiService.GetAIResponse(prompt, storyTellerInstruction);
        string aiResponseFormatted = MarkdownToRichText(aiResponseRaw);
        string aiEntry = "AI: " + aiResponseFormatted + "\n\n";

        // Simpan jawaban AI ke history sementara dan tampilkan
        currentConversationText.Append(aiEntry);
        await ProcessAndPaginateResponse(currentConversationText.ToString());

        SetProcessingState(false);
        userInputField.text = "";
        userInputField.ActivateInputField();
    }

    // --- FUNGSI BARU UNTUK MENYIMPAN ---

    private async Task SaveCurrentConversation()
    {
        string conversationText = currentConversationText.ToString();


        // 1. Definisikan peran AI dengan sangat ketat
        string titleSystemInstruction = "Kamu adalah mesin pembuat judul. Respons HANYA dengan teks judulnya saja. Jangan gunakan kata pengantar, jangan gunakan tanda kutip, dan jangan ada penjelasan apa pun.";

        // 2. Buat prompt yang lebih direktif, bukan pertanyaan
        string titleUserPrompt = $"Berikan satu judul yang sangat singkat (maksimal 5 kata) untuk percakapan berikut: \"{conversationText}\"";

        // 3. Panggil AI dengan instruksi baru
        string title = await geminiService.GetAIResponse(titleUserPrompt, titleSystemInstruction);

        // 4. Tambahkan pembersihan ekstra untuk hasil yang lebih rapi
        title = title.Trim(); // Menghapus spasi atau baris baru di awal/akhir

        // Buat entri baru
        Conversation newEntry = new Conversation
        {
            title = string.IsNullOrEmpty(title) ? "Percakapan Baru" : title,
            fullText = conversationText,
            timestamp = System.DateTime.Now.ToString("g")
        };

        savedHistory.allConversations.Add(newEntry);
        HistoryManager.SaveHistory(savedHistory);
    }

    // --- METODE PAGINASI & HELPER (SEBAGIAN BESAR TETAP SAMA) ---

    private void ClearBookPages()
    {
        bookPages.Clear();
        bookPages.Add(new PageContent { LeftText = "", RightText = "" });
        currentPageIndex = 0;
    }

    // (Letakkan metode ini di dalam class AIBookController, di mana saja)

    /// <summary>
    /// Menemukan indeks untuk memotong teks secara manual dengan memeriksa karakter
    /// terakhir yang terlihat. Ini bekerja di semua versi TextMeshPro.
    /// </summary>
    /// <param name="textComponent">Komponen TextMeshPro yang akan diperiksa.</param>
    /// <returns>Indeks karakter pertama yang tidak terlihat.</returns>
    private int FindManualSplitIndex(TextMeshProUGUI textComponent)
    {
        if (!textComponent.isTextTruncated)
        {
            // Jika tidak terpotong, tidak ada yang perlu dihitung.
            return textComponent.text.Length;
        }

        // Dapatkan info dari teks yang sudah di-render
        TMP_TextInfo textInfo = textComponent.textInfo;

        // Cari dari belakang, karakter terakhir yang masih terlihat
        for (int i = textInfo.characterCount - 1; i >= 0; --i)
        {
            // Periksa apakah karakter ini ada di dalam array dan terlihat
            if (i < textInfo.characterInfo.Length && textInfo.characterInfo[i].isVisible)
            {
                // Kita menemukan karakter terakhir yang terlihat pada indeks 'i'.
                // Maka, kita harus memotong teks SETELAH karakter ini.
                return i + 1;
            }
        }

        // Jika karena suatu alasan tidak ada karakter yang terlihat, potong dari awal.
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

            // --- Proses Halaman Kiri ---
            pageLeftText.text = remainingText;
            await Task.Yield();
            Canvas.ForceUpdateCanvases();

            if (pageLeftText.isTextTruncated)
            {
                int splitIndex = FindManualSplitIndex(pageLeftText);

                // --- LOGIKA CERDAS UNTUK PEMOTONGAN KATA (BARU) ---
                // Cek apakah titik potong berada di dalam teks dan bukan di spasi
                if (splitIndex > 0 && splitIndex < remainingText.Length && !char.IsWhiteSpace(remainingText[splitIndex]))
                {
                    // Mundur dari titik potong untuk mencari spasi terakhir
                    int lastSpaceIndex = remainingText.LastIndexOf(' ', splitIndex - 1);

                    // Jika spasi ditemukan (dan bukan di awal sekali), gunakan itu sebagai titik potong baru
                    if (lastSpaceIndex > 0)
                    {
                        splitIndex = lastSpaceIndex;
                    }
                    // Jika tidak ada spasi (satu kata yang sangat panjang), biarkan apa adanya (hard cut).
                }

                if (splitIndex > 0 && splitIndex <= remainingText.Length)
                {
                    newPage.LeftText = remainingText.Substring(0, splitIndex).TrimEnd();

                    // --- PEMBERSIHAN SPASI AWAL (BARU) ---
                    // Hapus spasi di awal sisa teks sebelum lanjut ke halaman kanan
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

            // --- Proses Halaman Kanan (dengan logika yang sama) ---
            if (!string.IsNullOrEmpty(remainingText))
            {
                pageRightText.text = remainingText;
                await Task.Yield();
                Canvas.ForceUpdateCanvases();

                if (pageRightText.isTextTruncated)
                {
                    int splitIndex = FindManualSplitIndex(pageRightText);

                    // --- LOGIKA CERDAS UNTUK PEMOTONGAN KATA (BARU) ---
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

                        // --- PEMBERSIHAN SPASI AWAL (BARU) ---
                        // Hapus spasi di awal sisa teks untuk halaman berikutnya
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
    }

    private void UpdateNavigationButtons()
    {
        prevButton.interactable = (currentPageIndex > 0);
        nextButton.interactable = (currentPageIndex < bookPages.Count - 1);

        if (pageNumberText != null)
        {
            // Menampilkan nomor halaman (misal: 1/3)
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
        // Konversi Bold/Italic tetap ada jika Anda masih menginginkannya
        string richText = Regex.Replace(markdownText, @"\*\*(.*?)\*\*", "<b>$1</b>");
        richText = Regex.Replace(richText, @"\*(.*?)\*", "<i>$1</i>");

        // --- TAMBAHAN: HAPUS FORMAT DAFTAR SECARA PAKSA ---

        // Hapus tag indentasi yang mungkin kita buat sebelumnya
        richText = Regex.Replace(richText, @"<indent=.*?>", "");

        // Hapus simbol bullet points (*, -, •) di awal baris
        richText = Regex.Replace(richText, @"^\s*[\*\-•]\s+", "", RegexOptions.Multiline);

        // Ganti beberapa baris baru berturut-turut dengan spasi agar menjadi paragraf
        richText = Regex.Replace(richText, @"\n+", " ");

        return richText.Trim(); // Trim untuk menghapus spasi di awal/akhir
    }

    public void OnClearHistoryClicked()
    {
        // 1. Kosongkan daftar history yang ada di memori
        savedHistory.allConversations.Clear();

        // 2. Simpan daftar yang sudah kosong ke file, menimpa file lama
        HistoryManager.SaveHistory(savedHistory);

        // 3. Perbarui tampilan UI untuk menunjukkan bahwa daftar sudah kosong
        PopulateHistoryList();

        Debug.Log("Conversation history cleared.");
    }

    private void SetProcessingState(bool processing)
    {
        isProcessing = processing;
        userInputField.interactable = !processing;
        sendButton.interactable = !processing;
    }
}