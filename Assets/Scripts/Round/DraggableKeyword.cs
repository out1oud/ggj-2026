using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Round
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DraggableKeyword : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] TMP_Text labelText;
        [SerializeField] Image backgroundImage;
        [SerializeField] KeywordType keywordType;
        [SerializeField] Color personColor = new(0.8f, 0.4f, 0.4f);
        [SerializeField] Color actionColor = new(0.4f, 0.8f, 0.4f);
        [SerializeField] Color objectColor = new(0.4f, 0.4f, 0.8f);
        [SerializeField] Color locationColor = new(0.8f, 0.8f, 0.4f);
        [SerializeField] Color timeColor = new(0.8f, 0.4f, 0.8f);
        
        string _keywordValue;
        CanvasGroup _canvasGroup;
        RectTransform _rectTransform;
        Canvas _canvas;
        Transform _originalParent;
        Vector2 _originalPosition;
        bool _isClone;
        DraggableKeyword _sourceKeyword;
        
        public KeywordType KeywordType => keywordType;
        
        public string KeywordValue => _keywordValue;
        
        public bool IsClone => _isClone;
        
        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
        }
        
        public void Initialize(string value, KeywordType type)
        {
            _keywordValue = value;
            keywordType = type;
            
            if (labelText != null)
            {
                labelText.text = value;
            }
            
            UpdateColor();
        }
        
        public void SetAsClone(DraggableKeyword source)
        {
            _isClone = true;
            _sourceKeyword = source;
            _keywordValue = source._keywordValue;
            keywordType = source.keywordType;
            
            if (labelText != null)
            {
                labelText.text = _keywordValue;
            }
            
            UpdateColor();
        }
        
        void UpdateColor()
        {
            if (backgroundImage == null) return;
            
            backgroundImage.color = keywordType switch
            {
                KeywordType.Person => personColor,
                KeywordType.Action => actionColor,
                KeywordType.Object => objectColor,
                KeywordType.Location => locationColor,
                KeywordType.Time => timeColor,
                _ => Color.gray
            };
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isClone)
            {
                DraggableKeyword clone = Instantiate(this, _canvas.transform);
                clone.SetAsClone(this);
                clone._rectTransform.position = _rectTransform.position;
                clone._rectTransform.sizeDelta = _rectTransform.sizeDelta;
                
                // Transfer drag to clone
                eventData.pointerDrag = clone.gameObject;
                clone.OnBeginDrag(eventData);
                return;
            }
            
            _originalParent = transform.parent;
            _originalPosition = _rectTransform.anchoredPosition;
            
            transform.SetParent(_canvas.transform);
            transform.SetAsLastSibling();
            
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.8f;
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isClone) return;
            
            _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isClone) return;
            
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
            
            if (eventData.pointerCurrentRaycast.gameObject == null ||
                eventData.pointerCurrentRaycast.gameObject.GetComponent<KeywordDropZone>() == null)
            {
                Destroy(gameObject);
            }
        }
        
        public void OnPlacedInDropZone(KeywordDropZone zone)
        {
            transform.SetParent(zone.transform);
            _rectTransform.anchoredPosition = Vector2.zero;
            _rectTransform.anchorMin = new(0.5f, 0.5f);
            _rectTransform.anchorMax = new(0.5f, 0.5f);
            _rectTransform.pivot = new(0.5f, 0.5f);
        }
        
        public void RemoveFromDropZone()
        {
            if (_isClone)
            {
                Destroy(gameObject);
            }
        }
    }
}
