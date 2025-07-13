using Photon.Pun;
using UnityEngine;
using StarterAssets; // Pastikan using ini ada

public class CameraControllerFix : MonoBehaviourPun
{
    public GameObject cameraRoot;

    // Kita ambil referensi ini untuk mempermudah, karena ia ada di object yang sama
    private StarterAssetsInputs _inputs;

    void Awake()
    {
        // Ambil komponen saat Awake agar siap digunakan di Start
        _inputs = GetComponent<StarterAssetsInputs>();
    }

    void Start()
    {
        // Pengecekan ini adalah kunci dari semua logika multiplayer
        if (photonView.IsMine)
        {
            // Jika ini adalah player lokal kita...
            if (UIModeController.instance != null)
            {
                // 1. DAFTARKAN DIRI: Beri tahu UIModeController, "Ini saya, player yang harus dikontrol!"
                UIModeController.instance.cameraControllerScript = this;

                // 2. SINKRONISASI STATUS: Langsung atur mode kontrol saat player baru muncul.
                // Ini adalah baris kode yang memperbaiki bug "tidak bisa gerak setelah pindah scene".
                UIModeController.instance.SyncPlayerControlState();
            }
        }
        else
        {
            // Jika ini bukan player kita, matikan komponen yang tidak perlu
            cameraRoot.SetActive(false);
            if (_inputs != null) _inputs.enabled = false;

            // Matikan juga ThirdPersonController agar tidak memproses apa pun
            ThirdPersonController controller = GetComponent<ThirdPersonController>();
            if (controller != null) controller.enabled = false;
        }
    }

    // Fungsi-fungsi ini sekarang hanya sebagai "boneka" yang dipanggil oleh UIModeController.
    // Logika sebenarnya ada di UIModeController yang akan memanggil _inputs.SetControlsEnabled().
    // Namun, kita tetap butuh referensinya di UIModeController.
    public void DisablePlayerCamera()
    {
        // Implementasi logika ada di UIModeController
    }

    public void EnablePlayerCamera()
    {
        // Implementasi logika ada di UIModeController
    }
}