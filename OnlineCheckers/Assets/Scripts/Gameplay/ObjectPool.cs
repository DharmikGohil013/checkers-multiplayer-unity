using System.Collections.Generic;
using UnityEngine;
using Checkers.Utilities;

namespace Checkers.Gameplay
{
    /// <summary>
    /// Generic object pool for MonoBehaviour-derived components.
    /// Pre-warms on initialization and auto-expands with a warning if exhausted.
    /// Used for CheckersPiece and BoardCell pooling to avoid runtime allocations.
    /// </summary>
    /// <typeparam name="T">MonoBehaviour type to pool.</typeparam>
    public class ObjectPool<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region Fields

        private T _prefab;
        private Queue<T> _pool;
        private Transform _poolParent;
        private int _initialSize;
        private bool _initialized;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the pool with a prefab and pre-warms the specified number of instances.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="initialSize">Number of instances to pre-create.</param>
        public void Initialize(T prefab, int initialSize)
        {
            if (_initialized)
            {
                GameLogger.Log(GameLogger.LogLevel.WARN,
                    $"ObjectPool<{typeof(T).Name}> already initialized.");
                return;
            }

            _prefab = prefab;
            _initialSize = initialSize;
            _pool = new Queue<T>(initialSize);

            // Create a parent object for pooled items
            GameObject parentGO = new GameObject($"Pool_{typeof(T).Name}");
            parentGO.transform.SetParent(transform);
            _poolParent = parentGO.transform;

            // Pre-warm the pool
            for (int i = 0; i < initialSize; i++)
            {
                T instance = CreateInstance();
                instance.gameObject.SetActive(false);
                _pool.Enqueue(instance);
            }

            _initialized = true;

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"ObjectPool<{typeof(T).Name}> initialized with {initialSize} instances.");
        }

        #endregion

        #region Pool Operations

        /// <summary>
        /// Retrieves an instance from the pool. If the pool is empty, creates a new instance
        /// (with a warning log about pool exhaustion).
        /// </summary>
        public T Get()
        {
            if (!_initialized)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR,
                    $"ObjectPool<{typeof(T).Name}>.Get() called before Initialize().");
                return null;
            }

            T instance;

            if (_pool.Count > 0)
            {
                instance = _pool.Dequeue();

                // Skip null/destroyed instances
                while (instance == null && _pool.Count > 0)
                {
                    instance = _pool.Dequeue();
                }

                if (instance != null)
                {
                    instance.gameObject.SetActive(true);
                    return instance;
                }
            }

            // Pool exhausted — create a new instance
            GameLogger.Log(GameLogger.LogLevel.WARN,
                $"ObjectPool<{typeof(T).Name}> exhausted! Creating new instance. " +
                $"Consider increasing initial pool size (currently {_initialSize}).");

            instance = CreateInstance();
            instance.gameObject.SetActive(true);
            return instance;
        }

        /// <summary>
        /// Returns an instance to the pool. The GameObject is deactivated.
        /// </summary>
        public void Return(T instance)
        {
            if (instance == null)
                return;

            if (!_initialized)
            {
                GameLogger.Log(GameLogger.LogLevel.WARN,
                    $"ObjectPool<{typeof(T).Name}>.Return() called before Initialize(). Destroying object.");
                Destroy(instance.gameObject);
                return;
            }

            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_poolParent);
            _pool.Enqueue(instance);
        }

        /// <summary>
        /// Returns the current number of available instances in the pool.
        /// </summary>
        public int AvailableCount => _initialized ? _pool.Count : 0;

        /// <summary>
        /// Clears the pool and destroys all instances.
        /// </summary>
        public void Clear()
        {
            if (!_initialized)
                return;

            while (_pool.Count > 0)
            {
                T instance = _pool.Dequeue();
                if (instance != null)
                    Destroy(instance.gameObject);
            }

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"ObjectPool<{typeof(T).Name}> cleared.");
        }

        #endregion

        #region Helpers

        private T CreateInstance()
        {
            if (_prefab == null)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR,
                    $"ObjectPool<{typeof(T).Name}> prefab is null!");
                return null;
            }

            T instance = Instantiate(_prefab, _poolParent);
            return instance;
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            Clear();
        }

        #endregion
    }
}
