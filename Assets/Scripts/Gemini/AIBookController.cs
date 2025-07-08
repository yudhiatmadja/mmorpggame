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

    public void ToggleBook()
    {
        bool isActive = !aiBookObject.activeSelf;
        aiBookObject.SetActive(isActive);
        if (isActive)
        {
            // Saat buku dibuka, reset ke halaman pertama jika ada konten
            if (bookPages.Count > 0)
            {
                currentPageIndex = 0;
                DisplayPage(currentPageIndex);
            }
        }
    }

    public void CloseBook()
    {
        aiBookObject.SetActive(false);
    }

    private async void OnSendButtonClicked()
    {
        string prompt = userInputField.text;
        if (string.IsNullOrWhiteSpace(prompt) || isProcessing) return;

        SetProcessingState(true);

        // Tambahkan prompt user ke buku terlebih dahulu
        string userEntry = $"Kamu: {prompt}\n\n";
        await ProcessAndPaginateResponse(userEntry + "AI: (Mengetik...)");

        // Dapatkan respon dari AI
        string aiResponseRaw = await geminiService.GetAIResponse(prompt);

        // --- LANGKAH BARU: KONVERSI MARKDOWN KE RICH TEXT ---
        string aiResponseFormatted = MarkdownToRichText(aiResponseRaw);

        // Gabungkan prompt user dengan respon AI dan perbarui buku
        string fullEntry = userEntry + "AI: " + aiResponseFormatted;
        await ProcessAndPaginateResponse(fullEntry);

        SetProcessingState(false);
        userInputField.text = "";
        userInputField.ActivateInputField(); // Fokus kembali ke input field
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


    // --- GANTI SELURUH METODE LAMA ANDA DENGAN YANG INI ---
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
                // Panggil fungsi manual kita untuk mendapatkan titik potong
                int splitIndex = FindManualSplitIndex(pageLeftText);

                // Pastikan splitIndex valid sebelum memotong string
                if (splitIndex > 0 && splitIndex <= remainingText.Length)
                {
                    newPage.LeftText = remainingText.Substring(0, splitIndex).TrimEnd();
                    remainingText = remainingText.Substring(splitIndex);
                }
                else // Jika ada masalah, anggap saja halaman kiri kosong
                {
                    newPage.LeftText = "";
                    // 'remainingText' tidak berubah dan akan diproses di halaman selanjutnya
                }
            }
            else
            {
                newPage.LeftText = remainingText;
                remainingText = string.Empty;
            }

            // --- Proses Halaman Kanan ---
            if (!string.IsNullOrEmpty(remainingText))
            {
                pageRightText.text = remainingText;
                await Task.Yield();
                Canvas.ForceUpdateCanvases();

                if (pageRightText.isTextTruncated)
                {
                    // Panggil fungsi manual yang sama untuk halaman kanan
                    int splitIndex = FindManualSplitIndex(pageRightText);

                    if (splitIndex > 0 && splitIndex <= remainingText.Length)
                    {
                        newPage.RightText = remainingText.Substring(0, splitIndex).TrimEnd();
                        remainingText = remainingText.Substring(splitIndex);
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

    /// <summary>
    /// Mengonversi string Markdown sederhana ke format Rich Text TextMeshPro.
    /// </summary>
    /// <param name="markdownText">Teks mentah dari AI dengan format Markdown.</param>
    /// <returns>Teks yang sudah diformat untuk TextMeshPro.</returns>
    public static string MarkdownToRichText(string markdownText)
    {
        // 1. Konversi Bold: **teks** menjadi <b>teks</b>
        // Pola Regex: \*\*(.*?)\*\*
        // - \*\* -> Mencari karakter literal "**"
        // - (.*?) -> Menangkap semua karakter di antaranya secara non-greedy
        // - $1 -> Merujuk pada teks yang ditangkap di dalam kurung
        string richText = Regex.Replace(markdownText, @"\*\*(.*?)\*\*", "<b>$1</b>");

        // 2. Konversi Italic: *teks* menjadi <i>teks</i> (jika diperlukan)
        richText = Regex.Replace(richText, @"\*(.*?)\*", "<i>$1</i>");

        // 3. Konversi List/Bullet point: "* " di awal baris menjadi "• " dengan indentasi
        // RegexOptions.Multiline diperlukan untuk mendeteksi awal baris (^)
        richText = Regex.Replace(richText, @"^\* (.*)", "<indent=15%>• $1</indent>", RegexOptions.Multiline);

        return richText;
    }

    private void SetProcessingState(bool processing)
    {
        isProcessing = processing;
        userInputField.interactable = !processing;
        sendButton.interactable = !processing;
    }
}