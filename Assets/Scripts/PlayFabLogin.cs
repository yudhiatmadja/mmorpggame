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
        statusText.text = "Login berhasil!";
        Debug.Log("Login sukses!");
        // lanjut ke scene berikutnya 
        SceneManager.LoadScene("Playground");

        Photon.Pun.PhotonNetwork.NickName = result.InfoResultPayload.PlayerProfile.DisplayName;
    }

    void OnLoginFailure(PlayFabError error)
    {
        statusText.text = "Login gagal: " + error.ErrorMessage;
        Debug.LogError("Login gagal: " + error.GenerateErrorReport());
    }

    public void Register()
    {
        var request = new RegisterPlayFabUserRequest
        {
            Username = usernameInput.text,
            Password = passwordInput.text,
            RequireBothUsernameAndEmail = false
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, result =>
        {
            statusText.text = "Registrasi berhasil!";
            Debug.Log("Registrasi sukses!");
        }, error =>
        {
            statusText.text = "Registrasi gagal: " + error.ErrorMessage;
            Debug.LogError("Registrasi gagal: " + error.GenerateErrorReport());
        });
    }
}
