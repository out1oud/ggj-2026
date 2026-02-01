using System;
using System.Collections;
using Character;
using DialogueSystem;
using Player;
using TrafficLight;
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
        Coroutine _trafficLightCoroutine;
        
        // Traffic light handling - uses separate flags to avoid state machine conflicts
        TrafficLightController _currentTrafficLight;
        Action _trafficLightCallback;
        bool _trafficLightBrakePressed;
        bool _trafficLightActive;
        
        enum TrafficLightPhase { None, WaitingForBrake, WaitingForStop, WaitingForGo }
        TrafficLightPhase _trafficLightPhase = TrafficLightPhase.None;
        
        /// <summary>
        /// Returns true if currently handling a traffic light sequence.
        /// </summary>
        public bool IsHandlingTrafficLight => _trafficLightActive;

        void Start()
        {
            qte.ShowForward();
            _flow = StartCoroutine(Flow());
        }

        public void StartMove()
        {
            Log($"StartMove called. Current state: {_state}, TrafficLightPhase: {_trafficLightPhase}");
            
            // Traffic light takes priority
            if (_trafficLightActive && _trafficLightPhase == TrafficLightPhase.WaitingForGo)
            {
                // Player pressed forward at traffic light after stopping
                _trafficLightPhase = TrafficLightPhase.None;
                movementController.StartMove();
                qte.HideForward();
                Log("Traffic light: Forward pressed, resuming movement");
                FinishTrafficLightSequence(ranRedLight: false);
                return;
            }
            
            switch (_state)
            {
                case State.WaitingForEngineStart:
                    movementController.StartMove();
                    qte.HideForward();
                    StartEngineSound();
                    _state = State.WaitingInitialTripDelay;
                    Log("Engine started, moving");
                    break;
                case State.WaitingForStartAfterEntry:
                    movementController.StartMove();
                    qte.HideForward();
                    StartEngineSound();
                    _state = State.PauseBeforeNodes;
                    Log("Started moving after passenger entry");
                    break;
                default:
                    Log($"StartMove ignored - state {_state} doesn't accept forward input");
                    break;
            }
        }

        public void StopMove()
        {
            Log($"StopMove called. Current state: {_state}, TrafficLightPhase: {_trafficLightPhase}");
            
            // Traffic light takes priority
            if (_trafficLightActive && _trafficLightPhase == TrafficLightPhase.WaitingForBrake)
            {
                // Player pressed brake at traffic light
                _trafficLightBrakePressed = true;
                _trafficLightPhase = TrafficLightPhase.WaitingForStop;
                movementController.StopMoveSmooth();
                qte.ResolveBreak(hide: true);
                Log("Traffic light: Brake pressed, waiting for stop");
                return;
            }
            
            if (_state == State.PromptStopForDropoff)
            {
                movementController.StopMoveSmooth();
                _state = State.WaitingStopForDropoff;
                Log("Dropoff: Brake pressed, waiting for stop");
            }
            else
            {
                Log($"StopMove ignored - not in a state that accepts brake input");
            }
        }

        /// <summary>
        /// Called by TrafficLightTrigger when player enters a red light zone.
        /// </summary>
        public void HandleTrafficLightRed(TrafficLightController trafficLight, float brakeTimerDuration, Action onComplete)
        {
            if (IsHandlingTrafficLight)
            {
                Log("Already handling a traffic light, ignoring");
                return;
            }

            _currentTrafficLight = trafficLight;
            _trafficLightCallback = onComplete;
            _trafficLightBrakePressed = false;
            _trafficLightActive = true;
            _trafficLightPhase = TrafficLightPhase.WaitingForBrake;

            // Pause the traffic light cycle
            trafficLight.PauseCycle();
            Log($"Traffic light red detected, pausing cycle. Phase: WaitingForBrake. Timer: {brakeTimerDuration}s");

            // Start the traffic light handling coroutine
            if (_trafficLightCoroutine != null)
                StopCoroutine(_trafficLightCoroutine);
            
            _trafficLightCoroutine = StartCoroutine(TrafficLightSequence(brakeTimerDuration));
        }

        IEnumerator TrafficLightSequence(float brakeTimerDuration)
        {
            // Show brake prompt with timer
            qte.ShowBreakWithTimer(brakeTimerDuration, autoHide: false, onTimerEnd: () =>
            {
                // Timer expired without braking - player ran the red light
                if (_trafficLightActive && _trafficLightPhase == TrafficLightPhase.WaitingForBrake && !_trafficLightBrakePressed)
                {
                    Log("Traffic light: Timer expired - ran red light!");
                    // Could add penalty here
                    qte.HideBreak();
                    FinishTrafficLightSequence(ranRedLight: true);
                }
            });

            Log("Traffic light: Showing brake prompt with timer");

            // Wait for brake to be pressed or timer to expire
            while (_trafficLightActive && _trafficLightPhase == TrafficLightPhase.WaitingForBrake)
                yield return null;

            // If brake was pressed, wait for full stop
            if (_trafficLightActive && _trafficLightPhase == TrafficLightPhase.WaitingForStop)
            {
                while (movementController.IsMoving)
                    yield return null;

                Log("Traffic light: Stopped, waiting before switching to green");
                
                // Short pause after stopping
                yield return new WaitForSeconds(0.8f);
                
                // Switch light to green
                if (_currentTrafficLight != null)
                {
                    _currentTrafficLight.ForceGreen(resumeCycle: false);
                    Log("Traffic light: Switched to green");
                }
                
                // Now show forward prompt and wait for player to go
                _trafficLightPhase = TrafficLightPhase.WaitingForGo;
                qte.ShowForward();
                Log("Traffic light: Showing forward prompt");

                // Wait for forward press (handled in StartMove)
                while (_trafficLightActive && _trafficLightPhase == TrafficLightPhase.WaitingForGo)
                    yield return null;
            }
        }

        void FinishTrafficLightSequence(bool ranRedLight = false)
        {
            if (_currentTrafficLight != null)
            {
                if (ranRedLight)
                {
                    // Ran red light - switch to green anyway
                    _currentTrafficLight.ForceGreen(resumeCycle: true);
                    Log("Traffic light: Ran red light, switching to green and resuming cycle");
                }
                else
                {
                    // Stopped correctly - just resume cycle (already green)
                    _currentTrafficLight.ResumeCycle();
                    Log("Traffic light: Resuming cycle");
                }
            }

            // Record stats in GameplayController
            if (GameplayController.Instance != null)
            {
                if (ranRedLight)
                    GameplayController.Instance.RecordTrafficLightFailure();
                else
                    GameplayController.Instance.RecordTrafficLightSuccess();
            }

            // Clear traffic light state
            _trafficLightActive = false;
            _trafficLightPhase = TrafficLightPhase.None;
            _currentTrafficLight = null;

            // Invoke callback
            _trafficLightCallback?.Invoke();
            _trafficLightCallback = null;
            _trafficLightCoroutine = null;
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