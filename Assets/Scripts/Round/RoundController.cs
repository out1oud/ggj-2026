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
            DoorOpeningForEntry,
            PassengerEntering,
            DoorClosingAfterEntry,
            WaitingForStartAfterEntry,
            PauseBeforeNodes,
            RunningDialogueNodes,
            PromptStopForDropoff,
            WaitingStopForDropoff,
            DoorOpeningForExit,
            PassengerExiting,
            DoorClosingAfterExit,
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

        [Header("Door Timing")] [SerializeField]
        Vector2 doorOpenDelayRange = new(0.3f, 0.6f);

        [SerializeField] Vector2 doorCloseDelayRange = new(0.3f, 0.5f);
        [SerializeField] Vector2 afterDoorOpenDelayRange = new(0.5f, 1.0f);
        [SerializeField] Vector2 afterDoorCloseDelayRange = new(0.3f, 0.6f);

        [Header("Door Audio")] [SerializeField]
        AudioClip doorOpenSound;

        [SerializeField] AudioClip doorCloseSound;
        [SerializeField] AudioSource audioSource;

        [Header("Engine Audio")] [SerializeField]
        AudioClip engineStartSound;

        [SerializeField] AudioClip engineStopSound;
        [SerializeField] AudioClip engineLoopSound;
        [SerializeField] AudioSource engineAudioSource;
        [SerializeField] [Range(0f, 1f)] float engineLoopVolume = 0.5f;

        [Header("Dependencies")] [SerializeField]
        MovementController movementController;

        [SerializeField] CharacterActor characterActor;
        [SerializeField] QteController qte;
        [SerializeField] DialoguePresenter dialoguePresenter;

        [Header("UI Hooks (optional)")] [SerializeField]
        bool showDebugLogs = true;

        [Header("Results")] [SerializeField] string resultsSceneName = "Results";

        State _state = State.WaitingForEngineStart;
        int _dialogueIndex;

        Coroutine _flow;

        void Start()
        {
            qte.ShowForward();
            _flow = StartCoroutine(Flow());
        }

        public void StartMove()
        {
            switch (_state)
            {
                case State.WaitingForEngineStart:
                    movementController.StartMove();
                    qte.HideForward();
                    StartEngineSound();
                    _state = State.WaitingInitialTripDelay;
                    break;
                case State.WaitingForStartAfterEntry:
                    movementController.StartMove();
                    qte.HideForward();
                    StartEngineSound();
                    _state = State.PauseBeforeNodes;
                    break;
            }
        }

        public void StopMove()
        {
            if (_state == State.PromptStopForDropoff)
            {
                movementController.StopMoveSmooth();
                _state = State.WaitingStopForDropoff;
            }
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

                while (movementController.IsMoving) yield return null;

                StopEngineSound();

                // Door opens for passenger entry
                _state = State.DoorOpeningForEntry;
                PlayDoorSound(doorOpenSound);
                yield return WaitRandom(doorOpenDelayRange);
                yield return WaitRandom(afterDoorOpenDelayRange);

                _state = State.PassengerEntering;
                characterActor.SetCharacter(cd.characterId);
                characterActor.Enter();
                yield return WaitRandom(passengerEnterAnimWaitRange);

                // Door closes after passenger entered
                _state = State.DoorClosingAfterEntry;
                PlayDoorSound(doorCloseSound);
                yield return WaitRandom(doorCloseDelayRange);
                yield return WaitRandom(afterDoorCloseDelayRange);

                // Wait for player to start moving
                _state = State.WaitingForStartAfterEntry;
                qte.ShowForward();
                Log("Waiting for player to start moving after passenger entered");

                while (_state == State.WaitingForStartAfterEntry)
                    yield return null;

                // Engine started in StartMove(), now pause before dialogue
                yield return WaitRandom(pauseBeforeNodesRange);

                _state = State.RunningDialogueNodes;
                Log($"Starting dialogue for character: {cd.characterId}");

                if (DialoguePresenter.Instance && cd)
                {
                    bool dialogueFinished = false;
                    void OnDialogueEnd() => dialogueFinished = true;

                    DialoguePresenter.Instance.OnDialogueEnded += OnDialogueEnd;
                    DialoguePresenter.Instance.StartDialogue(cd);
                    Log("Dialogue started, waiting for completion...");

                    while (!dialogueFinished)
                        yield return null;

                    DialoguePresenter.Instance.OnDialogueEnded -= OnDialogueEnd;
                    Log("Dialogue finished");
                }
                else
                {
                    Log($"WARNING: Cannot start dialogue - presenter={DialoguePresenter.Instance}, cd={cd}");
                }

                _state = State.PromptStopForDropoff;
                qte.ShowBreak();
                Log("Showing break prompt, waiting for player to stop");

                // Wait for player to initiate stop (handled by StopMove())
                while (_state == State.PromptStopForDropoff)
                    yield return null;

                qte.HideBreak();

                // Wait for car to fully stop
                while (_state == State.WaitingStopForDropoff && movementController.IsMoving)
                    yield return null;

                StopEngineSound();

                // Door opens for passenger exit
                _state = State.DoorOpeningForExit;
                PlayDoorSound(doorOpenSound);
                yield return WaitRandom(doorOpenDelayRange);
                yield return WaitRandom(afterDoorOpenDelayRange);

                _state = State.PassengerExiting;
                characterActor.Exit();
                yield return WaitRandom(passengerExitAnimWaitRange);

                // Door closes after passenger exited
                _state = State.DoorClosingAfterExit;
                PlayDoorSound(doorCloseSound);
                yield return WaitRandom(doorCloseDelayRange);
                yield return WaitRandom(afterDoorCloseDelayRange);

                // Engine starts after door closed, continue to next passenger
                StartEngineSound();

                _state = State.BetweenDialoguesDelay;
                yield return WaitRandom(delayBetweenDialoguesRange);

                _dialogueIndex++;
            }

            _state = State.FinishedDelay;
            StopEngineSound();
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

        void PlayDoorSound(AudioClip clip)
        {
            if (!clip || !audioSource) return;
            audioSource.PlayOneShot(clip);
            Log($"Playing door sound: {clip.name}");
        }

        void StartEngineSound()
        {
            if (!engineAudioSource) return;

            // Play engine start sound
            if (engineStartSound)
            {
                audioSource.PlayOneShot(engineStartSound);
                Log($"Playing engine start sound: {engineStartSound.name}");
            }

            // Start engine loop
            if (engineLoopSound)
            {
                engineAudioSource.clip = engineLoopSound;
                engineAudioSource.loop = true;
                engineAudioSource.volume = engineLoopVolume;
                engineAudioSource.PlayDelayed(engineStartSound ? engineStartSound.length * 0.7f : 0f);
                Log("Starting engine loop");
            }
        }

        void StopEngineSound()
        {
            if (!engineAudioSource) return;

            // Stop engine loop
            engineAudioSource.Stop();
            Log("Stopping engine loop");

            // Play engine stop sound
            if (engineStopSound && audioSource)
            {
                audioSource.PlayOneShot(engineStopSound);
                Log($"Playing engine stop sound: {engineStopSound.name}");
            }
        }

        void Log(string msg)
        {
            if (showDebugLogs) Debug.Log($"[{nameof(RoundController)}] {_state}: {msg}");
        }
    }
}