using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using ExitGames.Client.Photon; // kalau pakai TextMeshPro

public class ChatManager : MonoBehaviour, IOnEventCallback
{
    public TMP_InputField chatInput;
    public TextMeshProUGUI chatDisplay;

    public void SendChat(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        object[] content = new object[] { PhotonNetwork.NickName, message };

        PhotonNetwork.RaiseEvent(
            0,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );

        chatInput.text = "";
    }

    // 🔄 Register listener saat aktif
    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    // ✅ Listener utama
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == 0)
        {
            object[] data = (object[])photonEvent.CustomData;
            string sender = (string)data[0];
            string message = (string)data[1];

            chatDisplay.text += $"\n<color=yellow>{sender}:</color> {message}";
        }
    }
}
