using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;

    // TAMBAHKAN VARIABEL INI
    [Tooltip("Titik di mana player akan di-spawn di scene ini.")]
    public Transform spawnPoint;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("Room1", new RoomOptions { MaxPlayers = 10 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room successfully!");

        // GANTI BARIS INI
        // Gunakan posisi dan rotasi dari spawnPoint yang sudah ditentukan
        if (spawnPoint != null)
        {
            PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            // Fallback jika spawnPoint lupa di-set
            Debug.LogError("Spawn Point belum di-set di Launcher! Player di-spawn di posisi default.");
            PhotonNetwork.Instantiate(playerPrefab.name, new Vector3(0, 1, 0), Quaternion.identity);
        }
    }
}