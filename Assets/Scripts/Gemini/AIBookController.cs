using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class AIBookController : MonoBehaviour
{
    [Header("Main Objects")]
    [SerializeField] public GameObject aiBookObject; // GameObject AIBook (parent)
    [SerializeField] public GeminiAPIService geminiService;

    [Header("UI Components")]
    [SerializeField] public TMP_InputField userInputField;
    [SerializeField] public Button sendButton;
    [SerializeField] public Button closeButton;
    [SerializeField] public Button nextButton;
    [SerializeField] public Button prevButton;
    [SerializeField] public TextMeshProUGUI pageLeftText;
    [SerializeField] public TextMeshProUGUI pageRightText;
    [SerializeField] public TextMeshProUGUI pageNumberText; // Opsional: untuk nomor halaman

    // Kelas internal untuk menyimpan konten per halaman
    private class PageContent
    {
        public string LeftText;
        public string RightText;
    }

    private List<PageContent> bookPages = new List<PageContent>();
    private int currentPageIndex = 0;
    private bool isProcessing = false;

    void Start()
    {
        // Nonaktifkan buku saat mulai
        if (aiBookObject != null)
        {
            aiBookObject.SetActive(false);
        }

        // Tambahkan listener ke tombol-tombol
        sendButton.onClick.AddListener(OnSendButtonClicked);
        closeButton.onClick.AddListener(CloseBook);
        nextButton.onClick.AddListener(GoToNextPage);
        prevButton.onClick.AddListener(GoToPreviousPage);

        // Listener untuk submit dengan tombol Enter
        userInputField.onSubmit.AddListener((text) => { if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) OnSendButtonClicked(); });
    }

    public void CloseBook()
    {
        // 1. Nonaktifkan object buku
        gameObject.SetActive(false);

        // 2. Minta UIModeController untuk kembali ke mode game
        if (UIModeController.instance != null)
        {
            UIModeController.instance.DeactivateUIMode();
        }
    }

    // Pastikan method ToggleBook Anda juga diubah jika ada
    public void ToggleBook()
    {
        bool isActive = !gameObject.activeSelf;
        gameObject.SetActive(isActive);

        if (isActive)
        {
            UIModeController.instance.ActivateUIMode();
        }
        else
        {
            UIModeController.instance.DeactivateUIMode();
        }
    }

    private async void OnSendButtonClicked()
    {
        string prompt = userInputField.text;
        if (string.IsNullOrWhiteSpace(prompt) || isProcessing) return;

        SetProcessingState(true);

        // Definisikan instruksi gaya atau "peran" untuk AI di sini
        string storyTellerInstruction = "Peranmu adalah seorang pendongeng yang bijaksana. Jawablah semua pertanyaan dalam bentuk paragraf naratif yang mengalir dan mudah dipahami, seolah-olah kamu sedang bercerita di dalam sebuah buku. Jangan pernah menggunakan format daftar (bullet points) atau penomoran. Gabungkan semua poin menjadi satu cerita yang utuh.";

        string userEntry = $"Kamu: {prompt}\n\n";
        await ProcessAndPaginateResponse(userEntry + "AI: (Mengetik...)");

        // Panggil GetAIResponse dengan menyertakan instruksi
        string aiResponseRaw = await geminiService.GetAIResponse(prompt, storyTellerInstruction);

        string aiResponseFormatted = MarkdownToRichText(aiResponseRaw);
        string fullEntry = userEntry + "AI: " + aiResponseFormatted;
        await ProcessAndPaginateResponse(fullEntry);

        SetProcessingState(false);
        userInputField.text = "";
        userInputField.ActivateInputField();
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

    private void SetProcessingState(bool processing)
    {
        isProcessing = processing;
        userInputField.interactable = !processing;
        sendButton.interactable = !processing;
    }
}