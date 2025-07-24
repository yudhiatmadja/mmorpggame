using UnityEngine;
using UnityEngine.InputSystem; // WAJIB TAMBAHKAN INI

public class UIModeController : MonoBehaviour
{
    public static UIModeController instance;
    private PlayerInput _playerInput; // Referensi ke komponen "sekring" input player
    private int _uiModeRequestCount = 0;

    public bool IsUIModeActive => _uiModeRequestCount > 0;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void RegisterPlayerInput(PlayerInput playerInput)
    {
        _playerInput = playerInput;
        UpdateControlAndCursorState(); // Langsung sinkronisasi saat pendaftaran
    }

    public void RequestUIMode()
    {
        _uiModeRequestCount++;
        if (_uiModeRequestCount == 1)
        {
            UpdateControlAndCursorState();
        }
    }

    public void ReleaseUIMode()
    {
        _uiModeRequestCount--;
        if (_uiModeRequestCount < 0) _uiModeRequestCount = 0;

        if (_uiModeRequestCount == 0)
        {
            UpdateControlAndCursorState();
        }
    }

    public void UpdateControlAndCursorState()
    {
        if (_playerInput == null) return;

        // PERINTAH UTAMA: Matikan atau hidupkan seluruh komponen PlayerInput
        _playerInput.enabled = !IsUIModeActive;

        Cursor.visible = IsUIModeActive;
        Cursor.lockState = IsUIModeActive ? CursorLockMode.None : CursorLockMode.Locked;

        // Safety net untuk me-reset input saat kontrol kembali aktif
        if (!IsUIModeActive)
        {
            StarterAssets.StarterAssetsInputs inputs = _playerInput.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (inputs != null)
            {
                inputs.MoveInput(Vector2.zero);
                inputs.LookInput(Vector2.zero);
            }
        }
    }
}