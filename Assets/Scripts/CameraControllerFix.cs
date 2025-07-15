using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class CameraControllerFix : MonoBehaviourPun
{
    void Start()
    {
        if (photonView.IsMine)
        {
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (UIModeController.instance != null && playerInput != null)
            {
                UIModeController.instance.RegisterPlayerInput(playerInput);
            }
        }
        else
        {
            // Nonaktifkan semua komponen yang tidak perlu pada player lain
            var controller = GetComponent<ThirdPersonController>();
            var inputs = GetComponent<StarterAssetsInputs>();
            var playerInput = GetComponent<PlayerInput>();

            if (controller != null) controller.enabled = false;
            if (inputs != null) inputs.enabled = false;
            if (playerInput != null) playerInput.enabled = false;
        }
    }
}