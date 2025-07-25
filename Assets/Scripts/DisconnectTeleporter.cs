using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class DisconnectTeleporter : MonoBehaviourPunCallbacks
{
    [Tooltip("Nama scene tujuan yang akan dimuat setelah disconnect.")]
    public string sceneTujuan;
    // Tambahkan referensi ke UI Text jika ingin menampilkan pesan
    [Tooltip("Opsional: UI Text untuk menampilkan pesan 'Tekan E'.")]
    public GameObject pesanUI;

    private bool bisaTeleport = false;
    private bool sedangProsesTeleport = false;
    private PhotonView playerView;

    // Fungsi ini dipanggil saat player MASUK ke dalam area trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerView = other.GetComponent<PhotonView>();
            // Cek apakah itu player milik kita
            if (playerView != null && playerView.IsMine)
            {
                bisaTeleport = true;
                // Tampilkan pesan UI jika sudah di-set
                if (pesanUI != null)
                {
                    pesanUI.SetActive(true);
                }
            }
        }
    }

    // Fungsi ini dipanggil saat player KELUAR dari area trigger
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                bisaTeleport = false;
                playerView = null;
                // Sembunyikan pesan UI
                if (pesanUI != null)
                {
                    pesanUI.SetActive(false);
                }
            }
        }
    }

    // Fungsi Update() dipanggil setiap frame
    void Update()
    {
        // Cek jika kita bisa teleport, belum dalam proses, dan tombol E ditekan
        if (bisaTeleport && !sedangProsesTeleport && Input.GetKeyDown(KeyCode.E))
        {
            // Pastikan playerView masih valid
            if (playerView != null)
            {
                Debug.Log("Tombol E ditekan! Memulai proses disconnect...");
                sedangProsesTeleport = true;
                // Sembunyikan UI saat proses dimulai
                if (pesanUI != null)
                {
                    pesanUI.SetActive(false);
                }

                PhotonNetwork.Disconnect();
            }
        }
    }

    // Callback OnDisconnected tetap sama
    public override void OnDisconnected(DisconnectCause cause)
    {
        if (sedangProsesTeleport)
        {
            SceneManager.LoadScene(sceneTujuan);
        }
    }
}