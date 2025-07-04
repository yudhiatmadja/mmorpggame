using UnityEngine;

public class UIModeController : MonoBehaviour
{
    // 1. Buat instance statis (kunci dari Singleton)
    public static UIModeController instance;

    // Referensi ke skrip kamera, akan diisi secara otomatis
    [HideInInspector] // Kita sembunyikan dari inspector agar tidak diisi manual
    public CameraControllerFix cameraControllerScript;

    // Awake() dipanggil sebelum Start()
    void Awake()
    {
        // 2. Logika Singleton
        // Jika belum ada instance, jadikan object ini sebagai instance utama
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Jangan hancurkan object ini saat pindah scene
        }
        // Jika sudah ada instance lain, hancurkan object ini agar tidak ada duplikat
        else if (instance != this)
        {
            Destroy(gameObject);
        }
        DeactivateUIMode();
    }

    void Update()
    {
        // Cek jika referensi kamera belum ada, jangan lakukan apa-apa
        if (cameraControllerScript == null)
        {
            return;
        }

        // Logika input tetap sama
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            ActivateUIMode();
        }
        else if (Input.GetKeyUp(KeyCode.LeftAlt))
        {
            DeactivateUIMode();
        }
    }

    public void ActivateUIMode()
    {
        if (cameraControllerScript == null) return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        cameraControllerScript.DisablePlayerCamera();
    }

    public void DeactivateUIMode()
    {
        if (cameraControllerScript == null) return;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        cameraControllerScript.EnablePlayerCamera();
    }
}