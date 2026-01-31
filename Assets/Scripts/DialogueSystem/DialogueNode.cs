using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    public enum DialogueActionType
    {
        None,
        TakePhone,
        HidePhone,
        LookAway,
        Smile
    }

    [System.Serializable]
    public class DialogueNode
    {
        [Header("Identity")]
        public string id;
        public string targetId;

        [Header("Action")]
        public DialogueActionType actionType;
        public float actionValue;

        [Header("Content")]
        [TextArea(3, 6)]
        public string text;

        [Header("Answers")]
        public List<DialogueAnswer> answers;

        public bool shouldPause;

        public bool HasAnswers => answers is { Count: > 0 };
    }
}