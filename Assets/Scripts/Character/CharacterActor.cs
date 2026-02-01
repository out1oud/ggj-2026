using System;
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

        [SerializeField] List<Character> characters;

        Animator _animator;
        Image _image;
        
        public event Action OnEnterFinished;
        public event Action OnExitFinished;

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _image = GetComponent<Image>();
        }

        public void StartMove() => _animator.SetBool(Moving, true);

        public void StopMove() => _animator.SetBool(Moving, false);

        public void Enter()
        {
            _animator.ResetTrigger(TriggerExit);
            _animator.SetTrigger(TriggerEnter);
        }

        public void Exit()
        {
            _animator.ResetTrigger(TriggerEnter);
            _animator.SetTrigger(TriggerExit);
        }

        public void SetCharacter(string characterId)
        {
            _image.sprite = characters.Find(x => x.id == characterId).sprite;
        }
        
        public void AnimEvent_EnterFinished() => OnEnterFinished?.Invoke();
        public void AnimEvent_ExitFinished()  => OnExitFinished?.Invoke();
    }

    [Serializable]
    struct Character
    {
        public string id;
        public Sprite sprite;
    }
}