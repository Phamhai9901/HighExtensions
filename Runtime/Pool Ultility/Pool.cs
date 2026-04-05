using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý một pool của một loại prefab cụ thể.
///
/// Thứ tự lifecycle đúng khi tạo instance mới:
///   1. Instantiate (prefab có thể đang inactive)
///   2. SetActive(true)  → đảm bảo Awake + Start chạy đủ
///   3. OnCreated()      → component đã init xong, an toàn để gọi
///   4. OnDespawn()      → báo sắp vào pool
///   5. SetActive(false) → đưa vào pool
///
/// Thứ tự lifecycle khi Spawn:
///   1. SetParent + position/rotation/scale
///   2. SetActive(true)
///   3. OnSpawn()
///
/// Thứ tự lifecycle khi Kill:
///   1. OnDespawn()
///   2. ResetTransform
///   3. SetActive(false)
///   4. Push về stack
/// </summary>
public class Pool
{
    public GameObject Prefab { get; }
    public Transform Transform { get; set; }

    private readonly Stack<GameObject> _pooled = new();
    private readonly HashSet<GameObject> _active = new();
    private readonly Dictionary<GameObject, IPoolObject[]> _cache = new();

    private int _expandCount;

    public Pool(GameObject prefab, int initialSize = 0)
    {
        Prefab = prefab;
        _expandCount = initialSize;
    }

    // ── Prewarm ──────────────────────────────

    public void InitPool(int count = 0)
    {
        for (int i = 0; i < count; i++)
        {
            var instance = CreateFreshInstance(); // Awake/Start chạy + OnCreated() + về pool
            _pooled.Push(instance);
        }
    }

    // ── Spawn ─────────────────────────────────

    public GameObject Spawn(
        Vector3 position,
        Quaternion rotation = default,
        Vector3 scale = default,
        Transform parent = null,
        bool useLocalPosition = false,
        bool useLocalRotation = false,
        bool isActive = true)
    {
        if (_pooled.Count == 0)
            _pooled.Push(CreateFreshInstance());

        var obj = _pooled.Pop();

        // ── Transform trước khi bật active ──
        // SetParent lúc inactive tránh layout rebuild không cần thiết trên UI
        obj.transform.SetParent(parent);

        if (useLocalPosition) obj.transform.localPosition = position;
        else obj.transform.position = position;

        var rot = (rotation == default || rotation == Quaternion.identity)
            ? Prefab.transform.rotation : rotation;

        if (useLocalRotation) obj.transform.localRotation = rot;
        else obj.transform.rotation = rot;

        obj.transform.localScale = (scale == default) ? Vector3.one : scale;

        // ── Bật active rồi mới gọi OnSpawn ──
        SetActiveSafe(obj, isActive);
        _active.Add(obj);

        // OnSpawn chỉ gọi sau SetActive → component đã OnEnable
        if (isActive)
            InvokeEvent(obj, PoolEvent.Spawn);

        return obj;
    }

    // ── Kill ──────────────────────────────────

    public void Kill(GameObject obj)
    {
        if (!_active.Remove(obj))
        {
            Object.Destroy(obj);
            return;
        }

        // OnDespawn trước khi tắt → component còn active, có thể dọn state
        InvokeEvent(obj, PoolEvent.Despawn);
        ResetTransform(obj);
        SetActiveSafe(obj, false);
        _pooled.Push(obj);
    }

    // ── Query ─────────────────────────────────

    public bool Contains(GameObject obj) => _active.Contains(obj);
    public int ActiveCount => _active.Count;
    public int PooledCount => _pooled.Count;

    // ── Internal ──────────────────────────────

    private GameObject CreateFreshInstance()
    {
        var obj = Object.Instantiate(Prefab);
        obj.name = $"{Prefab.name}_{_expandCount++}";

        // ── Bước 1: bật active để Awake + Start chạy đủ ──
        // Dù prefab gốc có inactive, instance cần active để init
        SetActiveSafe(obj, true);

        // ── Bước 2: cache SAU khi Awake chạy ──
        // GetComponentsInChildren lúc này trả về đủ component đã init
        _cache[obj] = obj.GetComponentsInChildren<IPoolObject>(includeInactive: true);

        // ── Bước 3: OnCreated – component đã sẵn sàng ──
        InvokeEvent(obj, PoolEvent.Create);

        // ── Bước 4: OnDespawn + tắt → đưa vào pool ──
        InvokeEvent(obj, PoolEvent.Despawn);
        ResetTransform(obj);
        SetActiveSafe(obj, false);

        return obj;
    }

    private void ResetTransform(GameObject obj)
    {
        obj.transform.SetParent(Transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one;
        obj.transform.localEulerAngles = Vector3.zero;
    }

    private static void SetActiveSafe(GameObject obj, bool value)
    {
        if (obj.activeSelf != value) obj.SetActive(value);
    }

    private enum PoolEvent { Spawn, Despawn, Create }

    private void InvokeEvent(GameObject obj, PoolEvent ev)
    {
        if (!_cache.TryGetValue(obj, out var scripts)) return;
        switch (ev)
        {
            case PoolEvent.Spawn:
                foreach (var s in scripts) s.OnSpawn(); break;
            case PoolEvent.Despawn:
                foreach (var s in scripts) s.OnDespawn(); break;
            case PoolEvent.Create:
                foreach (var s in scripts) s.OnCreated(); break;
        }
    }
}
