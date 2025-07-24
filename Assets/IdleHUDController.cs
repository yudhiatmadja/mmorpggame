using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(CanvasGroup))]
public class IdleHUDController : MonoBehaviour
{
    public string axisHorizontal = "Horizontal";
    public string axisVertical = "Vertical";
    public float idleThreshold = 0.01f;
    public float fadeDuration = 0.5f;
    public float idleDelay = 5f;  // 🕒 Tambahkan ini: Delay idle 5 detik

    private CanvasGroup canvasGroup;
    private float targetAlpha;
    private float fadeVelocity;

    private float idleTimer = 0f; // ⏱️ Timer untuk menghitung waktu idle

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        targetAlpha = 1f;
    }

    void Update()
    {
        float h = CrossPlatformInputManager.GetAxis(axisHorizontal);
        float v = CrossPlatformInputManager.GetAxis(axisVertical);

        bool isIdle = Mathf.Abs(h) < idleThreshold && Mathf.Abs(v) < idleThreshold;

        if (isIdle)
        {
            idleTimer += Time.deltaTime; // Tambah waktu idle
        }
        else
        {
            idleTimer = 0f;              // Reset jika bergerak
            targetAlpha = 0f;            // Langsung hilangkan HUD saat bergerak
        }

        // HUD hanya muncul jika idle lebih dari idleDelay (5 detik)
        if (idleTimer >= idleDelay)
        {
            targetAlpha = 1f;
        }

        // Lakukan fade
        canvasGroup.alpha = Mathf.SmoothDamp(canvasGroup.alpha, targetAlpha, ref fadeVelocity, fadeDuration);

        if (Mathf.Abs(canvasGroup.alpha - targetAlpha) < 0.01f)
        {
            canvasGroup.alpha = targetAlpha;
        }

        // Matikan interaksi saat tidak tampil
        canvasGroup.interactable = canvasGroup.blocksRaycasts = canvasGroup.alpha > 0.1f;
    }
}
