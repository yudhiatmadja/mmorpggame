using UnityEngine;
using StarterAssets; // Jangan lupa tambahkan using ini

public class UIModeController : MonoBehaviour
{
    public static UIModeController instance;
    public CameraControllerFix cameraControllerScript; // Ini akan diisi oleh player saat runtime

    // Referensi langsung ke "penjaga gerbang" input player
    private StarterAssetsInputs _playerInputs;

    public bool IsUIModeActive { get; private set; }

    // Di dalam script UIModeController.cs

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // DontDestroyOnLoad(gameObject); // <-- HAPUS ATAU BERI COMMENT BARIS INI
        }
        else
        {
            // Logika `else if (instance != this)` Anda bisa disederhanakan menjadi ini
            Destroy(gameObject);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            SyncPlayerControlState();
            SyncCursorState();
        }
    }

    public void ActivateUIMode()
    {
        IsUIModeActive = true;
        SyncPlayerControlState();
        SyncCursorState();
    }

    public void DeactivateUIMode()
    {
        IsUIModeActive = false;
        SyncPlayerControlState();
        SyncCursorState();
    }

    /// <summary>
    /// Fungsi baru untuk mengatur status kontrol player.
    /// </summary>
    public void SyncPlayerControlState()
    {
        // Pertama, pastikan kita punya referensi ke script input player.
        if (cameraControllerScript != null && _playerInputs == null)
        {
            // Ambil komponen StarterAssetsInputs dari player yang terdaftar.
            _playerInputs = cameraControllerScript.GetComponent<StarterAssetsInputs>();
        }

        // Jika referensi masih kosong, jangan lakukan apa-apa.
        if (_playerInputs == null) return;

        // Beri perintah langsung ke "penjaga gerbang" input.
        if (IsUIModeActive)
        {
            _playerInputs.SetControlsEnabled(false);
        }
        else
        {
            _playerInputs.SetControlsEnabled(true);
        }
    }

    /// <summary>
    /// Fungsi baru untuk mengatur status kursor.
    /// </summary>
    private void SyncCursorState()
    {
        if (IsUIModeActive)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}