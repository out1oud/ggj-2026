using UnityEngine;

namespace DialogueSystem
{
    [System.Serializable]
    public class DialogueAnswer
    {
        public string id;
        public string targetId;

        [TextArea(2, 4)] public string text;
 
        public bool shouldPause;
    }
}