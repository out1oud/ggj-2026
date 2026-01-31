using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Character
{
    public class CharacterActor : MonoBehaviour
    {
        static readonly int Moving = Animator.StringToHash("Moving");
        static readonly int TriggerEnter = Animator.StringToHash("Enter");
        static readonly int TriggerExit = Animator.StringToHash("Exit");

        [SerializeField] Dictionary<string, Sprite> sprites;
        
        Animator _animator;
        Image _image;

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _image = GetComponent<Image>();
        }
        
        public void StartMove() => _animator.SetBool(Moving, true);
        
        public void StopMove() => _animator.SetBool(Moving, false);
        
        public void Enter() => _animator.SetTrigger(TriggerEnter);
        
        public void Exit() => _animator.ResetTrigger(TriggerExit);

        public void SetCharacter(string characterId)
        {
            _image.sprite = sprites[characterId];
        }
    }
}