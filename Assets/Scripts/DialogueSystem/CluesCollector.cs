using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace DialogueSystem
{
    public class CluesCollector : Singleton<CluesCollector>
    {
        List<string> _collectedClues = new();
        List<string> _missedClues = new();
        
        public List<string> CollectedClues => _collectedClues;
        public List<string> MissedClues => _missedClues;
        
        public void AddClue(string clue)
        {
            _collectedClues.Add(clue);
        }
        
        public void AddMissedClues(List<string> clue)
        {
            _missedClues.AddRange(clue);
        }
    }
}