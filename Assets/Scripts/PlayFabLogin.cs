using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayFabLogin : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text statusText;
    public GameObject statusBackground; // ← background image (misalnya Panel atau Image)

    void Start()
    {
        statusText.gameObject.SetActive(false);
        if (statusBackground != null)
            statusBackground.SetActive(false); // sembunyikan saat awal
    }

    public void Login()
    {
        var request = new LoginWithPlayFabRequest
        {
            Username = usernameInput.text,
            Password = passwordInput.text,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };

        PlayFabClientAPI.LoginWithPlayFab(request, OnLoginSuccess, OnLoginFailure);
    }

    void OnLoginSuccess(LoginResult result)
    {
        ShowStatus("Login berhasil!");
        Debug.Log("Login sukses!");

        // Set nickname ke Photon jika digunakan
        Photon.Pun.PhotonNetwork.NickName = result.InfoResultPayload.PlayerProfile.DisplayName;
<<<<<<< HEAD
=======

        // Lanjut ke scene berikutnya
>>>>>>> origin/DisplayName
        SceneManager.LoadScene("Playground");
    }

    void OnLoginFailure(PlayFabError error)
    {
        ShowStatus("Login gagal: " + error.ErrorMessage);
        Debug.LogError("Login gagal: " + error.GenerateErrorReport());
    }

    public void Register()
    {
        // Buka halaman registrasi web eksternal
        Application.OpenURL("https://backend-mmorpg.vercel.app/register");

        // Atau jika kamu ingin tetap pakai kode register dari Unity, hapus baris atas dan pakai kode di bawah ini
        /*
        var request = new RegisterPlayFabUserRequest
        {
            Username = usernameInput.text,
            Password = passwordInput.text,
            RequireBothUsernameAndEmail = false
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, result =>
        {
            ShowStatus("Registrasi berhasil!");
            Debug.Log("Registrasi sukses!");
        }, error =>
        {
            ShowStatus("Registrasi gagal: " + error.ErrorMessage);
            Debug.LogError("Registrasi gagal: " + error.GenerateErrorReport());
        });
        */
    }

    void ShowStatus(string message)
    {
        statusText.text = message;
        statusText.gameObject.SetActive(true);
        if (statusBackground != null)
            statusBackground.SetActive(true);
    }
}
