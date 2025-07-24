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
}
