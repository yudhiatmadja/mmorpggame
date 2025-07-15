using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private AIBookController aiBookController;
    [SerializeField] private GameObject ChatWorld;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            aiBookController.ToggleBook();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            bool isOpening = !ChatWorld.activeSelf;
            ChatWorld.SetActive(isOpening);

            if (isOpening)
            {
                UIModeController.instance.RequestUIMode();
            }
            else
            {
                UIModeController.instance.ReleaseUIMode();
            }
        }
    }
}