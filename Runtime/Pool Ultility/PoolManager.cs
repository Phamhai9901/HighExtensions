using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton quản lý toàn bộ GameObject pool trong project.
///
/// Tối ưu:
/// - _ownerMap  : reverse-lookup → Kill O(1)
/// - key = prefab.name (string) → ổn định qua scene reload
/// - Delay dùng Coroutine → không alloc mỗi frame
/// - OnObjectPermanentlyDestroyed : event để GameObjectExtension tự dọn cache
///   chỉ khi object bị Destroy thật sự (không qua pool)
/// </summary>
public class PoolManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────
    public static PoolManager Instance { get; private set; }

    // ── Event – chỉ fire khi object bị Destroy hẳn, KHÔNG fire khi về pool ──
    /// <summary>
    /// Subscribe để nhận thông báo khi một object bị Destroy vĩnh viễn.
    /// GameObjectExtension dùng event này để dọn component cache đúng chỗ.
    /// </summary>
    public event Action<GameObject> OnObjectPermanentlyDestroyed;

    // ── Pools ─────────────────────────────────
    private readonly Dictionary<string, Pool>     _pools    = new();
    private readonly Dictionary<GameObject, Pool> _ownerMap = new();

    // ── Component pools (giữ API gốc) ─────────
    private readonly Dictionary<Type, List<DataPoolSpecial>> _componentPools = new();

    // ─────────────────────────────────────────
    //  Bootstrap
    // ─────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("[PoolManager]", typeof(PoolManager));
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────
    //  Spawn
    // ─────────────────────────────────────────

    public GameObject Spawn(
        GameObject prefab,
        Vector3    position,
        Transform  parent   = null,
        bool       isActive = true)
    {
        var pool = GetOrCreatePool(prefab);
        var obj  = pool.Spawn(position, parent: parent, isActive: isActive);
        _ownerMap[obj] = pool;
        return obj;
    }

    public GameObject SpawnDelay(
        GameObject prefab,
        Vector3    position,
        float      delay,
        Transform  parent = null)
    {
        var obj = Spawn(prefab, position, parent, isActive: false);
        StartCoroutine(ActivateAfter(obj, delay));
        return obj;
    }

    // ─────────────────────────────────────────
    //  Kill
    // ─────────────────────────────────────────

    /// <summary>Trả object về pool ngay lập tức. O(1).</summary>
    public void Kill(GameObject obj)
    {
        if (obj == null)     return;
        if (!obj.activeSelf) return;

        if (_ownerMap.TryGetValue(obj, out var pool))
        {
            _ownerMap.Remove(obj);
            pool.Kill(obj);
            // KHÔNG fire event – object vẫn sống trong pool
        }
        else
        {
            // Không thuộc pool nào → destroy thật sự
            OnObjectPermanentlyDestroyed?.Invoke(obj);
            Destroy(obj);
        }
    }

    /// <summary>Trả object về pool sau <paramref name="delay"/> giây.</summary>
    public void KillDelay(GameObject obj, float delay)
    {
        if (obj == null) return;
        StartCoroutine(KillAfter(obj, delay));
    }

    // ─────────────────────────────────────────
    //  Prewarm
    // ─────────────────────────────────────────

    public void Prewarm(GameObject prefab, int count)
    {
        var pool = GetOrCreatePool(prefab);
        pool.InitPool(count);
    }

    // ─────────────────────────────────────────
    //  Component pool (giữ API gốc)
    // ─────────────────────────────────────────

    public void AddListComponent(Type type, DataPoolSpecial data)
    {
        if (!_componentPools.TryGetValue(type, out var list))
        {
            list = new List<DataPoolSpecial>();
            _componentPools[type] = list;
        }
        list.Add(data);
    }

    // ─────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────

    private Pool GetOrCreatePool(GameObject prefab)
    {
        string key = prefab.name;
        if (!_pools.TryGetValue(key, out var pool))
        {
            pool = new Pool(prefab, 0);
            RegisterPool(key, pool);
        }
        return pool;
    }

    private void RegisterPool(string key, Pool pool)
    {
        _pools[key] = pool;
        var container = new GameObject(key);
        container.transform.SetParent(transform);
        pool.Transform = container.transform;
    }

    private IEnumerator KillAfter(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Kill(obj);
    }

    private static IEnumerator ActivateAfter(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null) obj.SetActive(true);
    }
}

// ── Data class (giữ API gốc) ──────────────────────────────────────────────────
public class DataPoolSpecial
{
    private GameObject _body;
    public  GameObject Body => _body;
    public  void SetBody(GameObject go) => _body = go;
}
