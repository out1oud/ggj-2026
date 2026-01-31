using UnityEngine;

namespace Utilities
{
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        [Tooltip("Keep this object when loading a new scene?")] [SerializeField]
        bool isPersistent;

        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance && gameObject && isPersistent)
            {
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;

            if (isPersistent && !gameObject.transform.parent) DontDestroyOnLoad(gameObject);
        }
    }
}