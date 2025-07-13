using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    public class StarterAssetsInputs : MonoBehaviour
    {
        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;

        [Header("Movement Settings")]
        public bool analogMovement;

        // Saklar utama untuk mengontrol semua input
        private bool _controlsEnabled = true;

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputValue value)
        {
            // Abaikan input jika kontrol dimatikan
            if (!_controlsEnabled) return;
            MoveInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            // Abaikan input jika kontrol dimatikan
            if (!_controlsEnabled) return;
            LookInput(value.Get<Vector2>());
        }

        public void OnJump(InputValue value)
        {
            // Abaikan input jika kontrol dimatikan
            if (!_controlsEnabled) return;
            JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            // Abaikan input jika kontrol dimatikan
            if (!_controlsEnabled) return;
            SprintInput(value.isPressed);
        }
#endif

        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
        }

        public void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
        }

        // === FUNGSI YANG HILANG & MENYEBABKAN ERROR ADA DI SINI ===
        /// <summary>
        /// Fungsi publik yang dipanggil oleh UIModeController untuk menghidupkan/mematikan kontrol.
        /// </summary>
        public void SetControlsEnabled(bool isEnabled)
        {
            _controlsEnabled = isEnabled;

            // Jika kontrol dinonaktifkan, langsung reset semua nilai saat ini untuk menghentikan gerakan.
            if (!isEnabled)
            {
                MoveInput(Vector2.zero);
                LookInput(Vector2.zero);
                JumpInput(false);
                SprintInput(false);
            }
        }
    }
}