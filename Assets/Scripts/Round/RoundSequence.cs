using System.Collections.Generic;
using DialogueSystem;
using UnityEngine;

namespace Round
{
    [CreateAssetMenu(menuName = "Round/RoundSequence")]
    public class RoundSequence : ScriptableObject
    {
        public List<CharacterDialogue> dialogues;
    }
}