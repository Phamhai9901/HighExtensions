using System.Collections.Generic;

/// <summary>
/// Interface bắt buộc cho object được quản lý bởi <see cref="ObjectPool{T}"/>.
/// </summary>
public interface IAstarPooledObject
{
    /// <summary>Gọi khi object được trả về pool – dọn sạch state bên trong.</summary>
    void OnEnterPool();
}

/// <summary>
/// Pool nhẹ cho plain C# object (không phải GameObject).
///
/// Tối ưu so với bản gốc:
/// - Release gốc scan O(n) để check duplicate → thay bằng HashSet O(1) ở Editor
/// - Build release: Release = O(1) hoàn toàn
///
/// Cách dùng:
///   var node = ObjectPool&lt;PathNode&gt;.Claim();
///   // ... dùng node ...
///   ObjectPool&lt;PathNode&gt;.Release(node);
/// </summary>
public static class ObjectPool<T> where T : class, IAstarPooledObject, new()
{
    private static readonly List<T> _pool = new();

#if UNITY_EDITOR
    private static readonly HashSet<T> _inPool = new();
#endif

    // ── Claim ────────────────────────────────

    public static T Claim()
    {
        if (_pool.Count > 0)
        {
            int last = _pool.Count - 1;
            var obj  = _pool[last];
            _pool.RemoveAt(last);
#if UNITY_EDITOR
            _inPool.Remove(obj);
#endif
            return obj;
        }
        return new T();
    }

    // ── Release ───────────────────────────────

    public static void Release(T obj)
    {
        if (obj == null) return;

#if UNITY_EDITOR
        if (!_inPool.Add(obj))
            throw new System.InvalidOperationException(
                "[ObjectPool] Object được Release 2 lần. Kiểm tra vòng đời.");
#endif
        obj.OnEnterPool();
        _pool.Add(obj);
    }

    // ── Warmup ───────────────────────────────

    public static void Warmup(int count)
    {
        var tmp = new T[count];
        for (int i = 0; i < count; i++) tmp[i] = Claim();
        for (int i = 0; i < count; i++) Release(tmp[i]);
    }

    // ── Utility ───────────────────────────────

    public static void Clear()
    {
#if UNITY_EDITOR
        _inPool.Clear();
#endif
        _pool.Clear();
    }

    public static int GetSize() => _pool.Count;
}
