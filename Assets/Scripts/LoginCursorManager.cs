using UnityEngine;

public class LoginCursorManager : MonoBehaviour
{
    void Start()
    {
        // Saat scene dimulai, langsung aktifkan kursor
        ShowAndUnlockCursor();
    }

    // Fungsi ini dipanggil otomatis saat window game kembali fokus (setelah Alt+Tab)
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            // Paksakan kursor untuk muncul lagi
            ShowAndUnlockCursor();
        }
    }

    private void ShowAndUnlockCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}