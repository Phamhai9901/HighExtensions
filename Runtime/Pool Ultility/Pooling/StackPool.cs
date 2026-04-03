using System.Collections.Generic;

namespace High
{
    /// <summary>
    /// Pool nhẹ cho Stack&lt;T&gt;.
    /// Duplicate-check O(1) bằng HashSet chỉ ở Editor.
    ///
    /// Cách dùng:
    ///   var stack = StackPool&lt;Node&gt;.Claim();
    ///   StackPool&lt;Node&gt;.Release(stack);
    /// </summary>
    public static class StackPool<T>
    {
        private static readonly List<Stack<T>> _pool = new();

#if UNITY_EDITOR
        private static readonly HashSet<Stack<T>> _inPool = new();
#endif

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

        public static void Release(Stack<T> stack)
        {
#if UNITY_EDITOR
            if (!_inPool.Add(stack))
            {
                UnityEngine.Debug.LogError("[StackPool] Release 2 lần.");
                return;
            }
#endif
            stack.Clear();
            _pool.Add(stack);
        }

        public static void Warmup(int count)
        {
            var tmp = new Stack<T>[count];
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
