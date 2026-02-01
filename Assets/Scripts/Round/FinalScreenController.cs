using System.Collections.Generic;
using System.Linq;
using DialogueSystem;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Round
{
    public class FinalScreenController : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Source for extracting words (used by editor button)")]
        [SerializeField] RoundSequence roundSequence;
        
        [SerializeField] List<WordEntry> words = new();

        [Header("UI")] 
        [SerializeField] WordBank wordBank;
        
        [Header("Check Button")]
        [SerializeField] Button checkButton;
        
        [Header("Error Popup")]
        [SerializeField] GameObject errorPopup;
        [SerializeField] Button errorCloseButton;
        [SerializeField] TMP_Text errorText;
        [SerializeField] string errorMessage = "Некоторые ответы неверны. Попробуйте ещё раз!";
        
        [Header("Success Popup")]
        [SerializeField] GameObject successPopup;
        [SerializeField] Button restartButton;
        [SerializeField] TMP_Text cluesText;
        [SerializeField] TMP_Text trafficLightsText;
        [SerializeField] string cluesFormat = "Собрано улик: {0}/{1}";
        [SerializeField] string trafficLightsFormat = "Светофоры: {0}/{1}";
        
        // Exposed for editor script
        public RoundSequence RoundSequence => roundSequence;
        public List<WordEntry> Words { get => words; set => words = value; }
        public List<string> CorrectProfessionAnswers { get => correctProfessionAnswers; set => correctProfessionAnswers = value; }
        public List<string> CorrectTextAnswers { get => correctTextAnswers; set => correctTextAnswers = value; }

        [SerializeField]
        List<DropSlot> professionSlots = new();

        [Tooltip("Correct answers for profession slots (by index, use word ID or title)")]
        [SerializeField]
        List<string> correctProfessionAnswers = new();

        [SerializeField]
        List<DropSlot> textSlots = new();

        [Tooltip("Correct answers for text slots (by index, use word ID or title)")]
        [SerializeField]
        List<string> correctTextAnswers = new();

        void Awake()
        {
            // базовая валидация списка (id уникальный)
            words = words
                .GroupBy(w => w.id)
                .Select(g => g.First())
                .ToList();
        }

        void Start()
        {
            wordBank.Build(words);
            
            // Setup check button
            if (checkButton)
            {
                checkButton.onClick.AddListener(OnCheckButtonClicked);
                checkButton.interactable = false;
                Debug.Log("[FinalScreen] Check button initialized");
            }
            else
            {
                Debug.LogWarning("[FinalScreen] Check button is not assigned!");
            }
            
            // Setup popup buttons
            if (errorCloseButton)
                errorCloseButton.onClick.AddListener(HideErrorPopup);
            
            if (restartButton)
                restartButton.onClick.AddListener(OnRestartClicked);
            
            // Hide popups initially
            if (errorPopup) errorPopup.SetActive(false);
            if (successPopup) successPopup.SetActive(false);
            
            Debug.Log($"[FinalScreen] Slots: {professionSlots.Count} profession, {textSlots.Count} text");
        }

        void Update()
        {
            // Update check button interactability based on completion
            if (checkButton)
            {
                bool complete = IsComplete();
                if (checkButton.interactable != complete)
                {
                    checkButton.interactable = complete;
                    Debug.Log($"[FinalScreen] Button interactable changed to: {complete}");
                }
            }
        }

        void OnCheckButtonClicked()
        {
            Debug.Log("[FinalScreen] Check button clicked!");
            
            if (!IsComplete())
            {
                Debug.Log("[FinalScreen] Not complete, ignoring click");
                return;
            }
            
            // Log what we're checking
            Debug.Log($"[FinalScreen] Checking: {professionSlots.Count} profession slots, {textSlots.Count} text slots");
            Debug.Log($"[FinalScreen] Correct answers defined: {correctProfessionAnswers.Count} profession, {correctTextAnswers.Count} text");
            
            bool allCorrect = CheckAllCorrect();
            Debug.Log($"[FinalScreen] All correct: {allCorrect}");
            
            if (allCorrect)
            {
                ShowSuccessPopup();
            }
            else
            {
                ShowErrorPopup();
            }
        }

        void ShowErrorPopup()
        {
            Debug.Log("[FinalScreen] Showing error popup");
            if (errorPopup)
            {
                if (errorText) errorText.text = errorMessage;
                errorPopup.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[FinalScreen] Error popup is not assigned!");
            }
            if (successPopup) successPopup.SetActive(false);
        }

        void HideErrorPopup()
        {
            if (errorPopup) errorPopup.SetActive(false);
        }

        void ShowSuccessPopup()
        {
            Debug.Log("[FinalScreen] Showing success popup");
            if (errorPopup) errorPopup.SetActive(false);
            
            if (successPopup)
            {
                // Update clues stats
                if (cluesText && CluesCollector.Instance != null)
                {
                    int collected = CluesCollector.Instance.CollectedClues.Count;
                    int total = collected + CluesCollector.Instance.MissedClues.Count;
                    cluesText.text = string.Format(cluesFormat, collected, total);
                    Debug.Log($"[FinalScreen] Clues: {collected}/{total}");
                }
                
                // Update traffic light stats
                if (trafficLightsText && GameplayController.Instance != null)
                {
                    int successes = GameplayController.Instance.TrafficLightSuccesses;
                    int total = GameplayController.Instance.TotalTrafficLightInteractions;
                    trafficLightsText.text = string.Format(trafficLightsFormat, successes, total);
                    Debug.Log($"[FinalScreen] Traffic lights: {successes}/{total}");
                }
                
                successPopup.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[FinalScreen] Success popup is not assigned!");
            }
        }

        void OnRestartClicked()
        {
            // Reload current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public bool IsComplete()
        {
            // Must have at least some slots to be considered complete
            if (professionSlots.Count == 0 && textSlots.Count == 0)
                return false;
            
            bool portraitsOk = professionSlots.All(s => s.GetWord() != null);
            bool textOk = textSlots.All(s => s.GetWord() != null);
            return portraitsOk && textOk;
        }

        public FinalScreenResult CollectResult()
        {
            var res = new FinalScreenResult
            {
                portraits = new(),
                text = new()
            };

            int portraitsCount = Mathf.Min(professionSlots.Count);
            for (int i = 0; i < portraitsCount; i++)
            {
                res.portraits.Add(new()
                {
                    portraitIndex = i,
                    profession = professionSlots[i].GetWord(),
                });
            }

            foreach (var slot in textSlots)
                res.text.Add(slot.GetWord());

            return res;
        }

        public void ClearAll()
        {
            foreach (var s in professionSlots) s.Clear();
            foreach (var s in textSlots) s.Clear();
        }

        /// <summary>
        /// Checks if all answers match the correct answers.
        /// </summary>
        public bool CheckAllCorrect()
        {
            return CheckProfessionAnswers() && CheckTextAnswers();
        }

        /// <summary>
        /// Checks if all profession slot answers are correct.
        /// </summary>
        public bool CheckProfessionAnswers()
        {
            for (int i = 0; i < professionSlots.Count; i++)
            {
                if (!IsSlotCorrect(professionSlots[i], i < correctProfessionAnswers.Count ? correctProfessionAnswers[i] : null))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if all text slot answers are correct.
        /// </summary>
        public bool CheckTextAnswers()
        {
            for (int i = 0; i < textSlots.Count; i++)
            {
                if (!IsSlotCorrect(textSlots[i], i < correctTextAnswers.Count ? correctTextAnswers[i] : null))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns correctness for each profession slot.
        /// </summary>
        public List<bool> GetProfessionCorrectness()
        {
            var results = new List<bool>();
            for (int i = 0; i < professionSlots.Count; i++)
            {
                string correct = i < correctProfessionAnswers.Count ? correctProfessionAnswers[i] : null;
                results.Add(IsSlotCorrect(professionSlots[i], correct));
            }
            return results;
        }

        /// <summary>
        /// Returns correctness for each text slot.
        /// </summary>
        public List<bool> GetTextCorrectness()
        {
            var results = new List<bool>();
            for (int i = 0; i < textSlots.Count; i++)
            {
                string correct = i < correctTextAnswers.Count ? correctTextAnswers[i] : null;
                results.Add(IsSlotCorrect(textSlots[i], correct));
            }
            return results;
        }

        bool IsSlotCorrect(DropSlot slot, string correctAnswer)
        {
            if (string.IsNullOrEmpty(correctAnswer))
            {
                Debug.Log($"[FinalScreen] Slot {slot.name}: no correct answer defined, treating as correct");
                return true;
            }
            
            var word = slot.GetWord();
            if (word == null)
            {
                Debug.Log($"[FinalScreen] Slot {slot.name}: empty, incorrect");
                return false;
            }
            
            bool isCorrect = word.id == correctAnswer || word.title == correctAnswer;
            Debug.Log($"[FinalScreen] Slot {slot.name}: got '{word.title}' (id:{word.id}), expected '{correctAnswer}', correct: {isCorrect}");
            return isCorrect;
        }
    }

    [System.Serializable]
    public class FinalScreenResult
    {
        public List<PortraitFill> portraits;
        public List<WordEntry> text;
    }

    [System.Serializable]
    public class PortraitFill
    {
        public int portraitIndex;
        public WordEntry name;
        public WordEntry profession;
    }
}