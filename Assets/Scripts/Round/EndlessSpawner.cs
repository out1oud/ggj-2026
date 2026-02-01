using System.Collections.Generic;
using UnityEngine;

namespace Round
{
    public class EndlessSpawner : MonoBehaviour
    {
        [Header("Prefabs")] [SerializeField] GameObject roadPrefab;

        [Header("References")] [SerializeField]
        Transform target; // машина / игрок

        [Header("Settings")] [SerializeField] float segmentLength = 270f;
        [SerializeField] int keepSegments = 6;
        [SerializeField] int preloadAhead = 2; // сколько сегментов держать "впереди"
        [SerializeField] float despawnBehindDistance = 540f;

        readonly Queue<GameObject> _spawned = new();
        float _nextSpawnX;

        void Start()
        {
            // Стартуем от нуля или от позиции игрока (см. ниже)
            _nextSpawnX = 0f;

            for (int i = 0; i < keepSegments; i++)
                SpawnNext();
        }

        void Update()
        {
            // Вперед = в минус по X
            float needAheadUntilX = target.position.x - preloadAhead * segmentLength;

            while (_nextSpawnX >= needAheadUntilX)
                SpawnNext();

            Cleanup();
        }

        void SpawnNext()
        {
            var pos = new Vector3(_nextSpawnX, 0f, 0f);
            var go = Instantiate(roadPrefab, pos, Quaternion.identity, transform);
            _spawned.Enqueue(go);

            // ВАЖНО: идем в минус
            _nextSpawnX -= segmentLength;
        }

        void Cleanup()
        {
            while (_spawned.Count > 0)
            {
                var oldest = _spawned.Peek();

                // Сегмент слишком далеко СПРАВА от игрока
                float behind = oldest.transform.position.x - target.position.x;

                if (behind > despawnBehindDistance || _spawned.Count > keepSegments)
                {
                    Destroy(_spawned.Dequeue());
                }
                else
                {
                    break;
                }
            }
        }
    }
}