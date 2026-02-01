using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DialogueSystem
{
    [CreateAssetMenu(menuName = "Dialogue/Character Dialogue")]
    public class CharacterDialogue : ScriptableObject
    {
        public string characterId;
        public string characterName;
        
        public TMP_StyleSheet styleSheet;

        public List<DialogueNode> nodes;
    }
}