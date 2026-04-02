using UnityEngine;

/// <summary>
/// Component tiện dụng để spawn prefab từ Inspector hoặc Animation Event.
/// Gắn vào GameObject, kéo prefab vào Target rồi gọi Spawn() từ code hoặc event.
/// </summary>
public class QuickSpawn : MonoBehaviour
{
    [SerializeField] private GameObject _target;
    [SerializeField] private Transform  _parent;
    [SerializeField] private Transform  _targetPoint;

    [Header("Auto Despawn")]
    [SerializeField] private bool  _autoDespawn;
    [SerializeField] private float _despawnDelay = 2f;

    protected GameObject Current { get; private set; }

    // ── Spawn tại vị trí chính object này ──

    public virtual void Spawn()
    {
        Current = _target.Spawn(transform.position, _parent);
        TryAutoDespawn();
    }

    // ── Spawn tại TargetPoint ──

    public virtual void SpawnAtPoint()
    {
        if (_targetPoint == null)
        {
            Debug.LogWarning($"[QuickSpawn] TargetPoint chưa được gán trên {name}.");
            return;
        }
        Current = _target.Spawn(_targetPoint.position, _parent);
        TryAutoDespawn();
    }

    // ── Spawn tại position tuỳ ý ──

    public virtual void SpawnAt(Vector3 position)
    {
        Current = _target.Spawn(position, _parent);
        TryAutoDespawn();
    }

    // ── Despawn current ──

    public void DespawnCurrent()
    {
        if (Current != null)
            Current.Despawn();
    }

    // ── Internal ──

    private void TryAutoDespawn()
    {
        if (_autoDespawn && Current != null)
            Current.DespawnDelay(_despawnDelay);
    }
}
