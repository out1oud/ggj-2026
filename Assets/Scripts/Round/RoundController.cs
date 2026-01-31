using System.Collections;
using Character;
using DialogueSystem;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;

namespace Round
{
    public class RoundController : Singleton<RoundController>
    {
        enum State
        {
            WaitingForEngineStart,
            WaitingInitialTripDelay,
            RequestStopAtPickup,
            WaitingStoppedAtPickup,
            PassengerEntering,
            PauseBeforeNodes,
            RunningDialogueNodes,
            PromptStopForDropoff,
            WaitingStopForDropoff,
            PassengerExiting,
            BetweenDialoguesDelay,
            FinishedDelay,
            TransitionToResults
        }

        [Header("Sequence")] [SerializeField] RoundSequence roundSequence;

        [Header("Delays (seconds)")] [SerializeField]
        Vector2 delayBeforeTripLoopRange = new(2f, 5f);

        [SerializeField] Vector2 delayBetweenDialoguesRange = new(2f, 5f);
        [SerializeField] Vector2 delayBeforeResultsRange = new(2f, 5f);
        [SerializeField] Vector2 pauseBeforeNodesRange = new(2f, 5f);

        [Header("Pickup / Dropoff")] [SerializeField]
        Vector2 passengerEnterAnimWaitRange = new(0.6f, 1.2f);

        [SerializeField] Vector2 passengerExitAnimWaitRange = new(0.6f, 1.2f);

        [Header("Input")] [SerializeField] KeyCode driveKey = KeyCode.W;
        [SerializeField] KeyCode stopKey = KeyCode.S;

        [Header("Dependencies")] [SerializeField]
        MonoBehaviour taxiDriveBehaviour;

        [SerializeField] MonoBehaviour characterServiceBehaviour; // ICharacterService
        [SerializeField] MonoBehaviour dialogueRunnerBehaviour; // IDialogueRunner
        [SerializeField] MovementController movementController;
        [SerializeField] CharacterActor characterActor;

        [Header("UI Hooks (optional)")] [SerializeField]
        bool showDebugLogs = true;

        [Header("Results")] [SerializeField] string resultsSceneName = "Results"; // или вызов твоего UI

        State _state = State.WaitingForEngineStart;
        int _dialogueIndex;

        Coroutine _flow;

        void Start()
        {
            _flow = StartCoroutine(Flow());
        }

        public void StartMove()
        {
            if (_state == State.WaitingForEngineStart) movementController.StartMove();
        }

        public void StopMove()
        {
            if (_state == State.WaitingStopForDropoff) movementController.StopMoveSmooth();
        }

        IEnumerator Flow()
        {
            while (_state == State.WaitingForEngineStart)
                yield return null;

            yield return WaitRandom(delayBeforeTripLoopRange);
            _state = State.BetweenDialoguesDelay;

            while (roundSequence && _dialogueIndex < roundSequence.dialogues.Count)
            {
                CharacterDialogue cd = roundSequence.dialogues[_dialogueIndex];
                Log($"Start CharacterDialogue #{_dialogueIndex}: characterId={cd.characterId}");

                _state = State.RequestStopAtPickup;
                movementController.StopMoveSmooth();
                _state = State.WaitingStoppedAtPickup;

                while (!movementController) yield return null;

                _state = State.PassengerEntering;
                characterActor.SetCharacter(cd.characterId);
                characterActor.Enter();
                yield return WaitRandom(passengerEnterAnimWaitRange);

                _state = State.PauseBeforeNodes;
                yield return WaitRandom(pauseBeforeNodesRange);

                _state = State.RunningDialogueNodes;
                
                // yield return DialoguePresenter.Instance.StartDialogue(cd);;

                _state = State.PromptStopForDropoff;

                while (_state == State.PromptStopForDropoff)
                    yield return null;

                while (_state == State.WaitingStopForDropoff && movementController.IsMoving)
                    yield return null;

                characterActor.Exit();
                yield return WaitRandom(passengerExitAnimWaitRange);

                _state = State.BetweenDialoguesDelay;
                yield return WaitRandom(delayBetweenDialoguesRange);

                _dialogueIndex++;
            }

            _state = State.FinishedDelay;
            yield return WaitRandom(delayBeforeResultsRange);

            _state = State.TransitionToResults;
            Log("Sequence finished -> Results");
            if (!string.IsNullOrWhiteSpace(resultsSceneName))
            {
                SceneManager.LoadScene(resultsSceneName);
            }
        }

        IEnumerator WaitRandom(Vector2 range)
        {
            float t = UnityEngine.Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
            if (t > 0f) yield return new WaitForSeconds(t);
        }

        void Log(string msg)
        {
            if (showDebugLogs) Debug.Log($"[{nameof(RoundController)}] {_state}: {msg}");
        }
    }
}