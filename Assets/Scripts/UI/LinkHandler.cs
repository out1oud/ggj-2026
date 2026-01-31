using System.Text.RegularExpressions;
using DialogueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class LinkHandler : MonoBehaviour,
        IPointerMoveHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        TMP_Text _text;

        string _baseText;
        string _hoveredLinkId;

        const string NormalStyle = "link";
        const string HoverStyle = "link_hover";
        const string DisabledStyle = "link_activated";

        bool _processing;
        bool _ignoreTextChanged;

        void Start()
        {
            _text = GetComponent<TMP_Text>();
            EnrichText();
        }

        void OnEnable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }

        void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }

        void OnTextChanged(Object obj)
        {
            if (_processing || _ignoreTextChanged || obj != _text)
                return;

            _processing = true;
            EnrichText();
            _processing = false;
        }

        void EnrichText()
        {
            _hoveredLinkId = null;

            string source = _text.text;

            _baseText = ApplyNormalStyleToAllLinks(source, NormalStyle);

            _ignoreTextChanged = true;
            _text.text = _baseText;
            _text.ForceMeshUpdate();
            _ignoreTextChanged = false;
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(
                _text,
                eventData.position,
                eventData.pressEventCamera
            );

            if (linkIndex == -1)
            {
                ClearHover();
                return;
            }

            string linkId = _text.textInfo.linkInfo[linkIndex].GetLinkID();

            if (_hoveredLinkId == linkId)
                return;

            _hoveredLinkId = linkId;

            string hoveredText = ApplyStyleToLink(_baseText, linkId, HoverStyle, NormalStyle);

            _ignoreTextChanged = true;
            _text.text = hoveredText;
            _text.ForceMeshUpdate();
            _ignoreTextChanged = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ClearHover();
        }

        void ClearHover()
        {
            if (_hoveredLinkId == null)
                return;

            _hoveredLinkId = null;

            _ignoreTextChanged = true;
            _text.text = _baseText;
            _text.ForceMeshUpdate();
            _ignoreTextChanged = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(
                _text,
                eventData.position,
                eventData.pressEventCamera
            );
            if (linkIndex == -1) return;

            string id = _text.textInfo.linkInfo[linkIndex].GetLinkID();

            CluesCollector.Instance.AddClue(id);

            _baseText = DisableLink(_baseText, id, DisabledStyle);

            _hoveredLinkId = null;

            _ignoreTextChanged = true;
            _text.text = _baseText;
            _text.ForceMeshUpdate();
            _ignoreTextChanged = false;
        }

        static string ApplyNormalStyleToAllLinks(string sourceText, string normalStyle)
        {
            if (string.IsNullOrEmpty(sourceText) || string.IsNullOrEmpty(normalStyle))
                return sourceText;

            var linkRegex = new Regex(
                @"<link\s*=\s*(""|')?.+?\1?\s*>(?<inner>.*?)</link>",
                RegexOptions.Singleline
            );

            return linkRegex.Replace(sourceText, match =>
            {
                string inner = match.Groups["inner"].Value;

                if (Regex.IsMatch(inner, @"<style\s*="))
                    return match.Value;

                inner = $"<style={normalStyle}>{inner}</style>";
                return match.Value.Replace(match.Groups["inner"].Value, inner);
            });
        }

        static string ApplyStyleToLink(string sourceText, string linkId, string newStyle, string defaultStyle = null)
        {
            if (string.IsNullOrEmpty(sourceText) || string.IsNullOrEmpty(linkId))
                return sourceText;

            string escapedId = Regex.Escape(linkId);

            var linkRegex = new Regex(
                $@"<link\s*=\s*(""|')?{escapedId}\1?\s*>(?<inner>.*?)</link>",
                RegexOptions.Singleline
            );

            return linkRegex.Replace(sourceText, match =>
            {
                string inner = match.Groups["inner"].Value;

                inner = Regex.Replace(inner, @"<style\s*=.*?>", "");
                inner = Regex.Replace(inner, @"</style>", "");

                if (string.IsNullOrEmpty(newStyle))
                {
                    if (!string.IsNullOrEmpty(defaultStyle))
                        inner = $"<style={defaultStyle}>{inner}</style>";
                }
                else
                {
                    inner = $"<style={newStyle}>{inner}</style>";
                }

                return $"<link={linkId}>{inner}</link>";
            }, 1);
        }

        static string DisableLink(string sourceText, string linkId, string disabledStyle)
        {
            if (string.IsNullOrEmpty(sourceText) || string.IsNullOrEmpty(linkId))
                return sourceText;

            string escapedId = Regex.Escape(linkId);

            var linkRegex = new Regex(
                $@"<link\s*=\s*(""|')?{escapedId}\1?\s*>(?<inner>.*?)</link>",
                RegexOptions.Singleline
            );

            return linkRegex.Replace(sourceText, match =>
            {
                string inner = match.Groups["inner"].Value;

                inner = Regex.Replace(inner, @"<style\s*=.*?>", "");
                inner = Regex.Replace(inner, @"</style>", "");

                return $"<style={disabledStyle}>{inner}</style>";
            }, 1);
        }
    }
}
