using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component gắn vào GameObject để phát audio từ Inspector.
/// KHÔNG cần viết code — chỉ cần kéo thả và config.
///
/// ╔═══════════════════════════════════════════════════════════╗
/// ║  USE CASES                                                ║
/// ║                                                           ║
/// ║  • Button click → gắn AudioTrigger, set key = "btn_click" ║
/// ║  • Enemy spawn → PlayOnEnable = true, key = "enemy_spawn" ║
/// ║  • Footstep → gọi Play() từ Animation Event               ║
/// ║  • Ambient loop → PlayOnEnable + Loop mode                 ║
/// ║  • Collision → PlayOnCollision = true                      ║
/// ╚═══════════════════════════════════════════════════════════╝
/// </summary>
public class AudioTrigger : MonoBehaviour
{
    [Header("Audio Key")]
    [Tooltip("Key trong AudioLibrary")]
    [SerializeField] string audioKey;

    [Header("Play Mode")]
    [SerializeField] SFXPlayMode playMode = SFXPlayMode.OneShot;
    [SerializeField] bool playOnEnable;
    [SerializeField] bool playOnDisable;
    [SerializeField] bool stopOnDisable = true;

    [Header("3D Settings")]
    [SerializeField] bool use3DPosition = true;

    [Header("Collision (Optional)")]
    [SerializeField] bool playOnCollision;
    [SerializeField] bool playOnTrigger;
    [SerializeField] LayerMask collisionLayers = ~0;

    AudioHandle currentHandle;

    void OnEnable()
    {
        if (playOnEnable) Play();
    }

    void OnDisable()
    {
        if (playOnDisable) Play();
        if (stopOnDisable) Stop();
    }

    // ═══════════════ PUBLIC API ═══════════════

    /// <summary>
    /// Phát audio với key đã config.
    /// Gọi từ: Button.onClick, Animation Event, UnityEvent, hoặc code.
    /// </summary>
    public void Play()
    {
        if (string.IsNullOrEmpty(audioKey)) return;

        Vector3 pos = use3DPosition ? transform.position : Vector3.zero;
        currentHandle = AudioManager.Play(audioKey, pos, playMode);
    }

    /// <summary>Phát audio với key khác (override lúc runtime).</summary>
    public void PlayKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        Vector3 pos = use3DPosition ? transform.position : Vector3.zero;
        currentHandle = AudioManager.Play(key, pos, playMode);
    }

    /// <summary>Dừng audio đang phát.</summary>
    public void Stop()
    {
        currentHandle?.Stop();
        currentHandle = null;
    }

    /// <summary>Fade out rồi dừng.</summary>
    public void FadeOut(float duration = 1f)
    {
        currentHandle?.FadeOut(duration);
    }

    // ═══════════════ COLLISION ═══════════════

    void OnCollisionEnter(Collision collision)
    {
        if (playOnCollision && IsInLayer(collision.gameObject.layer))
            Play();
    }

    void OnTriggerEnter(Collider other)
    {
        if (playOnTrigger && IsInLayer(other.gameObject.layer))
            Play();
    }

    bool IsInLayer(int layer)
    {
        return (collisionLayers & (1 << layer)) != 0;
    }
}
