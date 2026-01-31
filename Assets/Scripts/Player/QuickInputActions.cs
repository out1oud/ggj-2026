using Round;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;

namespace Player
{
    [RequireComponent(typeof(PlayerInput))]
    public class QuickInputActions : MonoBehaviour
    {
        void OnAccelerate(InputValue value)
        {
            Debug.Log("Accelerate");
            if (!value.isPressed) return;
            RoundController.Instance.StartMove();
        }

        void OnHalt(InputValue value)
        {
            Debug.Log("Stop");
            if (!value.isPressed) return;
            RoundController.Instance.StopMove();
        }
    }
}