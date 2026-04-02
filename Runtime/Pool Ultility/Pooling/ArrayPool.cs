using System;
using System.Collections.Generic;

/// <summary>
/// Pool nhẹ cho array T[] theo bucket power-of-2.
///
/// Tối ưu so với bản gốc:
/// - Bỏ lock() – main thread only; lock gây overhead không cần thiết trên mobile
/// - Giữ inPool HashSet ở Editor để phát hiện double-release
/// - Build release bỏ HashSet → Claim/Release nhanh hơn
///
/// Cách dùng:
///   var arr = ArrayPool&lt;int&gt;.Claim(10);   // sẽ trả về array dài ≥ 10 (power-of-2)
///   // ... dùng arr ...
///   ArrayPool&lt;int&gt;.Release(ref arr);       // arr = null sau dòng này
/// </summary>
public static class ArrayPool<T>
{
    // 31 bucket: bucket[i] chứa array có length = 2^i
    private static readonly Stack<T[]>[] _pool = new Stack<T[]>[31];

#if UNITY_EDITOR
    private static readonly HashSet<T[]> _inPool = new();
#endif

    // ── Claim ────────────────────────────────

    /// <summary>Trả array có length ít nhất <paramref name="minimumLength"/> (làm tròn lên power-of-2).</summary>
    public static T[] Claim(int minimumLength)
    {
        int bucket = BucketIndex(minimumLength);

        ref var slot = ref _pool[bucket];
        if (slot != null && slot.Count > 0)
        {
            var array = slot.Pop();
#if UNITY_EDITOR
            _inPool.Remove(array);
#endif
            return array;
        }

        return new T[1 << bucket];
    }

    // ── Release ───────────────────────────────

    /// <summary>
    /// Trả array về pool và set <paramref name="array"/> = null.
    /// KHÔNG dùng array sau khi Release.
    /// </summary>
    public static void Release(ref T[] array)
    {
        if (array == null) return;

#if UNITY_EDITOR
        if (!_inPool.Add(array))
            throw new InvalidOperationException(
                "[ArrayPool] Array được Release 2 lần.");

        // Kiểm tra length là power-of-2 (bắt buộc của bucket scheme)
        int bucket = BucketIndex(array.Length);
        if (array.Length != (1 << bucket))
            throw new ArgumentException(
                $"[ArrayPool] Length {array.Length} không phải power-of-2.");
#endif
        int b = BucketIndex(array.Length);
        _pool[b] ??= new Stack<T[]>();
        _pool[b].Push(array);

        array = null;
    }

    // ── Utility ───────────────────────────────

    public static void Clear()
    {
#if UNITY_EDITOR
        _inPool.Clear();
#endif
        for (int i = 0; i < _pool.Length; i++)
            _pool[i]?.Clear();
    }

    // ── Internal ──────────────────────────────

    private static int BucketIndex(int length)
    {
        int bucket = 0;
        while ((1 << bucket) < length && bucket < 30)
            bucket++;
        if (bucket >= 30)
            throw new ArgumentException($"[ArrayPool] Length quá lớn: {length}");
        return bucket;
    }
}
