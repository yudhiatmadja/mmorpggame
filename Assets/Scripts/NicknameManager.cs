using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.SceneManagement;

public class NicknameManager : MonoBehaviour
{
    public GameObject nicknamePanel;
    public TMP_InputField nicknameInput;
    public TMP_Text statusText;
    private bool isSubmitting = false;

    void Start()
    {
        GetDisplayName();
    }

    void Update()
{
    if (nicknamePanel.activeSelf && Input.GetKeyDown(KeyCode.Return))
    {
        SubmitNickname();
    }
}

    void GetDisplayName()
{
    var request = new GetAccountInfoRequest();
    PlayFabClientAPI.GetAccountInfo(request, result =>
    {
        var displayName = result.AccountInfo?.TitleInfo?.DisplayName;

        if (!string.IsNullOrEmpty(displayName))
        {
            // ✅ DisplayName sudah ada → langsung pakai
            PlayerPrefs.SetString("Username", displayName);
            Photon.Pun.PhotonNetwork.NickName = displayName;
            nicknamePanel.SetActive(false); // pastikan panel disembunyikan
        }
        else
        {
            // ❌ Belum punya DisplayName → minta input nickname
            nicknamePanel.SetActive(true);
        }
    },
    error =>
    {
        Debug.LogError("Gagal ambil info akun: " + error.GenerateErrorReport());
    });
}


    public void SubmitNickname()
{
    if (isSubmitting) return;
    isSubmitting = true;

    string nickname = nicknameInput.text.Trim();

    if (string.IsNullOrEmpty(nickname))
    {
        statusText.text = "Nickname tidak boleh kosong!";
        isSubmitting = false;
        return;
    }

    var request = new UpdateUserTitleDisplayNameRequest
    {
        DisplayName = nickname
    };

    PlayFabClientAPI.UpdateUserTitleDisplayName(request, result =>
    {
        Debug.Log("Nickname berhasil disimpan ke PlayFab.");

        PlayerPrefs.SetString("Username", nickname);
        Photon.Pun.PhotonNetwork.NickName = nickname;

        nicknamePanel.SetActive(false);
    },
    error =>
    {
        statusText.text = "Gagal simpan nickname: " + error.ErrorMessage;
        Debug.LogError("Gagal update DisplayName: " + error.GenerateErrorReport());
        isSubmitting = false;
    });
}
}
