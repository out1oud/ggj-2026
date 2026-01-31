using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovementController : MonoBehaviour
    {
        [Header("Speed")] [SerializeField] float maxSpeed = 6f;
        [SerializeField] float acceleration = 18f;
        [SerializeField] float deceleration = 24f;

        [Header("Stop")] [SerializeField] float stopEpsilon = 0.02f;

        Rigidbody _rb;

        float _targetSpeed;
        bool _isStopping = true;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            Vector3 v = _rb.linearVelocity;

            float rate = _isStopping ? deceleration : acceleration;
            float newX = Mathf.MoveTowards(v.x, _targetSpeed, rate * Time.fixedDeltaTime);

            if (_isStopping && Mathf.Abs(newX) <= stopEpsilon)
            {
                newX = 0f;
                _targetSpeed = 0f;
            }

            _rb.linearVelocity = new(newX, 0, 0);
        }

        public void StartMove(float direction)
        {
            if (direction == 0f) return;

            direction = Mathf.Sign(direction);
            _targetSpeed = direction * maxSpeed;
            _isStopping = false;
        }

        public void StopMoveSmooth()
        {
            _targetSpeed = 0f;
            _isStopping = true;
        }

        public void StopMoveInstant()
        {
            Vector3 v = _rb.linearVelocity;
            _rb.linearVelocity = new(0f, 0f, 0f);
            _targetSpeed = 0f;
            _isStopping = true;
        }

        public float CurrentSpeed => _rb.linearVelocity.x;
    }
}