using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings(); // Connect ke Photon Cloud
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("Room1", new RoomOptions { MaxPlayers = 10 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
{
    Debug.Log("Joined room successfully!");
    PhotonNetwork.Instantiate("PlayerPrefab", new Vector3(0, 1, 0), Quaternion.identity);
}

}
