using UnityEngine;

public class InputManager : MonoBehaviour
{
    // Referensi ke controller buku kita
    [SerializeField] private AIBookController aiBookController;
    [SerializeField] private GameObject ChatWorld;

    void Update()
    {
        // Cek jika tombol J ditekan
        if (Input.GetKeyDown(KeyCode.J))
        {
            // Pastikan referensi controllernya ada
            if (aiBookController != null)
            {
                // HANYA panggil ToggleBook jika GameObject-nya sedang TIDAK aktif
                if (!aiBookController.gameObject.activeSelf)
                {
                    aiBookController.ToggleBook();
                }
            }
            else
            {
                Debug.LogWarning("Referensi AIBookController belum diatur di InputManager!");
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (ChatWorld != null)
            {
                if (ChatWorld.gameObject.activeSelf)
                {
                    ChatWorld.SetActive(false);
                }
                else
                { 
                ChatWorld.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning("Referensi ChatWorld belum diatur di InputManager!");
            }
        }
    }
}