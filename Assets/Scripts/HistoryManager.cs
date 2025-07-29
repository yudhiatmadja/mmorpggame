// File: HistoryManager.cs
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;

public static class HistoryManager
{
    private const string HistoryDataKey = "AIBookChatHistory";
    private static ConversationHistory currentHistory;

    // Event untuk memberitahu skrip lain
    public static event Action OnHistoryLoaded;
    public static event Action OnHistorySaved;
    public static event Action<string> OnError;

    /// <summary>
    /// Meminta data histori dari server PlayFab.
    /// </summary>
    public static void LoadHistory()
    {
        // Cek login status dan retry jika perlu
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogWarning("PlayFab belum login, akan retry...");
            LoadHistoryWithRetry(3);
            return;
        }

        Debug.Log("Meminta histori dari PlayFab...");
        var request = new GetUserDataRequest { 
            Keys = new List<string> { HistoryDataKey }
        };
        PlayFabClientAPI.GetUserData(request, OnLoadSuccess, OnLoadFailure);
    }

    // Fungsi retry untuk load yang disederhanakan
    private static void LoadHistoryWithRetry(int retryCount)
    {
        if (retryCount <= 0)
        {
            Debug.LogError("Gagal load history setelah beberapa kali retry.");
            currentHistory = new ConversationHistory();
            OnHistoryLoaded?.Invoke();
            return;
        }

        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            var request = new GetUserDataRequest { 
                Keys = new List<string> { HistoryDataKey }
            };
            PlayFabClientAPI.GetUserData(request, OnLoadSuccess, OnLoadFailure);
        }
        else
        {
            Debug.Log($"Retry loading history... {retryCount} attempts left");
            // Gunakan Invoke untuk retry setelah delay
            var delayObject = new GameObject("DelayHelper");
            var mono = delayObject.AddComponent<DelayHelper>();
            mono.DelayedAction(1f, () => {
                LoadHistoryWithRetry(retryCount - 1);
                UnityEngine.Object.Destroy(delayObject);
            });
        }
    }

    /// <summary>
    /// Menyimpan histori ke PlayFab dengan validasi yang lebih ketat.
    /// </summary>
    public static void SaveHistory()
    {
        if (currentHistory == null)
        {
            Debug.LogWarning("Tidak ada data histori untuk disimpan.");
            return;
        }

        // PERBAIKAN 3: Validasi login yang lebih robust
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogError("Pemain belum login ke PlayFab! Tidak bisa menyimpan histori.");
            OnError?.Invoke("Player is not logged in.");
            return;
        }

        // PERBAIKAN 4: Validasi data sebelum save
        try
        {
            string json = JsonUtility.ToJson(currentHistory);
            
            // Cek apakah JSON valid dan tidak kosong
            if (string.IsNullOrEmpty(json) || json == "{}")
            {
                Debug.LogWarning("Data histori kosong, tidak perlu disimpan.");
                return;
            }

            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> { { HistoryDataKey, json } },
                // PERBAIKAN 5: Tambahkan permission agar data bisa diakses oleh user
                Permission = UserDataPermission.Private
            };

            Debug.Log($"Menyimpan histori ke PlayFab... Data: {json.Substring(0, Math.Min(100, json.Length))}...");
            PlayFabClientAPI.UpdateUserData(request, OnSaveSuccess, OnSaveFailure);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saat serializing history: {ex.Message}");
            OnError?.Invoke($"Serialization error: {ex.Message}");
        }
    }

    public static ConversationHistory GetCurrentHistory()
    {
        if (currentHistory == null)
        {
            Debug.LogWarning("Histori belum dimuat. Membuat instance baru untuk sementara.");
            currentHistory = new ConversationHistory();
        }
        return currentHistory;
    }
    
    public static void ClearAllConversations()
    {
        if (currentHistory != null)
        {
            currentHistory.allConversations.Clear();
            Debug.Log("Histori di memori telah dibersihkan.");
        }
    }

    // PERBAIKAN 6: Callback yang lebih informatif
    private static void OnLoadSuccess(GetUserDataResult result)
    {
        Debug.Log("Berhasil mendapatkan data dari PlayFab!");
        
        if (result.Data != null && result.Data.ContainsKey(HistoryDataKey))
        {
            string json = result.Data[HistoryDataKey].Value;
            Debug.Log($"Data JSON diterima: {json}");
            
            try
            {
                currentHistory = JsonUtility.FromJson<ConversationHistory>(json);
                if (currentHistory == null)
                {
                    Debug.LogWarning("JSON berhasil di-parse tapi hasilnya null, membuat history baru.");
                    currentHistory = new ConversationHistory();
                }
                else
                {
                    Debug.Log($"Histori berhasil dimuat dengan {currentHistory.allConversations.Count} percakapan.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error parsing JSON: {ex.Message}");
                currentHistory = new ConversationHistory();
            }
        }
        else
        {
            Debug.Log("Tidak ada histori ditemukan di PlayFab, membuat histori baru.");
            currentHistory = new ConversationHistory();
        }
        
        OnHistoryLoaded?.Invoke();
    }

    private static void OnLoadFailure(PlayFabError error)
    {
        Debug.LogError("Gagal memuat histori dari PlayFab: " + error.GenerateErrorReport());
        Debug.LogWarning("Membuat histori lokal baru karena gagal memuat.");
        currentHistory = new ConversationHistory();
        OnError?.Invoke(error.GenerateErrorReport());
        OnHistoryLoaded?.Invoke();
    }

    private static void OnSaveSuccess(UpdateUserDataResult result)
    {
        Debug.Log("Histori berhasil disimpan ke PlayFab!");
        OnHistorySaved?.Invoke();
    }

    private static void OnSaveFailure(PlayFabError error)
    {
        Debug.LogError("Gagal menyimpan histori ke PlayFab: " + error.GenerateErrorReport());
        Debug.LogError($"Error Code: {error.Error}, HTTP Code: {error.HttpCode}");
        OnError?.Invoke(error.GenerateErrorReport());
    }
}

// Helper class untuk delay action
public class DelayHelper : MonoBehaviour
{
    public void DelayedAction(float delay, System.Action action)
    {
        StartCoroutine(DelayCoroutine(delay, action));
    }

    private System.Collections.IEnumerator DelayCoroutine(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}