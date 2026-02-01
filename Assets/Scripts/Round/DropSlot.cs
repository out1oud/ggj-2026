using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Round
{
    public class DropSlot : MonoBehaviour, IDropHandler
    {
        public enum AcceptMode
        {
            Any,
            OnlySpecificType
        }

        [Header("Rules")] [SerializeField] AcceptMode acceptMode = AcceptMode.OnlySpecificType;
        [SerializeField] WordType acceptedType;

        [Header("UI")] [SerializeField] TMP_Text placeholder; // "перетащи сюда"
        [SerializeField] string defaultPlaceholderText = "...";
        [SerializeField] Image background;

        [Header("Colors by Type")]
        [SerializeField] Color defaultColor = Color.white;
        [SerializeField] Color nameColor = new(0.17f, 0.35f, 0.63f);       // #2C5AA0
        [SerializeField] Color professionColor = new(0.24f, 0.55f, 0.24f); // #3D8B3D
        [SerializeField] Color actionColor = new(0.83f, 0.52f, 0.06f);     // #D4850F
        [SerializeField] Color subjectColor = new(0.42f, 0.23f, 0.55f);    // #6B3A8C

        public WordEntry CurrentWord { get; private set; }
        
        void Awake()
        {
            if (background) background.color = GetColorForType(acceptedType);
        }

        public bool CanAccept(DraggableWord word)
        {
            if (word == null) return false;
            if (word.IsSource) return false; // источник нельзя вставлять, только клон

            if (acceptMode == AcceptMode.Any) return true;
            return word.Data.type == acceptedType;
        }

        public void OnDrop(PointerEventData eventData)
        {
            // используем статический CurrentlyDragged, т.к. pointerDrag указывает на source, а не на клон
            var dropped = DraggableWord.CurrentlyDragged;
            if (!CanAccept(dropped)) return;

            // сохраняем данные
            CurrentWord = dropped.Data;

            if (placeholder) placeholder.text = dropped.Data.title;
            if (background) background.color = GetColorForType(dropped.Data.type);

            // уничтожаем клон, т.к. используем только текст в label
            Destroy(dropped.gameObject);
        }

        public WordEntry GetWord() => CurrentWord;

        public void Clear()
        {
            CurrentWord = null;
            if (placeholder) placeholder.text = defaultPlaceholderText;
            if (background) background.color = defaultColor;
        }

        Color GetColorForType(WordType type)
        {
            return type switch
            {
                WordType.Name => nameColor,
                WordType.Profession => professionColor,
                WordType.Action => actionColor,
                WordType.Subject => subjectColor,
                _ => defaultColor
            };
        }
    }
}