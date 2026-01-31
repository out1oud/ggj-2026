using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;

namespace Player
{
    [RequireComponent(typeof(PlayerInput))]
    public class QuickInputActions : MonoBehaviour
    {
        [SerializeField] float forwardDirection = 1f;
        [SerializeField] MovementController movementController;
        
        void OnAccelerate(InputValue value)
        {
            Debug.Log("Accelerate");
            if (!value.isPressed) return;
            movementController.StartMove(forwardDirection);
        }

        void OnHalt(InputValue value)
        {
            Debug.Log("Stop");
            if (!value.isPressed) return;
            movementController.StopMoveSmooth();
        }
    }
}