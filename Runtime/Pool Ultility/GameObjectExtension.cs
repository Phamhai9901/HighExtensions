using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Extension methods cho GameObject – toàn bộ spawn/despawn đi qua PoolManager.
///
/// Component cache:
/// - GetCached&lt;T&gt; / SpawnAs&lt;T&gt; gọi GetComponent MỘT LẦN rồi cache mãi mãi.
/// - Cache KHÔNG bao giờ bị clear thủ công.
/// - Khi PoolManager destroy hẳn một object (không về pool), cache entry
///   của object đó được xoá tự động qua OnObjectPermanentlyDestroyed event.
/// - Object DontDestroyOnLoad / UI xuyên scene: cache luôn còn nguyên, zero ảnh hưởng.
/// </summary>
public static class GameObjectExtension
{
    // ─────────────────────────────────────────────────────────────────
    //  Component cache
    //  Key ngoài : InstanceID (int)  – không alloc
    //  Key trong : Type              – reference equality, O(1)
    // ─────────────────────────────────────────────────────────────────
    private static readonly Dictionary<int, Dictionary<Type, Component>> _componentCache = new();

    // ─────────────────────────────────────────────────────────────────
    //  Bootstrap – đăng ký cleanup tự động 1 lần duy nhất
    // ─────────────────────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // Khi PoolManager được tạo, đăng ký lắng nghe event destroy hẳn
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (PoolManager.Instance == null) return;

        // Tránh đăng ký nhiều lần (unsubscribe trước rồi subscribe lại)
        PoolManager.Instance.OnObjectPermanentlyDestroyed -= RemoveFromCache;
        PoolManager.Instance.OnObjectPermanentlyDestroyed += RemoveFromCache;
    }

    /// <summary>
    /// Xoá cache của đúng object bị destroy hẳn.
    /// Gọi tự động qua PoolManager.OnObjectPermanentlyDestroyed.
    /// </summary>
    private static void RemoveFromCache(GameObject obj)
    {
        if (obj == null) return;
        _componentCache.Remove(obj.GetInstanceID());
    }

    // ─────────────────────────────────────────────────────────────────
    //  GetCached<T>
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy component T từ cache – GetComponent chỉ gọi 1 lần đầu tiên.
    /// Mọi lần tiếp theo: O(1) Dictionary lookup, zero native call.
    /// Trả về null nếu object không có component T (null cũng được cache).
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
            comp = obj.GetComponent<T>(); // native call duy nhất
            typeMap[type] = comp;
        }

        return comp as T;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Spawn → GameObject  (API cũ giữ nguyên)
    // ─────────────────────────────────────────────────────────────────

    public static GameObject Spawn(
        this GameObject prefab,
        Vector3         worldPosition,
        Transform       parent)
        => PoolManager.Instance.Spawn(prefab, worldPosition, parent);

    public static GameObject Spawn(
        this GameObject prefab,
        Vector3         worldPosition,
        Quaternion      rotation,
        Transform       parent)
    {
        var obj = PoolManager.Instance.Spawn(prefab, worldPosition, parent);
        obj.transform.rotation = rotation;
        return obj;
    }

    public static GameObject Spawn(
        this GameObject prefab,
        Vector3         worldPosition,
        Vector3         scale,
        Transform       parent)
    {
        var obj = PoolManager.Instance.Spawn(prefab, worldPosition, parent);
        obj.transform.localScale = scale;
        return obj;
    }

    public static GameObject SpawnLocal(
        this GameObject prefab,
        Vector3         localPosition,
        Transform       parent)
    {
        var obj = PoolManager.Instance.Spawn(prefab, Vector3.zero, parent);
        obj.transform.localPosition = localPosition;
        return obj;
    }

    public static GameObject SpawnDelay(
        this GameObject prefab,
        Vector3         worldPosition,
        float           duration,
        Transform       parent)
        => PoolManager.Instance.SpawnDelay(prefab, worldPosition, duration, parent);

    // ─────────────────────────────────────────────────────────────────
    //  SpawnAs<T>  – spawn và trả về component T ngay, zero GetComponent
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawn prefab và trả về component <typeparamref name="T"/> ngay lập tức.
    /// Dùng component cache nên không tốn GetComponent sau lần đầu.
    /// Trả về <c>null</c> nếu prefab không có component T.
    /// <code>
    /// var vfx  = vfxPrefab.SpawnAs&lt;ParticleSystem&gt;(pos, parent);
    /// var unit = unitPrefab.SpawnAs&lt;EnemyCore&gt;(spawnPos, parent);
    /// unit?.Init(wave);
    /// </code>
    /// </summary>
    public static T SpawnAs<T>(
        this GameObject prefab,
        Vector3         worldPosition,
        Transform       parent) where T : Component
    {
        var obj = PoolManager.Instance.Spawn(prefab, worldPosition, parent);
        return obj.GetCached<T>();
    }

    /// <summary>SpawnAs với localPosition.</summary>
    public static T SpawnAsLocal<T>(
        this GameObject prefab,
        Vector3         localPosition,
        Transform       parent) where T : Component
    {
        var obj = PoolManager.Instance.Spawn(prefab, Vector3.zero, parent);
        obj.transform.localPosition = localPosition;
        return obj.GetCached<T>();
    }

    // ─────────────────────────────────────────────────────────────────
    //  UI Spawn
    // ─────────────────────────────────────────────────────────────────

    public static RectTransform UISpawn(
        this GameObject prefab,
        Vector2         anchoredPosition,
        Transform       parent)
    {
        var obj  = PoolManager.Instance.Spawn(prefab, Vector3.zero, parent);
        var rect = obj.GetCached<RectTransform>();
        if (rect) rect.anchoredPosition = anchoredPosition;
        return rect;
    }

    public static RectTransform UISpawn(
        this GameObject prefab,
        Vector3         anchoredPosition3D,
        Transform       parent)
    {
        var obj  = PoolManager.Instance.Spawn(prefab, Vector3.zero, parent);
        var rect = obj.GetCached<RectTransform>();
        if (rect) rect.anchoredPosition3D = anchoredPosition3D;
        return rect;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Despawn
    // ─────────────────────────────────────────────────────────────────

    public static void Despawn(this GameObject obj)
        => PoolManager.Instance.Kill(obj);

    public static void DespawnDelay(this GameObject obj, float duration)
        => PoolManager.Instance.KillDelay(obj, duration);
}
