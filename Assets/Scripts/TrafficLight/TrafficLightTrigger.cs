using System;
using Round;
using UnityEngine;

namespace TrafficLight
{
    [RequireComponent(typeof(Collider))]
    public class TrafficLightTrigger : MonoBehaviour
    {
        [Header("Traffic Light Reference")]
        [SerializeField] TrafficLightController trafficLightController;

        [Header("Player Detection")]
        [SerializeField] string playerTag = "Player";

        [Header("Timer Settings")]
        [Tooltip("Time in seconds for the brake prompt timer")]
        [SerializeField] float brakeTimerDuration = 3f;

        [Header("Debug")]
        [SerializeField] bool showDebugLogs = true;

        bool _isTriggered;
        bool _isProcessing;

        void Start()
        {
            // Ensure the collider is set as a trigger
            var col = GetComponent<Collider>();
            if (col && !col.isTrigger)
            {
                col.isTrigger = true;
                Log("Collider was not set as trigger, automatically enabled isTrigger");
            }

            if (!trafficLightController)
            {
                // Try to find in parent
                trafficLightController = GetComponentInParent<TrafficLightController>();
                if (!trafficLightController)
                {
                    Debug.LogError($"[{nameof(TrafficLightTrigger)}] No TrafficLightController assigned or found in parent!");
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            Log($"OnTriggerEnter: {other.name}, tag: {other.tag}");
            
            if (_isTriggered || _isProcessing)
            {
                Log("Already triggered or processing, ignoring");
                return;
            }
            
            if (!other.CompareTag(playerTag))
            {
                Log($"Object tag '{other.tag}' doesn't match player tag '{playerTag}'");
                return;
            }
            
            if (!trafficLightController)
            {
                Log("No traffic light controller assigned!");
                return;
            }

            // Only trigger if the light is red or yellow
            if (trafficLightController.ShouldStop)
            {
                _isTriggered = true;
                _isProcessing = true;
                Log($"Player entered red/yellow light zone. Current state: {trafficLightController.CurrentState}");

                // Notify RoundController to handle the traffic light stop sequence
                if (RoundController.Instance != null)
                {
                    RoundController.Instance.HandleTrafficLightRed(trafficLightController, brakeTimerDuration, OnSequenceComplete);
                }
                else
                {
                    Log("RoundController.Instance is null!");
                }
            }
            else
            {
                Log($"Player passed through green light. Current state: {trafficLightController.CurrentState}");
            }
        }

        void OnSequenceComplete()
        {
            _isProcessing = false;
            Log("Traffic light sequence completed");
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            // Reset triggered state when player leaves (for potential re-entry)
            if (!_isProcessing)
            {
                _isTriggered = false;
                Log("Player exited trigger zone, reset for potential re-trigger");
            }
        }

        void Log(string message)
        {
            if (showDebugLogs)
                Debug.Log($"[{nameof(TrafficLightTrigger)}] {message}");
        }

        /// <summary>
        /// Manually reset the trigger state (useful for testing or special scenarios).
        /// </summary>
        public void ResetTrigger()
        {
            _isTriggered = false;
            _isProcessing = false;
        }
    }
}
