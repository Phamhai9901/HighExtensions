using System;
using System.Collections.Generic;

namespace High
{
    /// <summary>
    /// Pool nhẹ cho List&lt;T&gt;.
    /// Bỏ lock() – Unity main thread only.
    /// Duplicate-check bằng HashSet chỉ ở Editor.
    ///
    /// Cách dùng:
    ///   var list = ListPool&lt;Enemy&gt;.Claim();
    ///   ListPool&lt;Enemy&gt;.Release(list);
    /// </summary>
    public static class ListPool<T>
    {
        private static readonly List<List<T>> _pool = new();
        private const int MaxCapacitySearch = 8;

#if UNITY_EDITOR
        private static readonly HashSet<List<T>> _inPool = new();
#endif

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
                { best = candidate; bestIndex = idx; }
            }

            if (best == null) return new List<T>(capacity);

            best.Capacity    = capacity;
            _pool[bestIndex] = _pool[_pool.Count - 1];
            _pool.RemoveAt(_pool.Count - 1);
#if UNITY_EDITOR
            _inPool.Remove(best);
#endif
            return best;
        }

        public static void Release(List<T> list)
        {
#if UNITY_EDITOR
            if (!_inPool.Add(list))
                throw new InvalidOperationException("[ListPool] Release 2 lần.");
#endif
            list.Clear();
            _pool.Add(list);
        }

        public static void Warmup(int count, int size)
        {
            var tmp = new List<T>[count];
            for (int i = 0; i < count; i++) tmp[i] = Claim(size);
            for (int i = 0; i < count; i++) Release(tmp[i]);
        }

        public static void Clear()
        {
#if UNITY_EDITOR
            _inPool.Clear();
#endif
            _pool.Clear();
        }

        public static int GetSize() => _pool.Count;
    }
}
