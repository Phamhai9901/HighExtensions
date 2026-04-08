using UnityEngine;

/// <summary>
/// Gắn vào Trigger Collider — khi player vào zone, đổi music.
/// Hỗ trợ cả single track (musicKey) lẫn playlist.
///
/// Setup:
/// 1. Tạo GameObject + Collider (isTrigger = true)
/// 2. Gắn MusicZone
/// 3. Assign playlist HOẶC set musicKey
/// 4. Set playerLayer
///
/// Ưu tiên: Playlist > musicKey
/// </summary>
[RequireComponent(typeof(Collider))]
public class MusicZone : MonoBehaviour
{
    [Header("Music Source (chọn 1)")]
    [Tooltip("Ưu tiên playlist. Để trống nếu dùng single key.")]
    [SerializeField] MusicPlaylist playlist;

    [Tooltip("Dùng nếu không có playlist.")]
    [SerializeField] string musicKey;

    [Header("Transition")]
    [SerializeField] float crossfadeDuration = 2f;

    [Header("On Exit")]
    [Tooltip("Playlist hoặc key phát khi rời zone. Để trống = không đổi.")]
    [SerializeField] MusicPlaylist exitPlaylist;
    [SerializeField] string exitMusicKey;

    [Header("Filter")]
    [SerializeField] LayerMask playerLayer;

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        if (playlist != null)
            AudioManager.SwitchPlaylist(playlist, crossfadeDuration);
        else if (!string.IsNullOrEmpty(musicKey))
            AudioManager.PlayMusic(musicKey, crossfadeDuration);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        if (exitPlaylist != null)
            AudioManager.SwitchPlaylist(exitPlaylist, crossfadeDuration);
        else if (!string.IsNullOrEmpty(exitMusicKey))
            AudioManager.PlayMusic(exitMusicKey, crossfadeDuration);
    }

    bool IsPlayer(Collider col)
    {
        return (playerLayer & (1 << col.gameObject.layer)) != 0;
    }
}
