using UnityEngine;

namespace Round
{
    using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DraggableWord : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI")]
    [SerializeField]
    TMP_Text label;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Image background;

    [Header("Colors by Type")]
    [SerializeField] Color nameColor = new(0.17f, 0.35f, 0.63f);       // #2C5AA0
    [SerializeField] Color professionColor = new(0.24f, 0.55f, 0.24f); // #3D8B3D
    [SerializeField] Color actionColor = new(0.83f, 0.52f, 0.06f);     // #D4850F
    [SerializeField] Color subjectColor = new(0.42f, 0.23f, 0.55f);    // #6B3A8C

    [Header("Runtime")]
    public WordEntry Data { get; private set; }
    public bool IsSource { get; private set; } = true;

    /// <summary>Текущий перетаскиваемый клон (для DropSlot)</summary>
    public static DraggableWord CurrentlyDragged { get; private set; }

    RectTransform _rect;
    Canvas _rootCanvas;
    Transform _originalParent;
    DraggableWord _dragInstance; // если это source, тут будет клон
    Vector2 _pointerOffset;
    Vector2 _originalSize;

    public void Init(WordEntry data, bool isSource, Canvas rootCanvas)
    {
        Data = data;
        IsSource = isSource;
        _rootCanvas = rootCanvas;

        if (!label) label = GetComponentInChildren<TMP_Text>(true);
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!background) background = GetComponent<Image>();

        label.text = data.title;
        if (background) background.color = GetColorForType(data.type);
        _rect = (RectTransform)transform;
    }

    Color GetColorForType(WordType type)
    {
        return type switch
        {
            WordType.Name => nameColor,
            WordType.Profession => professionColor,
            WordType.Action => actionColor,
            WordType.Subject => subjectColor,
            _ => Color.white
        };
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalParent = transform.parent;
        _originalSize = _rect.sizeDelta;

        if (IsSource)
        {
            // создаём клон, который и будет реально таскаться
            var cloneGo = Instantiate(gameObject, _rootCanvas.transform);
            _dragInstance = cloneGo.GetComponent<DraggableWord>();
            _dragInstance.Init(Data, isSource: false, _rootCanvas);

            // сохраняем размер оригинала
            var cloneRect = (RectTransform)cloneGo.transform;
            cloneRect.sizeDelta = _rect.sizeDelta;

            // чтобы клон не считался "источником" по визуалу (опционально)
            _dragInstance.canvasGroup.alpha = 1f;
            _dragInstance.canvasGroup.blocksRaycasts = false;

            _dragInstance.CalcPointerOffset(eventData);

            // запоминаем текущий перетаскиваемый клон
            CurrentlyDragged = _dragInstance;

            // source не трогаем
        }
        else
        {
            // сохраняем размер перед переносом
            var savedSize = _rect.sizeDelta;
            
            // таскаем себя
            transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
            
            // восстанавливаем размер после смены родителя
            _rect.sizeDelta = savedSize;
            
            canvasGroup.blocksRaycasts = false;
            CalcPointerOffset(eventData);

            // запоминаем текущий перетаскиваемый элемент
            CurrentlyDragged = this;
        }
    }

    void CalcPointerOffset(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_rootCanvas.transform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint
        );

        _pointerOffset = (Vector2)transform.localPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        var target = IsSource ? _dragInstance : this;
        if (!target) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_rootCanvas.transform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint
        );

        ((RectTransform)target.transform).localPosition = localPoint + _pointerOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CurrentlyDragged = null;

        if (IsSource)
        {
            // клон либо будет принят DropSlot'ом, либо уничтожен
            if (_dragInstance != null)
            {
                _dragInstance.canvasGroup.blocksRaycasts = true;

                // Если не был "пристроен" в слот (остался на канвасе)
                // DropSlot сам переставит родителя, если принял.
                if (_dragInstance.transform.parent == _rootCanvas.transform)
                {
                    Destroy(_dragInstance.gameObject);
                }
            }

            _dragInstance = null;
        }
        else
        {
            canvasGroup.blocksRaycasts = true;

            // Если нас никуда не дропнули — можно вернуть назад или удалить
            // Тут вариант: удалить если не в слоте
            if (transform.parent == _rootCanvas.transform)
                Destroy(gameObject);
        }
    }
}

}