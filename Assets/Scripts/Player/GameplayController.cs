using DialogueSystem;
using UnityEngine;
using Utilities;

namespace Player
{
    public class GameplayController : Singleton<GameplayController>
    {
        [SerializeField] CharacterDialogue startingDialogue;
        
        void Start()
        {
            DialoguePresenter.Instance.StartDialogue(startingDialogue);
        }
    }
}