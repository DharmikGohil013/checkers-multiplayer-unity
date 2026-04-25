// Assets/Scripts/Gameplay/PoolBase.cs
using System.Collections.Generic;
using UnityEngine;

namespace Checkers.Gameplay
{
    /// <summary>
    /// Non-generic MonoBehaviour pool base. Safe for Unity.
    /// Do not add this directly — use PiecePool or CellPool instead.
    /// </summary>
    public abstract class PoolBase : MonoBehaviour
    {
        [SerializeField] protected GameObject prefab;
        [SerializeField] protected int        initialSize = 24;
        [SerializeField] protected Transform  poolParent;

        private readonly Queue<GameObject> _available = new Queue<GameObject>();
        private readonly List<GameObject>  _active    = new List<GameObject>();

        protected virtual void Awake()
        {
            if (poolParent == null) poolParent = transform;
            if (prefab != null) PreWarm();
        }

        private void PreWarm()
        {
            for (int i = 0; i < initialSize; i++)
            {
                var obj = Instantiate(prefab, poolParent);
                obj.SetActive(false);
                _available.Enqueue(obj);
            }
        }

        /// <summary>Call this from BoardManager if prefab/parent are set at runtime.</summary>
        public void Setup(GameObject p, Transform parent, int size)
        {
            prefab      = p;
            poolParent  = parent != null ? parent : transform;
            initialSize = size;

            if (_available.Count == 0 && _active.Count == 0)
                PreWarm();
        }

        public GameObject Get()
        {
            if (prefab == null)
            {
                Debug.LogError($"[{GetType().Name}] Prefab is null! Assign it in the Inspector.");
                return null;
            }

            GameObject obj = _available.Count > 0
                ? _available.Dequeue()
                : Instantiate(prefab, poolParent);

            obj.SetActive(true);
            _active.Add(obj);
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            obj.transform.SetParent(poolParent);
            // Reset transform to prevent stale scale/rotation from leaking into reuse
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            _active.Remove(obj);
            _available.Enqueue(obj);
        }

        public void ReturnAll()
        {
            var copy = new List<GameObject>(_active);
            foreach (var o in copy) Return(o);
        }

        public int ActiveCount => _active.Count;
        public int PooledCount => _available.Count;
    }
}