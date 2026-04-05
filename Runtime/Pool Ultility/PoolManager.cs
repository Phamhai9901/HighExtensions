using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    public event Action<GameObject> OnObjectPermanentlyDestroyed;

    private readonly Dictionary<string, Pool> _pools = new();
    private readonly Dictionary<GameObject, Pool> _ownerMap = new();
    private readonly Dictionary<Type, List<DataPoolSpecial>> _componentPools = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("[PoolManager]", typeof(PoolManager));
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Spawn ─────────────────────────────────

    public GameObject Spawn(
        GameObject prefab,
        Vector3 position,
        Transform parent = null,
        bool isActive = true)
    {
        var pool = GetOrCreatePool(prefab);
        var obj = pool.Spawn(position, parent: parent, isActive: isActive);
        _ownerMap[obj] = pool;
        return obj;
    }

    /// <summary>
    /// Spawn inactive, sau delay mới gọi pool.ActivateSpawned()
    /// để đảm bảo OnSpawn() và SetActive đúng thứ tự.
    /// </summary>
    public GameObject SpawnDelay(
        GameObject prefab,
        Vector3 position,
        float delay,
        Transform parent = null)
    {
        var pool = GetOrCreatePool(prefab);
        var obj = pool.Spawn(position, parent: parent, isActive: false);
        _ownerMap[obj] = pool;
        StartCoroutine(ActivateAfter(pool, obj, parent, position, delay));
        return obj;
    }

    // ── Kill ──────────────────────────────────

    public void Kill(GameObject obj)
    {
        if (obj == null || !obj.activeSelf) return;

        if (_ownerMap.TryGetValue(obj, out var pool))
        {
            _ownerMap.Remove(obj);
            pool.Kill(obj);
        }
        else
        {
            OnObjectPermanentlyDestroyed?.Invoke(obj);
            Destroy(obj);
        }
    }

    public void KillDelay(GameObject obj, float delay)
    {
        if (obj == null) return;
        StartCoroutine(KillAfter(obj, delay));
    }

    // ── Prewarm ───────────────────────────────

    public void Prewarm(GameObject prefab, int count)
        => GetOrCreatePool(prefab).InitPool(count);

    // ── Component pool ────────────────────────

    public void AddListComponent(Type type, DataPoolSpecial data)
    {
        if (!_componentPools.TryGetValue(type, out var list))
        {
            list = new List<DataPoolSpecial>();
            _componentPools[type] = list;
        }
        list.Add(data);
    }

    // ── Internal ──────────────────────────────

    private Pool GetOrCreatePool(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab.name, out var pool))
        {
            pool = new Pool(prefab);
            _pools[prefab.name] = pool;
            var container = new GameObject(prefab.name);
            container.transform.SetParent(transform);
            pool.Transform = container.transform;
        }
        return pool;
    }

    private IEnumerator KillAfter(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Kill(obj);
    }

    private static IEnumerator ActivateAfter(
        Pool pool,
        GameObject obj,
        Transform parent,
        Vector3 position,
        float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
            pool.ActivateSpawned(obj, parent, position);
    }
}

public class DataPoolSpecial
{
    private GameObject _body;
    public GameObject Body => _body;
    public void SetBody(GameObject go) => _body = go;
}
