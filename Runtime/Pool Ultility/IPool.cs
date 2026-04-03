namespace High
{
    /// <summary>
    /// Generic pool interface – dùng cho các pool tự quản lý lifecycle.
    /// </summary>
    public interface IPool<T>
    {
        void Prewarm(int amount);
        T    Request();
        void Return(T obj);
    }

    /// <summary>
    /// Implement trên bất kỳ component nào cần nhận callback
    /// từ PoolManager khi được spawn / despawn / tạo mới.
    /// </summary>
    public interface IPoolObject
    {
        /// <summary>Gọi khi object được lấy ra khỏi pool.</summary>
        void OnSpawn();

        /// <summary>Gọi khi object được trả về pool.</summary>
        void OnDespawn();

        /// <summary>Gọi MỘT LẦN DUY NHẤT khi object được Instantiate.</summary>
        void OnCreated();
    }
}
