using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace Round
{
    public class QteController : MonoBehaviour
    {
        [Serializable]
        public class PromptView
        {
            public GameObject root;
            public Image timerFill;
            public Animator animator;

            [Header("Animator")]
            public string showTrigger = "Show";
            public string hideTrigger = "Hide";
            public string hideStateName = "Hide";
        }

        [Header("Views")]
        [SerializeField] PromptView forwardPrompt;
        [SerializeField] PromptView breakPrompt;

        [Header("Defaults")]
        [SerializeField] bool hideOnStart = true;

        Coroutine _breakTimerCo;
        Coroutine _hideForwardCo;
        Coroutine _hideBreakCo;

        void Awake()
        {
            if (hideOnStart)
            {
                ForceHide(forwardPrompt);
                ForceHide(breakPrompt);
            }

            InitDefaults();
        }

        void InitDefaults()
        {
            if (forwardPrompt.timerFill) forwardPrompt.timerFill.fillAmount = 0f;
            if (breakPrompt.timerFill) breakPrompt.timerFill.fillAmount = 0f;
        }

        public void ShowForward()
        {
            CancelHide(ref _hideForwardCo);
            CancelBreakTimer();

            Show(forwardPrompt);
        }

        public void HideForward()
        {
            CancelHide(ref _hideForwardCo);
            _hideForwardCo = StartCoroutine(HideRoutine(forwardPrompt));
        }

        public void ShowBreak()
        {
            CancelHide(ref _hideBreakCo);
            CancelBreakTimer();

            Show(breakPrompt);
            ResetFill(breakPrompt);
        }

        public void ShowBreakWithTimer(float seconds, bool autoHide = true, Action onTimerEnd = null)
        {
            CancelHide(ref _hideBreakCo);
            CancelBreakTimer();

            Show(breakPrompt);
            ResetFill(breakPrompt);

            if (seconds > 0f)
                _breakTimerCo = StartCoroutine(BreakTimerRoutine(seconds, autoHide, onTimerEnd));
            else
                onTimerEnd?.Invoke();
        }

        public void HideBreak()
        {
            CancelBreakTimer();
            CancelHide(ref _hideBreakCo);
            _hideBreakCo = StartCoroutine(HideRoutine(breakPrompt));
        }

        public void ResolveBreak(bool hide = true)
        {
            CancelBreakTimer();

            if (hide)
                HideBreak();
            else
                ResetFill(breakPrompt);
        }

        IEnumerator BreakTimerRoutine(float seconds, bool autoHide, Action onTimerEnd)
        {
            var t = 0f;

            while (t < seconds)
            {
                t += Time.deltaTime;
                float normalized = Mathf.Clamp01(t / seconds);
                SetFill01(breakPrompt, 1f - normalized);
                yield return null;
            }

            SetFill01(breakPrompt, 0f);
            _breakTimerCo = null;

            onTimerEnd?.Invoke();

            if (autoHide)
                HideBreak();
        }

        void CancelBreakTimer()
        {
            if (_breakTimerCo == null) return;
            StopCoroutine(_breakTimerCo);
            _breakTimerCo = null;
        }

        static void Show(PromptView prompt)
        {
            if (!prompt.root) return;

            prompt.root.SetActive(true);

            if (prompt.animator)
                prompt.animator.SetTrigger(prompt.showTrigger);
        }

        static IEnumerator HideRoutine(PromptView prompt)
        {
            if (!prompt.root) yield break;

            if (prompt.animator)
            {
                prompt.animator.SetTrigger(prompt.hideTrigger);

                yield return WaitForAnimation(prompt.animator, prompt.hideStateName);
            }

            prompt.root.SetActive(false);
        }

        static IEnumerator WaitForAnimation(Animator animator, string stateName)
        {
            if (!animator) yield break;

            while (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
                yield return null;

            while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
                yield return null;
        }

        static void ForceHide(PromptView prompt)
        {
            if (prompt.root)
                prompt.root.SetActive(false);
        }

        static void CancelHide(ref Coroutine routine)
        {
            Coroutine coroutine = routine;
            if (coroutine == null) return;
            routine = null;
        }

        static void ResetFill(PromptView prompt)
        {
            if (prompt.timerFill)
                prompt.timerFill.fillAmount = 0f;
        }

        static void SetFill01(PromptView prompt, float value01)
        {
            if (prompt.timerFill)
                prompt.timerFill.fillAmount = Mathf.Clamp01(value01);
        }
    }
}
