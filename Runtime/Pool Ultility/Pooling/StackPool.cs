using System.Collections.Generic;

/// <summary>
/// Pool nhẹ cho Stack&lt;T&gt;.
///
/// Tối ưu so với bản gốc:
/// - Release gốc scan toàn bộ pool O(n) để check duplicate → thay bằng
///   HashSet ở Editor: O(1) check, zero overhead ở build release
///
/// Cách dùng:
///   var stack = StackPool&lt;Node&gt;.Claim();
///   // ... dùng stack ...
///   StackPool&lt;Node&gt;.Release(stack);
/// </summary>
public static class StackPool<T>
{
    private static readonly List<Stack<T>> _pool = new();

#if UNITY_EDITOR
    private static readonly HashSet<Stack<T>> _inPool = new();
#endif

    // ── Claim ────────────────────────────────

    public static Stack<T> Claim()
    {
        if (_pool.Count > 0)
        {
            int last = _pool.Count - 1;
            var s    = _pool[last];
            _pool.RemoveAt(last);
#if UNITY_EDITOR
            _inPool.Remove(s);
#endif
            return s;
        }
        return new Stack<T>();
    }

    // ── Release ───────────────────────────────

    public static void Release(Stack<T> stack)
    {
#if UNITY_EDITOR
        if (!_inPool.Add(stack))
        {
            UnityEngine.Debug.LogError(
                "[StackPool] Stack được Release 2 lần. Kiểm tra vòng đời.");
            return;
        }
#endif
        stack.Clear();
        _pool.Add(stack);
    }

    // ── Warmup ───────────────────────────────

    public static void Warmup(int count)
    {
        var tmp = new Stack<T>[count];
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
