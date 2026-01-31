using System.Collections;
using UnityEngine;
using Utilities;

namespace DialogueSystem
{
    public sealed class DialoguePresenter : Singleton<DialoguePresenter>
    {
        [Header("UI")] [SerializeField] DialogueUIController ui;

        [Header("Text auto-hide (only text)")] [SerializeField]
        float textHideDuration = 0.6f;

        [SerializeField] AnimationCurve textHideCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Pause (node/answer shouldPause)")] [SerializeField]
        float pauseMin = 0.3f;

        [SerializeField] float pauseMax = 0.8f;
        [SerializeField] bool useUnscaledTimeForPause = true;

        DialogueRunner _runner;
        Coroutine _pauseRoutine;
        bool _isRunning;

        public void StartDialogue(CharacterDialogue dialogue, string startNodeId = null)
        {
            StopDialogue();

            if (!dialogue)
            {
                Debug.LogWarning("DialoguePresenter: dialogue is null");
                return;
            }
            
            ui.SetStyleSheet(dialogue.styleSheet);

            _runner = new()
            {
                OnAction = OnAction,
                OnNode = OnNode,
                OnAnswers = _ => { },
                OnEnd = OnEnd
            };

            _isRunning = true;

            _runner.Start(dialogue, startNodeId);
        }

        public void StopDialogue()
        {
            _isRunning = false;

            CancelPause();
            ui.HideAllInstant();

            _runner = null;
        }
        
        void OnAction(DialogueActionType type, float value)
        {
            // TODO: play animations / FX
            // animator.SetTrigger(type.ToString());
        }

        void OnNode(DialogueNode node)
        {
            if (!_isRunning)
                return;

            CancelPause();
            RenderNode(node);
        }

        void OnEnd()
        {
            StopDialogue();
        }

        void RenderNode(DialogueNode node)
        {
            bool hasText = !string.IsNullOrWhiteSpace(node.text);
            bool hasAnswers = node.HasAnswers;

            switch (hasText)
            {
                case false when hasAnswers:
                    ui.HideTextInstant();
                    ui.ShowAnswers(node.answers, OnAnswerClicked);
                    return;
                case true when hasAnswers:
                    ui.ShowText(node.text);
                    ui.ShowAnswers(node.answers, OnAnswerClicked);
                    return;
                case true:
                    ui.ShowText(node.text);
                    ui.HideAnswersInstant();

                    ui.FadeOutTextPanel(textHideDuration, textHideCurve, () => { StartPauseThenGoTo(node.targetId, !node.shouldPause); });

                    return;
                default:
                    ui.HideAllInstant();
                    StartPauseThenGoTo(node.targetId, !node.shouldPause);
                    break;
            }
        }

        void OnAnswerClicked(DialogueAnswer answer)
        {
            CancelPause();

            // По требованию: при выборе варианта скрывать текст и ответы
            ui.HideAllInstant();

            if (answer == null)
            {
                _runner.GoTo(null);
                return;
            }

            if (answer.shouldPause)
            {
                StartPauseThenGoTo(answer.targetId);
                return;
            }

            _runner.GoTo(answer.targetId);
        }

        void StartPauseThenGoTo(string targetId, bool force = false)
        {
            CancelPause();

            if (force)
            {
                _runner.GoTo(targetId);
                return;
            }
            
            _pauseRoutine = StartCoroutine(PauseThenGoTo(targetId));
        }

        void CancelPause()
        {
            if (_pauseRoutine != null)
            {
                StopCoroutine(_pauseRoutine);
                _pauseRoutine = null;
            }
        }

        IEnumerator PauseThenGoTo(string targetId)
        {
            float min = Mathf.Max(0f, pauseMin);
            float max = Mathf.Max(min, pauseMax);
            float wait = Random.Range(min, max);

            var t = 0f;
            while (t < wait)
            {
                t += useUnscaledTimeForPause ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            _pauseRoutine = null;
            _runner.GoTo(targetId);
        }
    }
}