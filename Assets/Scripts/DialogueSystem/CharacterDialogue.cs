using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    [CreateAssetMenu(menuName = "Dialogue/Character Dialogue")]
    public class CharacterDialogue : ScriptableObject
    {
        public string characterId;
        public string characterName;

        public List<DialogueNode> nodes;
    }
}