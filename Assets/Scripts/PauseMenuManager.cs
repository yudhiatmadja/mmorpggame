using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    public GameObject mainMenuUI;
    private bool isPaused = false;

    private CameraControllerFix cameraController;

    void Start()
    {
        // Cari komponen CameraControllerFix dari player lokal (PhotonView.IsMine)
        CameraControllerFix[] allControllers = FindObjectsOfType<CameraControllerFix>();
        foreach (var controller in allControllers)
        {
            if (controller.photonView != null && controller.photonView.IsMine)
            {
                cameraController = controller;
                break;
            }
        }
    }

    public void OpenMainMenu()
    {
        mainMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        if (cameraController != null)
            cameraController.DisablePlayerCamera();
    }

    public void CloseMainMenu()
    {
        mainMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        if (cameraController != null)
            cameraController.EnablePlayerCamera();
    }

    public void GoToMainMenuScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
