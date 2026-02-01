using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Round
{
    [Serializable]
    public class KeywordData
    {
        public string value;
        public KeywordType type;
    }
    
    [Serializable]
    public class FieldData
    {
        public string fieldId;
        public KeywordType acceptedType;
        public string expectedValue;
    }
    
    public class ResultScreenController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform keywordPaletteContainer;
        [SerializeField] Transform dropZonesContainer;
        [SerializeField] DraggableKeyword keywordPrefab;
        [SerializeField] KeywordDropZone dropZonePrefab;
        [SerializeField] Button submitButton;
        [SerializeField] Button resetButton;
        [SerializeField] TMP_Text resultText;
        [SerializeField] CanvasGroup canvasGroup;
        
        [Header("Keywords")]
        [SerializeField] List<KeywordData> availableKeywords = new();
        
        [Header("Fields")]
        [SerializeField] List<FieldData> fields = new();
        
        List<DraggableKeyword> _spawnedKeywords = new();
        List<KeywordDropZone> _dropZones = new();
        
        public event Action<int, int> OnSubmit;
        
        public event Action OnAllCorrect;
        
        void Awake()
        {
            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmitClicked);
            
            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetClicked);
        }
        
        void OnDestroy()
        {
            if (submitButton != null)
                submitButton.onClick.RemoveListener(OnSubmitClicked);
            
            if (resetButton != null)
                resetButton.onClick.RemoveListener(OnResetClicked);
        }
        
        public void Show(List<KeywordData> keywords = null, List<FieldData> showFields = null)
        {
            if (keywords != null)
                availableKeywords = keywords;
            
            if (showFields != null)
                fields = showFields;
            
            gameObject.SetActive(true);
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            SpawnKeywords();
            SetupDropZones();
            
            if (resultText)
                resultText.text = "";
        }
        
        public void Hide()
        {
            if (canvasGroup)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        
        public void AddKeyword(string value, KeywordType type)
        {
            availableKeywords.Add(new() { value = value, type = type });
        }
        
        public void AddField(string fieldId, KeywordType acceptedType, string expectedValue)
        {
            fields.Add(new()
            {
                fieldId = fieldId,
                acceptedType = acceptedType,
                expectedValue = expectedValue
            });
        }
        
        public void ClearAll()
        {
            availableKeywords.Clear();
            fields.Clear();
            ClearSpawnedElements();
        }
        
        public string GetFieldValue(string fieldId)
        {
            var zone = _dropZones.FirstOrDefault(z => z.FieldId == fieldId);
            return zone?.CurrentValue;
        }
        
        public bool AreAllFieldsFilled()
        {
            return _dropZones.All(z => z.HasKeyword);
        }
        
        public Dictionary<string, bool> ValidateFields()
        {
            var results = new Dictionary<string, bool>();
            
            foreach (KeywordDropZone zone in _dropZones)
            {
                results[zone.FieldId] = zone.IsCorrect;
                zone.ShowValidation(zone.IsCorrect);
            }
            
            return results;
        }
        
        void SpawnKeywords()
        {
            foreach (DraggableKeyword keyword in _spawnedKeywords.Where(keyword => keyword != null))
            {
                Destroy(keyword.gameObject);
            }
            _spawnedKeywords.Clear();
            
            if (!keywordPrefab || !keywordPaletteContainer) return;
            
            foreach (KeywordData data in availableKeywords)
            {
                DraggableKeyword keyword = Instantiate(keywordPrefab, keywordPaletteContainer);
                keyword.Initialize(data.value, data.type);
                _spawnedKeywords.Add(keyword);
            }
        }
        
        void SetupDropZones()
        {
            if (dropZonesContainer)
            {
                KeywordDropZone[] existingZones = dropZonesContainer.GetComponentsInChildren<KeywordDropZone>();
                _dropZones = existingZones.ToList();
                
                foreach (KeywordDropZone zone in _dropZones)
                {
                    FieldData fieldData = fields.FirstOrDefault(f => f.fieldId == zone.FieldId);
                    if (fieldData != null)
                    {
                        zone.SetExpectedValue(fieldData.expectedValue);
                    }
                    
                    zone.OnValueChanged -= OnDropZoneValueChanged;
                    zone.OnValueChanged += OnDropZoneValueChanged;
                    zone.ResetVisuals();
                }
            }

            if (!dropZonePrefab || !dropZonesContainer) return;
            
            {
                HashSet<string> existingIds = _dropZones.Select(z => z.FieldId).ToHashSet();
                
                foreach (FieldData field in fields.Where(f => !existingIds.Contains(f.fieldId)))
                {
                    KeywordDropZone zone = Instantiate(dropZonePrefab, dropZonesContainer);
                    zone.SetExpectedValue(field.expectedValue);
                    zone.OnValueChanged += OnDropZoneValueChanged;
                    _dropZones.Add(zone);
                }
            }
        }
        
        void OnDropZoneValueChanged(KeywordDropZone zone, string value)
        {
            zone.ResetVisuals();
            
            // Could add auto-validation or progress tracking here
        }
        
        void OnSubmitClicked()
        {
            Dictionary<string, bool> results = ValidateFields();
            int correct = results.Count(r => r.Value);
            int total = results.Count;
            
            if (resultText != null)
            {
                resultText.text = correct == total
                    ? "Perfect! All answers correct!"
                    : $"Score: {correct}/{total}";
            }
            
            OnSubmit?.Invoke(correct, total);
            
            if (correct == total)
            {
                OnAllCorrect?.Invoke();
            }
        }
        
        void OnResetClicked()
        {
            foreach (KeywordDropZone zone in _dropZones)
            {
                zone.Clear();
                zone.ResetVisuals();
            }
            
            if (resultText != null)
                resultText.text = "";
        }
        
        void ClearSpawnedElements()
        {
            foreach (DraggableKeyword keyword in _spawnedKeywords.Where(keyword => keyword != null))
            {
                Destroy(keyword.gameObject);
            }
            _spawnedKeywords.Clear();
            
            foreach (KeywordDropZone zone in _dropZones.Where(zone => zone != null))
            {
                zone.Clear();
                zone.OnValueChanged -= OnDropZoneValueChanged;
            }
        }
    }
}
