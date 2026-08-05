using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : SingletonBase<ObjectPoolManager>
{
    private Dictionary<string, Queue<GameObject>> _pools
        = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> _prefabs
        = new Dictionary<string, GameObject>();
    private Dictionary<string, Transform> _poolRoots
        = new Dictionary<string, Transform>();

    public void CreatePool(string key, GameObject prefab, int initialCount)
    {
        if (_pools.ContainsKey(key))
        {
            return;
        }

        GameObject rootObject = new GameObject($"Pool_{key}");
        rootObject.transform.SetParent(transform);

        _pools.Add(key, new Queue<GameObject>());
        _prefabs.Add(key, prefab);
        _poolRoots.Add(key, rootObject.transform);

        for (int i = 0; i < initialCount; i++)
        {
            GameObject instance = CreateInstance(key);
            instance.SetActive(false);
            _pools[key].Enqueue(instance);
        }
    }

    public GameObject GetObject(string key)
    {
        Queue<GameObject> pool;
        if (_pools.TryGetValue(key, out pool) == false)
        {
            Debug.LogError($"[ObjectPoolManager] 풀 없음: {key}");
            return null;
        }

        GameObject instance;
        if (pool.Count > 0)
        {
            instance = pool.Dequeue();
        }
        else
        {
            instance = CreateInstance(key);
        }

        instance.SetActive(true);
        return instance;
    }

    public void ReturnObject(string key, GameObject instance)
    {
        Queue<GameObject> pool;
        if (_pools.TryGetValue(key, out pool) == false)
        {
            Debug.LogError($"[ObjectPoolManager] 풀 없음: {key}");
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(_poolRoots[key]);
        pool.Enqueue(instance);
    }

    private GameObject CreateInstance(string key)
    {
        GameObject instance = Instantiate(_prefabs[key], _poolRoots[key]);
        instance.name = key;
        return instance;
    }
}