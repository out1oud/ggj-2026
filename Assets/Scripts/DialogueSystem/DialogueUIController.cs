using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueSystem
{
    public sealed class DialogueUIController : MonoBehaviour
    {
        [Header("Panels")] [SerializeField] CanvasGroup textPanel;
        [SerializeField] TMP_Text textLabel;

        [SerializeField] CanvasGroup answersPanel;
        [SerializeField] Transform answersContainer;
        [SerializeField] Button answerButtonPrefab;

        Coroutine _fadeRoutine;

        void Awake()
        {
            HideTextInstant();
            HideAnswersInstant();
        }

        public void SetStyleSheet(TMP_StyleSheet styleSheet)
        {
            textLabel.styleSheet = styleSheet;
        }

        public void ShowText(string text)
        {
            StopFade();

            textLabel.SetText(text ?? "");
            SetVisible(textPanel, true);
        }

        public void HideTextInstant()
        {
            StopFade();
            SetVisible(textPanel, false);
        }

        public void FadeOutTextPanel(float duration, AnimationCurve curve, Action onComplete = null)
        {
            StopFade();
            _fadeRoutine = StartCoroutine(Fade(textPanel, 1f, 0f, duration, curve, deactivateOnEnd: true, onComplete));
        }

        public void ShowAnswers(List<DialogueAnswer> answers,
            Action<DialogueAnswer> onClick)
        {
            SetVisible(answersPanel, true);
            ClearAnswers();

            foreach (DialogueAnswer a in answers)
            {
                Button btn = Instantiate(answerButtonPrefab, answersContainer);
                var tmp = btn.GetComponentInChildren<TMP_Text>();
                if (tmp) tmp.SetText(a.text);

                btn.onClick.AddListener(() => onClick?.Invoke(a));
            }
        }

        public void HideAnswersInstant()
        {
            SetVisible(answersPanel, false);
            ClearAnswers();
        }

        public void HideAllInstant()
        {
            HideTextInstant();
            HideAnswersInstant();
        }

        void StopFade()
        {
            if (_fadeRoutine == null) return;
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        void ClearAnswers()
        {
            for (int i = answersContainer.childCount - 1; i >= 0; i--)
                Destroy(answersContainer.GetChild(i).gameObject);
        }

        static void SetVisible(CanvasGroup g, bool visible)
        {
            if (!g) return;

            g.gameObject.SetActive(visible);
            g.alpha = visible ? 1f : 0f;
            g.interactable = visible;
            g.blocksRaycasts = visible;
        }

        static IEnumerator Fade(CanvasGroup g, float from, float to, float duration, AnimationCurve curve,
            bool deactivateOnEnd, Action onComplete)
        {
            if (!g) yield break;

            g.gameObject.SetActive(true);
            g.interactable = true;
            g.blocksRaycasts = true;

            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);

                float k = curve?.Evaluate(p) ?? p;
                g.alpha = Mathf.LerpUnclamped(from, to, k);

                yield return null;
            }

            g.alpha = to;

            bool visible = to > 0.001f;
            g.interactable = visible;
            g.blocksRaycasts = visible;

            if (deactivateOnEnd && !visible)
                g.gameObject.SetActive(false);

            onComplete?.Invoke();
        }
    }
}