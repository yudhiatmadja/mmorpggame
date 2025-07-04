using Photon.Pun;
using UnityEngine;

public class CameraControllerFix : MonoBehaviourPun
{
    public GameObject cameraRoot;

    void Start()
    {
        if (!photonView.IsMine)
        {
            cameraRoot.SetActive(false); // Nonaktifkan kamera player lain
        }
    }

    public void DisablePlayerCamera()
    {
        // Kita tetap cek IsMine untuk memastikan hanya kamera milik kita yang dikontrol
        if (photonView.IsMine)
        {
            cameraRoot.SetActive(false);
        }
    }
    public void EnablePlayerCamera()
    {
        if (photonView.IsMine)
        {
            cameraRoot.SetActive(true);
        }
    }
}
