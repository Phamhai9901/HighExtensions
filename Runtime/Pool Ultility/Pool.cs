using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý một pool của một loại prefab cụ thể.
///
/// Tối ưu so với bản gốc:
/// - activeSet  : HashSet thay List → Contains/Remove O(1) thay O(n)
/// - _componentCache : cache IPoolObject[] mỗi instance → không alloc
///   GetComponentsInChildren mỗi Spawn/Kill
/// - Bỏ IsResponsibleForObject (logic chuyển lên PoolManager._ownerMap)
/// - Rotation default dùng Quaternion.Equals thay eulerAngles so sánh
/// </summary>
public class Pool
{
    // ── Identity ──────────────────────────────
    public GameObject Prefab       { get; }
    public Transform  Transform    { get; set; }

    // ── Storage ───────────────────────────────
    private readonly Stack<GameObject>                      _pooled  = new();
    private readonly HashSet<GameObject>                    _active  = new();
    private readonly Dictionary<GameObject, IPoolObject[]> _cache   = new();

    private int _expandCount; // dùng để đặt tên instance mở rộng

    // ── Ctor ──────────────────────────────────
    public Pool(GameObject prefab, int initialSize = 0)
    {
        Prefab           = prefab;
        _expandCount     = initialSize; // bắt đầu đặt tên từ sau số prewarm
    }

    // ─────────────────────────────────────────
    //  Init / Prewarm
    // ─────────────────────────────────────────

    /// <summary>Tạo trước <paramref name="count"/> instance và đưa vào pool.</summary>
    public void InitPool(int count = 0)
    {
        for (int i = 0; i < count; i++)
        {
            var instance = CreateFreshInstance();
            ResetTransform(instance);
            instance.SetActive(false);
            _pooled.Push(instance);
        }
    }

    // ─────────────────────────────────────────
    //  Spawn
    // ─────────────────────────────────────────

    public GameObject Spawn(
        Vector3   position,
        Quaternion rotation        = default,
        Vector3   scale            = default,
        Transform  parent          = null,
        bool       useLocalPosition = false,
        bool       useLocalRotation = false,
        bool       isActive         = true)
    {
        if (_pooled.Count == 0)
        {
            // Pool cạn → mở rộng tự động (không spike GC vì chỉ tạo 1)
            var fresh = CreateFreshInstance();
            _pooled.Push(fresh);
        }

        var obj = _pooled.Pop();

        // ── Transform ──
        obj.transform.SetParent(parent);

        if (useLocalPosition) obj.transform.localPosition = position;
        else                  obj.transform.position      = position;

        var targetRotation = (rotation == default || rotation == Quaternion.identity)
            ? Prefab.transform.rotation
            : rotation;

        if (useLocalRotation) obj.transform.localRotation = targetRotation;
        else                  obj.transform.rotation      = targetRotation;

        obj.transform.localScale = (scale == default) ? Vector3.one : scale;

        // ── Activate ──
        SetActiveSafe(obj, isActive);
        _active.Add(obj);

        InvokeEvent(obj, PoolEvent.Spawn);
        return obj;
    }

    // ─────────────────────────────────────────
    //  Kill (trả về pool)
    // ─────────────────────────────────────────

    /// <summary>
    /// Trả <paramref name="obj"/> về pool.
    /// Nếu object không thuộc pool này, Destroy nó.
    /// </summary>
    public void Kill(GameObject obj)
    {
        if (!_active.Remove(obj))
        {
            // Không phải của pool này → destroy an toàn
            Object.Destroy(obj);
            return;
        }

        InvokeEvent(obj, PoolEvent.Despawn);
        ResetTransform(obj);
        SetActiveSafe(obj, false);
        _pooled.Push(obj);
    }

    // ─────────────────────────────────────────
    //  Query
    // ─────────────────────────────────────────

    /// <summary>Kiểm tra O(1) xem pool này có chịu trách nhiệm obj không.</summary>
    public bool Contains(GameObject obj) => _active.Contains(obj);

    public int ActiveCount  => _active.Count;
    public int PooledCount  => _pooled.Count;

    // ─────────────────────────────────────────
    //  Internal helpers
    // ─────────────────────────────────────────

    private GameObject CreateFreshInstance()
    {
        var obj  = Object.Instantiate(Prefab);
        obj.name = $"{Prefab.name}_{_expandCount++}";

        // Cache component array MỘT LẦN → tái dùng mãi mãi
        _cache[obj] = obj.GetComponentsInChildren<IPoolObject>(includeInactive: true);

        InvokeEvent(obj, PoolEvent.Create);
        return obj;
    }

    private void ResetTransform(GameObject obj)
    {
        obj.transform.SetParent(Transform);
        obj.transform.localPosition    = Vector3.zero;
        obj.transform.localScale       = Vector3.one;
        obj.transform.localEulerAngles = Vector3.zero;
    }

    private static void SetActiveSafe(GameObject obj, bool value)
    {
        if (obj.activeSelf != value)
            obj.SetActive(value);
    }

    // ── Event dispatch (dùng cached array, zero alloc) ──

    private enum PoolEvent { Spawn, Despawn, Create }

    private void InvokeEvent(GameObject obj, PoolEvent ev)
    {
        if (!_cache.TryGetValue(obj, out var scripts)) return;

        switch (ev)
        {
            case PoolEvent.Spawn:
                foreach (var s in scripts) s.OnSpawn();
                break;
            case PoolEvent.Despawn:
                foreach (var s in scripts) s.OnDespawn();
                break;
            case PoolEvent.Create:
                foreach (var s in scripts) s.OnCreated();
                break;
        }
    }
}
