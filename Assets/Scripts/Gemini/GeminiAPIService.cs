using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;

// Kelas untuk struktur data JSON yang dikirim dan diterima dari Gemini API
[System.Serializable]
public class GeminiRequest
{
    public Content[] contents;
}

[System.Serializable]
public class Content
{
    public Part[] parts;
    public string role;
}

[System.Serializable]
public class Part
{
    public string text;
}

[System.Serializable]
public class GeminiResponse
{
    public Candidate[] candidates;
}

[System.Serializable]
public class Candidate
{
    public Content content;
    public string finishReason;
    public int index;
}


public class GeminiAPIService : MonoBehaviour
{
    // GANTI DENGAN API KEY ANDA
    public string apiKey = "YOURAPIKEY";
    public string model = "gemini-2.5-flash";
    private string url;

    private void Awake()
    {
        url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
    }

    public async Task<string> GetAIResponse(string userPrompt, string systemInstruction)
    {
        // Membuat body request dengan instruksi sistem dan prompt pengguna
        var requestBody = new GeminiRequest
        {
            contents = new Content[]
            {
            // Tambahkan instruksi sistem sebagai giliran pertama
            new Content { parts = new Part[] { new Part { text = systemInstruction } }, role = "user" },
            new Content { parts = new Part[] { new Part { text = "Baik, saya mengerti. Saya akan mengikuti instruksi tersebut." } }, role = "model" },
            
            // Kemudian tambahkan prompt dari pengguna
            new Content { parts = new Part[] { new Part { text = userPrompt } }, role = "user" }
            }
        };
        string jsonBody = JsonUtility.ToJson(requestBody, true); // 'true' untuk pretty print (opsional)

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("Mengirim request ke Gemini...");
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(jsonResponse);

                // Cek jika ada response text
                if (response != null && response.candidates != null && response.candidates.Length > 0)
                {
                    Debug.Log("Respon diterima dari Gemini.");
                    return response.candidates[0].content.parts[0].text;
                }
                else
                {
                    Debug.LogWarning("Respon dari Gemini tidak valid atau kosong: " + jsonResponse);
                    return "Maaf, saya tidak menerima respon yang valid saat ini.";
                }
            }
            else
            {
                Debug.LogError("Error: " + request.error + "\nResponse: " + request.downloadHandler.text);
                return "Maaf, terjadi kesalahan saat menghubungi AI. Coba periksa koneksi atau API Key Anda.";
            }
        }
    }
}