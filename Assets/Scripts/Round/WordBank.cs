using UnityEngine;

namespace Round
{
    using System.Collections.Generic;
    using UnityEngine;

    public class WordBank : MonoBehaviour
    {
        [SerializeField] DraggableWord wordPrefab;
        [SerializeField] Transform contentRoot;
        [SerializeField] Canvas rootCanvas;

        readonly List<DraggableWord> _sources = new();

        public void Build(IReadOnlyList<WordEntry> words)
        {
            // очистка
            foreach (Transform ch in contentRoot) Destroy(ch.gameObject);
            _sources.Clear();

            // копируем и перемешиваем
            var shuffled = new List<WordEntry>(words);
            Shuffle(shuffled);

            foreach (var w in shuffled)
            {
                var go = Instantiate(wordPrefab, contentRoot);
                go.Init(w, isSource: true, rootCanvas);
                _sources.Add(go);
            }
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}