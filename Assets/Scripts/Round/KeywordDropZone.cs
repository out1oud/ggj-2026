using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Round
{
    public class KeywordDropZone : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [SerializeField] KeywordType acceptedType;
        [SerializeField] string fieldId;
        [SerializeField] TMP_Text placeholderText;
        [SerializeField] Image borderImage;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color highlightColor = new(0.5f, 1f, 0.5f);
        [SerializeField] Color errorColor = new(1f, 0.5f, 0.5f);
        [SerializeField] string expectedValue;
        
        DraggableKeyword _currentKeyword;
        
        public event Action<KeywordDropZone, string> OnValueChanged;
        
        public KeywordType AcceptedType => acceptedType;
        
        public string FieldId => fieldId;
        
        public string ExpectedValue => expectedValue;
        
        public string CurrentValue => _currentKeyword?.KeywordValue;
        
        public bool HasKeyword => _currentKeyword != null;
        
        public bool IsCorrect => HasKeyword && CurrentValue == expectedValue;
        
        void Start()
        {
            UpdateVisuals();
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            var keyword = eventData.pointerDrag?.GetComponent<DraggableKeyword>();
            if (keyword == null || !keyword.IsClone) return;
            
            if (keyword.KeywordType != acceptedType)
            {
                StartCoroutine(FlashError());
                return;
            }
            
            if (_currentKeyword != null)
            {
                _currentKeyword.RemoveFromDropZone();
            }
            
            _currentKeyword = keyword;
            keyword.OnPlacedInDropZone(this);
            
            UpdateVisuals();
            OnValueChanged?.Invoke(this, CurrentValue);
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_currentKeyword) return;
            
            _currentKeyword.RemoveFromDropZone();
            _currentKeyword = null;
            UpdateVisuals();
            OnValueChanged?.Invoke(this, null);
        }
        
        public void Clear()
        {
            if (_currentKeyword != null)
            {
                _currentKeyword.RemoveFromDropZone();
                _currentKeyword = null;
            }
            UpdateVisuals();
        }
        
        public void SetExpectedValue(string value)
        {
            expectedValue = value;
        }
        
        public void ShowValidation(bool isCorrect)
        {
            if (borderImage != null)
            {
                borderImage.color = isCorrect ? highlightColor : errorColor;
            }
        }
        
        public void ResetVisuals()
        {
            if (borderImage != null)
            {
                borderImage.color = normalColor;
            }
            UpdateVisuals();
        }
        
        void UpdateVisuals()
        {
            if (placeholderText != null)
            {
                placeholderText.gameObject.SetActive(!HasKeyword);
            }
        }
        
        System.Collections.IEnumerator FlashError()
        {
            if (!borderImage) yield break;
            
            borderImage.color = errorColor;
            yield return new WaitForSeconds(0.3f);
            borderImage.color = normalColor;
        }
    }
}
