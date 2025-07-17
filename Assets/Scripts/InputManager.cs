using UnityEngine;
using TMPro;
using System.Linq;

public class InputManager : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private AIBookController aiBookController;
    [SerializeField] private GameObject ChatWorld;

    [Header("Input Detection")]
    [SerializeField] private bool debugMode = false; // Untuk debugging

    // Status apakah sedang mengetik
    private bool isTyping = false;

    void Update()
    {
        // Deteksi apakah player sedang mengetik
        CheckIfTyping();

        // Hanya proses input keyboard jika tidak sedang mengetik
        if (!isTyping)
        {
            ProcessKeyboardInput();
        }

        // Debug info (optional)
        if (debugMode)
        {
            DebugTypingStatus();
        }
    }

    /// <summary>
    /// Metode untuk mendeteksi apakah player sedang mengetik di InputField manapun
    /// </summary>
    private void CheckIfTyping()
    {
        // Metode 1: Cek apakah ada TMP_InputField yang sedang fokus
        TMP_InputField[] allInputFields = FindObjectsOfType<TMP_InputField>();

        isTyping = allInputFields.Any(field => field.isFocused);

        // Metode 2: Alternatif - cek dengan UnityEngine.EventSystems
        // UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        // if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
        // {
        //     TMP_InputField inputField = eventSystem.currentSelectedGameObject.GetComponent<TMP_InputField>();
        //     isTyping = inputField != null && inputField.isFocused;
        // }
    }

    /// <summary>
    /// Proses input keyboard hanya jika tidak sedang mengetik
    /// </summary>
    private void ProcessKeyboardInput()
    {
        // Input untuk AI Book
        if (Input.GetKeyDown(KeyCode.J))
        {
            aiBookController.OpenJournal();
        }

        // Input untuk Chat World
        if (Input.GetKeyDown(KeyCode.Return))
        {
            bool isOpening = !ChatWorld.activeSelf;
            ChatWorld.SetActive(isOpening);
        }

        // Tambahkan input keyboard lainnya di sini
        // if (Input.GetKeyDown(KeyCode.Escape))
        // {
        //     // Handle escape key
        // }
    }

    /// <summary>
    /// Debug info untuk melihat status typing
    /// </summary>
    private void DebugTypingStatus()
    {
        if (isTyping)
        {
            Debug.Log("Player is typing - Keyboard input disabled");
        }
    }

    /// <summary>
    /// Public method untuk cek status typing dari script lain
    /// </summary>
    public bool IsPlayerTyping()
    {
        return isTyping;
    }
}