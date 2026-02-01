using System;
using DialogueSystem;
using UnityEngine;
using Utilities;

namespace Player
{
    public class GameplayController : Singleton<GameplayController>
    {
        [Header("Traffic Light Stats")]
        [SerializeField] int trafficLightSuccesses;
        [SerializeField] int trafficLightFailures;

        public int TrafficLightSuccesses => trafficLightSuccesses;

        public int TrafficLightFailures => trafficLightFailures;

        public int TotalTrafficLightInteractions => trafficLightSuccesses + trafficLightFailures;

        public event Action<int, int> OnTrafficLightStatsChanged;

        public void RecordTrafficLightSuccess()
        {
            trafficLightSuccesses++;
            Debug.Log($"[GameplayController] Traffic light SUCCESS. Total: {trafficLightSuccesses} successes, {trafficLightFailures} failures");
            OnTrafficLightStatsChanged?.Invoke(trafficLightSuccesses, trafficLightFailures);
        }

        public void RecordTrafficLightFailure()
        {
            trafficLightFailures++;
            Debug.Log($"[GameplayController] Traffic light FAILURE. Total: {trafficLightSuccesses} successes, {trafficLightFailures} failures");
            OnTrafficLightStatsChanged?.Invoke(trafficLightSuccesses, trafficLightFailures);
        }

        public void ResetTrafficLightStats()
        {
            trafficLightSuccesses = 0;
            trafficLightFailures = 0;
            OnTrafficLightStatsChanged?.Invoke(trafficLightSuccesses, trafficLightFailures);
        }
    }
}