using System.Collections.Generic;
using UnityEngine;
using Checkers.Utilities;

/// <summary>
/// Generic MonoBehaviour object pool.
/// Attach two instances to GameManagers — one for pieces, one for highlights.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    [Header("Pool Configuration")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialSize = 24;
    [SerializeField] private Transform poolParent;

    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    private readonly List<GameObject> _active = new List<GameObject>();

    private void Awake()
    {
        if (poolParent == null)
            poolParent = transform;

        PreWarm();
    }

    private void PreWarm()
    {
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = CreateNew();
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
    }

    private GameObject CreateNew()
    {
        GameObject obj = Instantiate(prefab, poolParent);
        return obj;
    }

    /// <summary>Get a pooled object. Auto-expands if pool is empty.</summary>
    public GameObject Get()
    {
        GameObject obj;

        if (_pool.Count > 0)
        {
            obj = _pool.Dequeue();
        }
        else
        {
            GameLogger.Log(GameLogger.LogLevel.WARN, $"[ObjectPool] Pool empty for {prefab.name} — expanding.");
            obj = CreateNew();
        }

        obj.SetActive(true);
        _active.Add(obj);
        return obj;
    }

    /// <summary>Get a pooled object cast to a specific component.</summary>
    public T Get<T>() where T : Component
    {
        return Get().GetComponent<T>();
    }

    /// <summary>Return an object back to the pool.</summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        obj.transform.SetParent(poolParent);
        _active.Remove(obj);
        _pool.Enqueue(obj);
    }

    /// <summary>Return all currently active objects back to the pool.</summary>
    public void ReturnAll()
    {
        // copy to avoid modifying list during iteration
        var toReturn = new List<GameObject>(_active);
        foreach (var obj in toReturn)
            Return(obj);
    }

    public int ActiveCount => _active.Count;
    public int PooledCount => _pool.Count;
}

/// <summary>
/// A generic object pool for components.
/// </summary>
public class ObjectPool<T> : MonoBehaviour where T : Component
{
    private T _prefab;
    private int _initialSize;
    private Transform _poolParent;
    private readonly Queue<T> _pool = new Queue<T>();
    private bool _initialized;

    public void Initialize(T prefab, int initialSize)
    {
        _prefab = prefab;
        _initialSize = initialSize;
        _poolParent = transform;
        
        for (int i = 0; i < initialSize; i++)
        {
            T instance = CreateInstance();
            instance.gameObject.SetActive(false);
            _pool.Enqueue(instance);
        }
        
        _initialized = true;
    }

    public T Get()
    {
        if (!_initialized)
        {
            GameLogger.Log(GameLogger.LogLevel.ERROR, $"ObjectPool<{typeof(T).Name}> not initialized!");
            return null;
        }

        while (_pool.Count > 0)
        {
            T instance = _pool.Dequeue();
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

        T newInstance = CreateInstance();
        newInstance.gameObject.SetActive(true);
        return newInstance;
    }

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

    public int AvailableCount => _initialized ? _pool.Count : 0;

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

    private void OnDestroy()
    {
        Clear();
    }
}
