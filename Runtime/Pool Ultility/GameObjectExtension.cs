using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Extension methods cho GameObject – spawn/despawn qua PoolManager.
///
/// Component cache:
/// - GetCached&lt;T&gt; / SpawnAs&lt;T&gt; gọi GetComponent MỘT LẦN rồi cache mãi mãi.
/// - Tự xoá cache khi object bị Destroy hẳn (qua OnObjectPermanentlyDestroyed).
/// - DontDestroyOnLoad / UI xuyên scene: cache không bao giờ bị xoá nhầm.
/// </summary>
public static class GameObjectExtension
{
    // ── Component cache ───────────────────────
    // Key ngoài : InstanceID (int) – không alloc
    // Key trong : Type            – reference equality O(1)
    private static readonly Dictionary<int, Dictionary<Type, Component>> _componentCache = new();

    // ── Bootstrap ────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
        => SceneManager.sceneLoaded += OnSceneLoaded;

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (PoolManager.Instance == null) return;
        PoolManager.Instance.OnObjectPermanentlyDestroyed -= RemoveFromCache;
        PoolManager.Instance.OnObjectPermanentlyDestroyed += RemoveFromCache;
    }

    private static void RemoveFromCache(GameObject obj)
    {
        if (obj != null) _componentCache.Remove(obj.GetInstanceID());
    }

    // ── GetCached<T> ─────────────────────────

    /// <summary>
    /// Lấy component T từ cache – GetComponent chỉ gọi 1 lần đầu tiên.
    /// Mọi lần tiếp theo: O(1) Dictionary lookup, zero native call.
    /// Trả về null nếu object không có component T.
    /// </summary>
    public static T GetCached<T>(this GameObject obj) where T : Component
    {
        int id = obj.GetInstanceID();

        if (!_componentCache.TryGetValue(id, out var typeMap))
        {
            typeMap = new Dictionary<Type, Component>();
            _componentCache[id] = typeMap;
        }

        var type = typeof(T);
        if (!typeMap.TryGetValue(type, out var comp))
        {
            comp = obj.GetComponent<T>();
            typeMap[type] = comp;
        }

        return comp as T;
    }

    // ── Spawn → GameObject ────────────────────

    public static GameObject Spawn(
        this GameObject prefab,
        Vector3 worldPosition,
        Transform parent)
        => PoolManager.Instance.Spawn(prefab, worldPosition, parent);

    public static GameObject Spawn(
        this GameObject prefab,
        Vector3 worldPosition,
        Quaternion rotation,
        Transform parent)
    {
        var obj = PoolManager.Instance.Spawn(prefab, worldPosition, parent);
        obj.transform.rotation = rotation;
        return obj;
    }

    public static GameObject Spawn(
        this GameObject prefab,
        Vector3 worldPosition,
        Vector3 scale,
        Transform parent)
    {
        var obj = PoolManager.Instance.Spawn(prefab, worldPosition, parent);
        obj.transform.localScale = scale;
        return obj;
    }

    public static GameObject SpawnLocal(
        this GameObject prefab,
        Vector3 localPosition,
        Transform parent)
    {
        var obj = PoolManager.Instance.Spawn(prefab, Vector3.zero, parent);
        obj.transform.localPosition = localPosition;
        return obj;
    }

    public static GameObject SpawnDelay(
        this GameObject prefab,
        Vector3 worldPosition,
        float duration,
        Transform parent)
        => PoolManager.Instance.SpawnDelay(prefab, worldPosition, duration, parent);

    // ── SpawnAs<T> ────────────────────────────

    /// <summary>
    /// Spawn prefab và trả về component T ngay lập tức.
    /// Zero GetComponent sau lần đầu nhờ component cache.
    /// <code>
    /// var fx    = fxPrefab.SpawnAs&lt;ParticleSystem&gt;(pos, parent);
    /// var enemy = enemyPrefab.SpawnAs&lt;EnemyCore&gt;(pos, parent);
    /// enemy?.Init(wave);
    /// </code>
    /// </summary>
    public static T SpawnAs<T>(
        this GameObject prefab,
        Vector3 worldPosition,
        Transform parent) where T : Component
    {
        var obj = PoolManager.Instance.Spawn(prefab, worldPosition, parent);
        return obj.GetCached<T>();
    }

    public static T SpawnAsLocal<T>(
        this GameObject prefab,
        Vector3 localPosition,
        Transform parent) where T : Component
    {
        var obj = PoolManager.Instance.Spawn(prefab, Vector3.zero, parent);
        obj.transform.localPosition = localPosition;
        return obj.GetCached<T>();
    }

    // ── UI Spawn ──────────────────────────────

    public static RectTransform UISpawn(
        this GameObject prefab,
        Vector2 anchoredPosition,
        Transform parent)
    {
        var obj = PoolManager.Instance.Spawn(prefab, Vector3.zero, parent);
        var rect = obj.GetCached<RectTransform>();
        if (rect) rect.anchoredPosition = anchoredPosition;
        return rect;
    }

    public static RectTransform UISpawn(
        this GameObject prefab,
        Vector3 anchoredPosition3D,
        Transform parent)
    {
        var obj = PoolManager.Instance.Spawn(prefab, Vector3.zero, parent);
        var rect = obj.GetCached<RectTransform>();
        if (rect) rect.anchoredPosition3D = anchoredPosition3D;
        return rect;
    }

    // ── Despawn ───────────────────────────────

    public static void Despawn(this GameObject obj)
        => PoolManager.Instance.Kill(obj);

    public static void DespawnDelay(this GameObject obj, float duration)
        => PoolManager.Instance.KillDelay(obj, duration);
}
