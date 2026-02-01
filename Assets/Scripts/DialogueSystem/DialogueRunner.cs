using System;
using System.Collections.Generic;
using System.Linq;

namespace DialogueSystem
{
    public sealed class DialogueRunner
    {
        CharacterDialogue _dialogue;
        readonly Dictionary<string, DialogueNode> _byId = new();

        public DialogueNode Current { get; private set; }

        public Action<DialogueActionType, float> OnAction;
        public Action<DialogueNode> OnNode;
        public Action<List<DialogueAnswer>> OnAnswers;
        public Action OnEnd;

        public void Start(CharacterDialogue dialogue, string startId = null)
        {
            _dialogue = dialogue;
            _byId.Clear();
            Current = null;

            if (_dialogue?.nodes == null || _dialogue.nodes.Count == 0)
            {
                UnityEngine.Debug.LogWarning($"[DialogueRunner] Dialogue '{_dialogue?.name}' has no nodes! Ending immediately.");
                OnEnd?.Invoke();
                return;
            }

            foreach (DialogueNode n in _dialogue.nodes.Where(n => n != null && !string.IsNullOrWhiteSpace(n.id)))
            {
                _byId[n.id.Trim()] = n;
            }

            DialogueNode startNode = null;

            if (!string.IsNullOrWhiteSpace(startId))
                _byId.TryGetValue(startId.Trim(), out startNode);

            startNode ??= _dialogue.nodes[0];

            GoToNode(startNode);
        }

        public void GoTo(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                OnEnd?.Invoke();
                return;
            }

            if (_byId.TryGetValue(targetId.Trim(), out var node))
            {
                GoToNode(node);
                return;
            }

            OnEnd?.Invoke();
        }

        void GoToNode(DialogueNode node)
        {
            Current = node;

            if (node.actionType != DialogueActionType.None)
                OnAction?.Invoke(node.actionType, node.actionValue);

            OnNode?.Invoke(node);

            if (node.HasAnswers)
                OnAnswers?.Invoke(node.answers);
        }
    }
}