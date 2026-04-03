using System;
using System.Collections.Generic;

namespace High
{
    /// <summary>
    /// Pool nhẹ cho T[] theo bucket power-of-2.
    /// Bỏ lock() – main thread only.
    /// Duplicate-check bằng HashSet chỉ ở Editor.
    ///
    /// Cách dùng:
    ///   var hits  = ArrayPool&lt;Collider&gt;.Claim(32);
    ///   int count = Physics.OverlapSphereNonAlloc(pos, radius, hits, mask);
    ///   ArrayPool&lt;Collider&gt;.Release(ref hits);
    /// </summary>
    public static class ArrayPool<T>
    {
        private static readonly Stack<T[]>[] _pool = new Stack<T[]>[31];

#if UNITY_EDITOR
        private static readonly HashSet<T[]> _inPool = new();
#endif

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

        public static void Release(ref T[] array)
        {
            if (array == null) return;

#if UNITY_EDITOR
            if (!_inPool.Add(array))
                throw new InvalidOperationException("[ArrayPool] Release 2 lần.");

            int check = BucketIndex(array.Length);
            if (array.Length != (1 << check))
                throw new ArgumentException("[ArrayPool] Length không phải power-of-2.");
#endif
            int b = BucketIndex(array.Length);
            _pool[b] ??= new Stack<T[]>();
            _pool[b].Push(array);
            array = null;
        }

        public static void Clear()
        {
#if UNITY_EDITOR
            _inPool.Clear();
#endif
            for (int i = 0; i < _pool.Length; i++) _pool[i]?.Clear();
        }

        private static int BucketIndex(int length)
        {
            int b = 0;
            while ((1 << b) < length && b < 30) b++;
            if (b >= 30) throw new ArgumentException($"[ArrayPool] Length quá lớn: {length}");
            return b;
        }
    }
}
