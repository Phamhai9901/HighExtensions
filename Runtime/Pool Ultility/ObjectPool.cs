using System.Collections.Generic;

namespace High
{
    /// <summary>Interface bắt buộc cho object dùng với ObjectPool&lt;T&gt;.</summary>
    public interface IAstarPooledObject
    {
        void OnEnterPool();
    }

    /// <summary>
    /// Pool nhẹ cho plain C# object (không phải GameObject).
    /// Duplicate-check O(1) bằng HashSet chỉ ở Editor.
    ///
    /// Cách dùng:
    ///   var node = ObjectPool&lt;PathNode&gt;.Claim();
    ///   ObjectPool&lt;PathNode&gt;.Release(node);
    /// </summary>
    public static class ObjectPool<T> where T : class, IAstarPooledObject, new()
    {
        private static readonly List<T> _pool = new();

#if UNITY_EDITOR
        private static readonly HashSet<T> _inPool = new();
#endif

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

        public static void Release(T obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!_inPool.Add(obj))
                throw new System.InvalidOperationException("[ObjectPool] Release 2 lần.");
#endif
            obj.OnEnterPool();
            _pool.Add(obj);
        }

        public static void Warmup(int count)
        {
            var tmp = new T[count];
            for (int i = 0; i < count; i++) tmp[i] = Claim();
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
