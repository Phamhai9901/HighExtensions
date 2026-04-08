using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Runtime player cho MusicPlaylist — gắn vào scene hoặc dùng qua AudioManager.
///
/// ╔══════════════════════════════════════════════════════════════════╗
/// ║  USAGE                                                          ║
/// ║                                                                  ║
/// ║  // Cách 1: Gắn component vào scene, assign playlist Inspector   ║
/// ║  // → Tự chạy khi scene load (autoPlay = true)                   ║
/// ║                                                                  ║
/// ║  // Cách 2: Gọi từ code                                          ║
/// ║  AudioManager.PlayPlaylist(gameplayPlaylist);                     ║
/// ║  AudioManager.SwitchPlaylist(bossPlaylist);   // crossfade sang   ║
/// ║  AudioManager.StopPlaylist(2f);               // fade out          ║
/// ║                                                                  ║
/// ║  // Cách 3: Chuyển intensity trong cùng scene                     ║
/// ║  AudioManager.SwitchPlaylist(intensePlaylist); // seamless switch  ║
/// ║                                                                  ║
/// ║  // Control                                                       ║
/// ║  AudioManager.SkipTrack();                                        ║
/// ║  AudioManager.PausePlaylist() / ResumePlaylist()                  ║
/// ╚══════════════════════════════════════════════════════════════════╝
///
/// WORKFLOW MÀN CHƠI:
/// 
///   Scene "Level_01":
///     GameObject "MusicController"
///       └─ MusicPlaylistPlayer
///            playlist = "Gameplay_Chill"   (5 bài shuffle)
///            autoPlay = true
///
///   Khi gặp boss → code gọi:
///     AudioManager.SwitchPlaylist(bossPlaylist);
///
///   Khi hạ boss → code gọi:
///     AudioManager.SwitchPlaylist(victoryPlaylist);
/// </summary>
public class MusicPlaylistPlayer : MonoBehaviour
{
    [Header("Playlist")]
    [SerializeField] MusicPlaylist playlist;
    [SerializeField] bool autoPlay = true;

    [Header("Audio Sources")]
    [Tooltip("Để trống = tự tạo 2 AudioSource cho crossfade")]
    [SerializeField] AudioMixerGroup musicMixerGroup;

    // ═══════════════ STATE ═══════════════

    AudioSource sourceA;
    AudioSource sourceB;
    AudioSource activeSource;

    MusicPlaylist currentPlaylist;
    List<int> playOrder = new();
    int currentIndex = -1;

    bool isPlaying;
    bool isPaused;

    Coroutine playbackRoutine;
    Coroutine crossfadeRoutine;

    // Shuffle history — tránh lặp bài vừa phát
    int lastPlayedIndex = -1;

    // ═══════════════ EVENTS ═══════════════

    /// <summary>Khi bắt đầu phát track mới.</summary>
    public event Action<MusicTrack, int> OnTrackChanged;

    /// <summary>Khi playlist kết thúc (nếu loop = false).</summary>
    public event Action OnPlaylistEnded;

    /// <summary>Track đang phát hiện tại.</summary>
    public MusicTrack CurrentTrack =>
        currentPlaylist != null && currentIndex >= 0 && currentIndex < currentPlaylist.Tracks.Count
            ? currentPlaylist.Tracks[playOrder[currentIndex]]
            : null;

    /// <summary>Index track hiện tại trong play order.</summary>
    public int CurrentOrderIndex => currentIndex;

    /// <summary>Tổng số track.</summary>
    public int TrackCount => currentPlaylist?.Tracks.Count ?? 0;

    public bool IsPlaying => isPlaying;
    public bool IsPaused => isPaused;

    // ═══════════════ LIFECYCLE ═══════════════

    void Awake()
    {
        CreateSources();
    }

    void Start()
    {
        if (autoPlay && playlist != null)
            Play(playlist);
    }

    void OnDestroy()
    {
        Stop(0f);
    }

    void CreateSources()
    {
        sourceA = CreateChildSource("PlaylistSource_A");
        sourceB = CreateChildSource("PlaylistSource_B");
        activeSource = sourceA;
    }

    AudioSource CreateChildSource(string label)
    {
        var go = new GameObject(label);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false; // Playlist tự quản lý loop
        if (musicMixerGroup != null)
            src.outputAudioMixerGroup = musicMixerGroup;
        return src;
    }

    // ═══════════════ PUBLIC API ═══════════════

    /// <summary>Bắt đầu phát playlist.</summary>
    public void Play(MusicPlaylist newPlaylist)
    {
        if (newPlaylist == null || newPlaylist.Tracks.Count == 0)
        {
            Debug.LogWarning("[MusicPlaylistPlayer] Playlist is empty!");
            return;
        }

        StopInternal();

        currentPlaylist = newPlaylist;
        currentIndex = -1;
        lastPlayedIndex = -1;
        isPaused = false;

        BuildPlayOrder();
        playbackRoutine = StartCoroutine(PlaybackLoop());
    }

    /// <summary>Chuyển sang playlist khác với crossfade.</summary>
    public void SwitchTo(MusicPlaylist newPlaylist, float fadeDuration = -1f)
    {
        if (newPlaylist == null) return;

        // Nếu cùng playlist đang phát → skip
        if (currentPlaylist == newPlaylist && isPlaying) return;

        float fade = fadeDuration >= 0f
            ? fadeDuration
            : newPlaylist.CrossfadeDuration;

        // Fade out source hiện tại
        if (activeSource != null && activeSource.isPlaying)
        {
            var fadingSource = activeSource;
            StartCoroutine(FadeAndStop(fadingSource, fade));
        }

        // Chuyển sang source khác và bắt đầu playlist mới
        activeSource = activeSource == sourceA ? sourceB : sourceA;

        StopPlaybackRoutine();

        currentPlaylist = newPlaylist;
        currentIndex = -1;
        lastPlayedIndex = -1;
        isPaused = false;

        BuildPlayOrder();
        playbackRoutine = StartCoroutine(PlaybackLoop(fadeInFirstTrack: fade));
    }

    /// <summary>Dừng playlist với fade out.</summary>
    public void Stop(float fadeDuration = 1.5f)
    {
        StopPlaybackRoutine();

        if (activeSource != null && activeSource.isPlaying)
            StartCoroutine(FadeAndStop(activeSource, fadeDuration));

        isPlaying = false;
        isPaused = false;
        currentPlaylist = null;
    }

    /// <summary>Skip sang bài tiếp theo.</summary>
    public void SkipTrack()
    {
        if (!isPlaying || currentPlaylist == null) return;

        // Dừng routine hiện tại, nó sẽ tự chuyển bài tiếp
        StopPlaybackRoutine();
        playbackRoutine = StartCoroutine(PlaybackLoop());
    }

    /// <summary>Pause music (giữ vị trí bài hiện tại).</summary>
    public void Pause()
    {
        if (!isPlaying) return;
        isPaused = true;
        activeSource?.Pause();
    }

    /// <summary>Resume music từ vị trí pause.</summary>
    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;
        activeSource?.UnPause();
    }

    /// <summary>Set volume cho playlist (nhân với category volume).</summary>
    public void SetPlaylistVolume(float volume)
    {
        if (currentPlaylist != null)
            currentPlaylist.PlaylistVolume = Mathf.Clamp01(volume);

        UpdateSourceVolume();
    }

    // ═══════════════ PLAYBACK LOOP ═══════════════

    IEnumerator PlaybackLoop(float fadeInFirstTrack = 0f)
    {
        isPlaying = true;

        while (isPlaying)
        {
            // Chọn bài tiếp
            int nextIndex = GetNextTrackIndex();

            if (nextIndex < 0)
            {
                // Hết playlist, không loop
                isPlaying = false;
                OnPlaylistEnded?.Invoke();
                yield break;
            }

            currentIndex = nextIndex;
            int trackIndex = playOrder[currentIndex];
            var track = currentPlaylist.Tracks[trackIndex];

            if (track.Clip == null)
            {
                Debug.LogWarning($"[Playlist] Track \"{track.Name}\" has no clip, skipping.");
                continue;
            }

            // Crossfade từ bài trước sang bài mới
            float fadeDuration = fadeInFirstTrack > 0f
                ? fadeInFirstTrack
                : currentPlaylist.CrossfadeDuration;

            fadeInFirstTrack = 0f; // Chỉ dùng cho bài đầu

            yield return StartCoroutine(CrossfadeToTrack(track, fadeDuration));

            lastPlayedIndex = trackIndex;
            OnTrackChanged?.Invoke(track, trackIndex);

            // Chờ bài phát xong (trừ crossfade duration để bắt đầu fade sớm)
            float trackLength = track.Clip.length;
            float waitTime = trackLength - currentPlaylist.CrossfadeDuration;
            waitTime = Mathf.Max(waitTime, 0f);

            float elapsed = 0f;
            while (elapsed < waitTime)
            {
                if (!isPlaying) yield break;

                // Pause handling
                while (isPaused)
                    yield return null;

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Gap giữa 2 bài
            if (currentPlaylist.GapBetweenTracks > 0f)
            {
                yield return new WaitForSecondsRealtime(currentPlaylist.GapBetweenTracks);
            }
        }
    }

    // ═══════════════ TRACK SELECTION ═══════════════

    int GetNextTrackIndex()
    {
        if (currentPlaylist == null || playOrder.Count == 0)
            return -1;

        int next = currentIndex + 1;

        if (next >= playOrder.Count)
        {
            if (!currentPlaylist.Loop)
                return -1;

            // Rebuild order cho lượt mới
            BuildPlayOrder();
            next = 0;
        }

        return next;
    }

    void BuildPlayOrder()
    {
        int count = currentPlaylist.Tracks.Count;
        playOrder.Clear();

        switch (currentPlaylist.Mode)
        {
            case PlaylistMode.Sequential:
                for (int i = 0; i < count; i++)
                    playOrder.Add(i);
                break;

            case PlaylistMode.Shuffle:
                playOrder = BuildWeightedShuffle(count);
                // Đảm bảo bài đầu lượt mới khác bài cuối lượt trước
                if (playOrder.Count > 1 && playOrder[0] == lastPlayedIndex)
                {
                    // Swap vị trí 0 với vị trí random khác
                    int swapIdx = UnityEngine.Random.Range(1, playOrder.Count);
                    (playOrder[0], playOrder[swapIdx]) = (playOrder[swapIdx], playOrder[0]);
                }
                break;

            case PlaylistMode.Random:
                // Weighted random — build mỗi lần pick 1 bài
                for (int i = 0; i < count; i++)
                    playOrder.Add(WeightedRandomPick());
                break;

            case PlaylistMode.SingleLoop:
                // Chỉ phát bài đầu tiên
                playOrder.Add(0);
                break;
        }
    }

    List<int> BuildWeightedShuffle(int count)
    {
        // Tạo pool có weight: track index 2 có weight 3 → xuất hiện 3 lần trong pool
        var pool = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int weight = currentPlaylist.Tracks[i].Weight;
            for (int w = 0; w < weight; w++)
                pool.Add(i);
        }

        // Fisher-Yates shuffle
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        // Deduplicate consecutive (giữ order nhưng bỏ lặp liền kề)
        var result = new List<int> { pool[0] };
        for (int i = 1; i < pool.Count; i++)
        {
            if (pool[i] != result[^1])
                result.Add(pool[i]);
        }

        return result;
    }

    int WeightedRandomPick()
    {
        int totalWeight = 0;
        foreach (var track in currentPlaylist.Tracks)
            totalWeight += track.Weight;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;
        for (int i = 0; i < currentPlaylist.Tracks.Count; i++)
        {
            cumulative += currentPlaylist.Tracks[i].Weight;
            if (roll < cumulative) return i;
        }

        return 0;
    }

    // ═══════════════ CROSSFADE ═══════════════

    IEnumerator CrossfadeToTrack(MusicTrack track, float duration)
    {
        var nextSource = activeSource == sourceA ? sourceB : sourceA;
        var prevSource = activeSource;

        float targetVolume = CalculateVolume(track);

        // Setup next source
        nextSource.clip = track.Clip;
        nextSource.volume = 0f;
        nextSource.Play();

        // Crossfade
        if (duration > 0.01f && prevSource.isPlaying)
        {
            float prevStartVol = prevSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth S-curve cho crossfade tự nhiên
                float smooth = t * t * (3f - 2f * t);

                prevSource.volume = Mathf.Lerp(prevStartVol, 0f, smooth);
                nextSource.volume = Mathf.Lerp(0f, targetVolume, smooth);

                yield return null;
            }

            prevSource.Stop();
        }

        prevSource.volume = 0f;
        nextSource.volume = targetVolume;
        activeSource = nextSource;
    }

    IEnumerator FadeAndStop(AudioSource source, float duration)
    {
        float start = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(start, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        source.Stop();
        source.volume = 0f;
    }

    // ═══════════════ HELPERS ═══════════════

    float CalculateVolume(MusicTrack track)
    {
        float categoryVol = AudioManager.GetVolume(AudioCategory.Music);
        float playlistVol = currentPlaylist?.PlaylistVolume ?? 1f;
        float trackVol = track.Volume;

        return categoryVol * playlistVol * trackVol;
    }

    void UpdateSourceVolume()
    {
        if (activeSource != null && activeSource.isPlaying && CurrentTrack != null)
            activeSource.volume = CalculateVolume(CurrentTrack);
    }

    void StopInternal()
    {
        StopPlaybackRoutine();
        sourceA?.Stop();
        sourceB?.Stop();
        isPlaying = false;
        isPaused = false;
    }

    void StopPlaybackRoutine()
    {
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }
        if (crossfadeRoutine != null)
        {
            StopCoroutine(crossfadeRoutine);
            crossfadeRoutine = null;
        }
    }
}
