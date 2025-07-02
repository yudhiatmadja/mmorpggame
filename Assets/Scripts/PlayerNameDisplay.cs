using TMPro;
using Photon.Pun;

public class PlayerNameDisplay : MonoBehaviourPun
{
    public TextMeshProUGUI nameText;

    void Start()
    {
        if (photonView.IsMine)
        {
            nameText.text = PhotonNetwork.NickName;
        }
        else
        {
            nameText.text = photonView.Owner.NickName;
        }
    }
}
