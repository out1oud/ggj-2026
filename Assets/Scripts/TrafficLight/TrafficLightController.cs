using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficLight
{
    public enum LightState
    {
        Red,
        Yellow,
        Green
    }

    public class TrafficLightController : MonoBehaviour
    {
        public List<GameObject> redLights;
        public List<GameObject> yellowLights;
        public List<GameObject> greenLights;

        [Header("Timings (seconds)")]
        public float redTime = 5f;
        public float greenTime = 5f;
        public float yellowTime = 2f;

        LightState _currentState;
        bool _isPaused;
        Coroutine _trafficLoopCoroutine;

        /// <summary>
        /// Current state of the traffic light.
        /// </summary>
        public LightState CurrentState => _currentState;

        /// <summary>
        /// Whether the traffic light cycle is currently paused.
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Returns true if the light is red or yellow (should stop).
        /// </summary>
        public bool ShouldStop => _currentState == LightState.Red || _currentState == LightState.Yellow;

        void Start()
        {
            SetState(LightState.Red);
            _trafficLoopCoroutine = StartCoroutine(TrafficLoop());
        }

        IEnumerator TrafficLoop()
        {
            while (true)
            {
                // Wait while paused
                while (_isPaused)
                    yield return null;

                switch (_currentState)
                {
                    case LightState.Red:
                        yield return new WaitForSeconds(redTime);
                        if (!_isPaused) SetState(LightState.Green);
                        break;

                    case LightState.Green:
                        yield return new WaitForSeconds(greenTime);
                        if (!_isPaused) SetState(LightState.Yellow);
                        break;

                    case LightState.Yellow:
                        yield return new WaitForSeconds(yellowTime);
                        if (!_isPaused) SetState(LightState.Red);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Pauses the traffic light cycle at its current state.
        /// </summary>
        public void PauseCycle()
        {
            _isPaused = true;
        }

        /// <summary>
        /// Resumes the traffic light cycle.
        /// </summary>
        public void ResumeCycle()
        {
            _isPaused = false;
        }

        /// <summary>
        /// Forces the light to switch to a specific state.
        /// </summary>
        public void ForceState(LightState state)
        {
            SetState(state);
        }

        /// <summary>
        /// Forces the light to green and optionally resumes the cycle.
        /// </summary>
        public void ForceGreen(bool resumeCycle = true)
        {
            SetState(LightState.Green);
            if (resumeCycle)
                _isPaused = false;
        }

        void SetState(LightState newState)
        {
            _currentState = newState;
            
            redLights.ForEach(x => x.SetActive(newState == LightState.Red));
            yellowLights.ForEach(x => x.SetActive(newState == LightState.Yellow));
            greenLights.ForEach(x => x.SetActive(newState == LightState.Green));
        }
    }
}