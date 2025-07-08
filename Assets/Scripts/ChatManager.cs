using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using System.IO;

public class ChatManager : MonoBehaviour, IOnEventCallback
{
    public TMP_InputField chatInput;
    public TextMeshProUGUI chatDisplay;

    private HashSet<string> badWords = new HashSet<string>();

    void Start()
    {
        LoadBadWords();
    }

    void LoadBadWords()
    {
        TextAsset file = Resources.Load<TextAsset>("badwords");
        if (file == null)
        {
            Debug.LogWarning("badwords.csv not found in Resources folder.");
            return;
        }

        using (StringReader reader = new StringReader(file.text))
        {
            string line;
            bool isFirstLine = true;
            while ((line = reader.ReadLine()) != null)
            {
                if (isFirstLine) { isFirstLine = false; continue; } // Skip header
                badWords.Add(line.Trim().ToLower());
            }
        }
    }

    string FilterBadWords(string input)
    {
        string[] words = input.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            string cleanWord = words[i].ToLower().Trim();
            if (badWords.Contains(cleanWord))
            {
                words[i] = new string('*', cleanWord.Length);
            }
        }
        return string.Join(" ", words);
    }

    public void SendChat(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        message = FilterBadWords(message);

        object[] content = new object[] { PhotonNetwork.NickName, message };

        PhotonNetwork.RaiseEvent(
            0,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );

        chatInput.text = "";
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

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
