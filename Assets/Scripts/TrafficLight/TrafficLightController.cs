using System;
using System.Collections;
using UnityEngine;

namespace TrafficLight
{
    public class TrafficLightController : MonoBehaviour
    {
        public GameObject redLight;
        public GameObject redLightBottom;
        public GameObject yellowLight;
        public GameObject yellowLightBottom;
        public GameObject greenLight;
        public GameObject greenLightBottom;
        public GameObject greenLightPedestrian;
        public GameObject redLightPedestrian;

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

            redLight.SetActive(newState == LightState.Red);
            redLightBottom.SetActive(newState == LightState.Red);
            greenLightPedestrian.SetActive(newState == LightState.Red);
            yellowLight.SetActive(newState == LightState.Yellow);
            yellowLightBottom.SetActive(newState == LightState.Yellow);
            greenLight.SetActive(newState == LightState.Green);
            greenLightBottom.SetActive(newState == LightState.Green);
            redLightPedestrian.SetActive(newState == LightState.Green);
        }
    }
}