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

        [Header("Initialization")]
        [Tooltip("Randomize the starting state of the traffic light")]
        public bool randomizeInitialState = true;
        
        [Tooltip("Randomize how far into the current phase the light starts (0-1)")]
        public bool randomizePhaseOffset = true;

        LightState _currentState;
        bool _isPaused;
        float _initialPhaseOffset;
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
            // Randomize initial state
            LightState initialState = LightState.Red;
            if (randomizeInitialState)
            {
                var states = (LightState[])Enum.GetValues(typeof(LightState));
                initialState = states[UnityEngine.Random.Range(0, states.Length)];
            }
            
            // Randomize phase offset (0-1 means how far into current phase)
            _initialPhaseOffset = randomizePhaseOffset ? UnityEngine.Random.value : 0f;
            
            SetState(initialState);
            _trafficLoopCoroutine = StartCoroutine(TrafficLoop());
        }

        IEnumerator TrafficLoop()
        {
            // Apply initial phase offset on first iteration
            bool firstIteration = true;
            
            while (true)
            {
                // Wait while paused
                while (_isPaused)
                    yield return null;

                switch (_currentState)
                {
                    case LightState.Red:
                        float redWait = firstIteration ? redTime * (1f - _initialPhaseOffset) : redTime;
                        yield return new WaitForSeconds(redWait);
                        if (!_isPaused) SetState(LightState.Green);
                        break;

                    case LightState.Green:
                        float greenWait = firstIteration ? greenTime * (1f - _initialPhaseOffset) : greenTime;
                        yield return new WaitForSeconds(greenWait);
                        if (!_isPaused) SetState(LightState.Yellow);
                        break;

                    case LightState.Yellow:
                        float yellowWait = firstIteration ? yellowTime * (1f - _initialPhaseOffset) : yellowTime;
                        yield return new WaitForSeconds(yellowWait);
                        if (!_isPaused) SetState(LightState.Red);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                firstIteration = false;
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