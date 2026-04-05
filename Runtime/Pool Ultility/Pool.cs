using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý một pool của một loại prefab cụ thể.
///
/// Thứ tự Spawn (quan trọng cho Unity UI):
///   1. SetActive(true)          ← TRƯỚC SetParent
///   2. SetParent(parent)        ← Canvas rebuild xảy ra SAU OnEnable
///   3. SetPosition/Rotation/Scale
///   4. OnSpawn()
///
/// Lý do: nếu SetParent trước khi SetActive trên Canvas hierarchy,
/// các Behaviour component con không được enable đúng cách.
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
            _pooled.Push(CreateFreshInstance());
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
        _active.Add(obj);

        if (isActive)
            ActivateSpawned(obj, parent, position, rotation, scale,
                            useLocalPosition, useLocalRotation);
        else
            ApplyTransform(obj, parent, position, rotation, scale,
                           useLocalPosition, useLocalRotation);

        return obj;
    }

    /// <summary>
    /// Bật active + apply transform + OnSpawn.
    /// Thứ tự: SetActive TRƯỚC SetParent để UI component enable đúng.
    /// </summary>
    public void ActivateSpawned(
        GameObject obj,
        Transform parent = null,
        Vector3 position = default,
        Quaternion rotation = default,
        Vector3 scale = default,
        bool useLocalPosition = false,
        bool useLocalRotation = false)
    {
        // ── 1. SetActive(true) TRƯỚC SetParent ──
        SetActiveSafe(obj, true);

        // ── 2. SetParent sau khi component đã OnEnable ──
        ApplyTransform(obj, parent, position, rotation, scale,
                       useLocalPosition, useLocalRotation);

        // ── 3. OnSpawn: component active + transform đúng chỗ ──
        InvokeEvent(obj, PoolEvent.Spawn);
    }

    // ── Kill ──────────────────────────────────

    public void Kill(GameObject obj)
    {
        if (!_active.Remove(obj))
        {
            Object.Destroy(obj);
            return;
        }

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

        SetActiveSafe(obj, true);

        _cache[obj] = obj.GetComponentsInChildren<IPoolObject>(includeInactive: true);

        InvokeEvent(obj, PoolEvent.Create);

        InvokeEvent(obj, PoolEvent.Despawn);
        ResetTransform(obj);
        SetActiveSafe(obj, false);

        return obj;
    }

    private static void ApplyTransform(
        GameObject obj,
        Transform parent,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        bool useLocalPosition,
        bool useLocalRotation)
    {
        obj.transform.SetParent(parent);

        if (useLocalPosition) obj.transform.localPosition = position;
        else obj.transform.position = position;

        var rot = (rotation == default || rotation == Quaternion.identity)
            ? obj.transform.rotation : rotation;

        if (useLocalRotation) obj.transform.localRotation = rot;
        else obj.transform.rotation = rot;

        obj.transform.localScale = (scale == default) ? Vector3.one : scale;
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
