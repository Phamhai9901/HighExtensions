using System;
using System.Collections.Generic;

/// <summary>
/// Pool nhẹ cho List&lt;T&gt; – tái dùng thay vì new List() mỗi lần.
///
/// Tối ưu so với bản gốc:
/// - Bỏ lock() – Unity chạy trên main thread; lock chỉ cần nếu dùng Job/Task
/// - Giữ duplicate-check (HashSet inPool) ở Editor để phát hiện lỗi sớm
/// - Build release (ASTAR_OPTIMIZE_POOLING) bỏ check → Release O(1)
///
/// Cách dùng:
///   var list = ListPool&lt;Enemy&gt;.Claim();
///   // ... dùng list ...
///   ListPool&lt;Enemy&gt;.Release(list); // KHÔNG dùng list sau dòng này
/// </summary>
public static class ListPool<T>
{
    private static readonly List<List<T>> _pool = new();

#if UNITY_EDITOR
    // Chỉ cần ở Editor để bắt lỗi pool 2 lần
    private static readonly HashSet<List<T>> _inPool = new();
#endif

    private const int MaxCapacitySearch = 8;

    // ── Claim ────────────────────────────────

    /// <summary>Lấy một List từ pool (hoặc tạo mới nếu pool rỗng).</summary>
    public static List<T> Claim()
    {
        if (_pool.Count > 0)
        {
            int last = _pool.Count - 1;
            var ls   = _pool[last];
            _pool.RemoveAt(last);
#if UNITY_EDITOR
            _inPool.Remove(ls);
#endif
            return ls;
        }
        return new List<T>();
    }

    /// <summary>
    /// Lấy một List có capacity ít nhất <paramref name="capacity"/>.
    /// Tìm trong pool trước; nếu không có thì tạo mới.
    /// </summary>
    public static List<T> Claim(int capacity)
    {
        List<T> best      = null;
        int     bestIndex = -1;

        int searchLen = Math.Min(_pool.Count, MaxCapacitySearch);
        for (int i = 0; i < searchLen; i++)
        {
            int idx       = _pool.Count - 1 - i;
            var candidate = _pool[idx];

            if (candidate.Capacity >= capacity)
            {
                _pool.RemoveAt(idx);
#if UNITY_EDITOR
                _inPool.Remove(candidate);
#endif
                return candidate;
            }

            if (best == null || candidate.Capacity > best.Capacity)
            {
                best      = candidate;
                bestIndex = idx;
            }
        }

        if (best == null)
            return new List<T>(capacity);

        // Expand capacity của list lớn nhất tìm thấy
        best.Capacity = capacity;
        // Swap với phần tử cuối để xoá hiệu quả
        _pool[bestIndex] = _pool[_pool.Count - 1];
        _pool.RemoveAt(_pool.Count - 1);
#if UNITY_EDITOR
        _inPool.Remove(best);
#endif
        return best;
    }

    // ── Release ───────────────────────────────

    /// <summary>
    /// Trả List về pool. KHÔNG dùng list sau khi Release.
    /// </summary>
    public static void Release(List<T> list)
    {
#if UNITY_EDITOR
        if (!_inPool.Add(list))
            throw new InvalidOperationException(
                "[ListPool] Đang pool một list lần thứ hai. Kiểm tra lại vòng đời.");
#endif
        list.Clear();
        _pool.Add(list);
    }

    // ── Warmup ───────────────────────────────

    /// <summary>Tạo trước <paramref name="count"/> list, mỗi list capacity <paramref name="size"/>.</summary>
    public static void Warmup(int count, int size)
    {
        var tmp = new List<T>[count];
        for (int i = 0; i < count; i++) tmp[i] = Claim(size);
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
