using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private AIBookController aiBookController;
    [SerializeField] private GameObject ChatWorld;
    // Anda bisa menambahkan referensi UI lain di sini

    void Update()
    {
        // 1. Logika untuk mengaktifkan UI Mode dengan LeftAlt
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            UIModeController.instance.ActivateUIMode();
        }
        else if (Input.GetKeyUp(KeyCode.LeftAlt))
        {
            // Hanya nonaktifkan jika tidak ada UI lain yang terbuka
            if (!aiBookController.gameObject.activeSelf && !ChatWorld.activeSelf)
            {
                UIModeController.instance.DeactivateUIMode();
            }
        }

        // 2. Logika untuk membuka/menutup Buku AI
        if (Input.GetKeyDown(KeyCode.J))
        {
            // Toggle buku
            bool isBookOpening = !aiBookController.gameObject.activeSelf;
            aiBookController.gameObject.SetActive(isBookOpening);

            if (isBookOpening)
            {
                UIModeController.instance.ActivateUIMode(); // Minta aktifkan UI Mode
            }
            else
            {
                UIModeController.instance.DeactivateUIMode(); // Minta nonaktifkan UI Mode
            }
        }

        // 3. Logika untuk membuka/menutup Chat World
        if (Input.GetKeyDown(KeyCode.Return))
        {
            bool isChatOpening = !ChatWorld.activeSelf;
            ChatWorld.SetActive(isChatOpening);

            if (isChatOpening)
            {
                UIModeController.instance.ActivateUIMode();
            }
            else
            {
                // Hanya nonaktifkan jika tidak ada UI lain yang terbuka
                if (!aiBookController.gameObject.activeSelf)
                {
                    UIModeController.instance.DeactivateUIMode();
                }
            }
        }
    }
}