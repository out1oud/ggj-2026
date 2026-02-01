using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficLight
{
    public class TrafficLightController : MonoBehaviour
    {
        public List<GameObject> redLights;
        public List<GameObject> yellowLights;
        public List<GameObject> greenLights;

        [Header("Timings (seconds)")]
        public float redTime = 5f;
        public float greenTime = 5f;
        public float yellowTime = 2f;

        enum LightState
        {
            Red,
            Yellow,
            Green
        }

        LightState _currentState;

        void Start()
        {
            SetState(LightState.Red);
            StartCoroutine(TrafficLoop());
        }

        IEnumerator TrafficLoop()
        {
            while (true)
            {
                switch (_currentState)
                {
                    case LightState.Red:
                        yield return new WaitForSeconds(redTime);
                        SetState(LightState.Green);
                        break;

                    case LightState.Green:
                        yield return new WaitForSeconds(greenTime);
                        SetState(LightState.Yellow);
                        break;

                    case LightState.Yellow:
                        yield return new WaitForSeconds(yellowTime);
                        SetState(LightState.Red);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
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