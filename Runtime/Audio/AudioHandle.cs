using UnityEngine;

/// <summary>
/// Handle trả về khi Play() — dùng để Stop, Fade, kiểm tra trạng thái.
/// 
/// var handle = AudioManager.Play("boss_theme");
/// handle.FadeOut(2f);
/// handle.Stop();
/// </summary>
public class AudioHandle
{
    public AudioSource Source { get; private set; }
    public string Key { get; private set; }
    public bool IsValid => Source != null && Source.isPlaying;

    MonoBehaviour owner;

    public AudioHandle(AudioSource source, string key, MonoBehaviour coroutineOwner)
    {
        Source = source;
        Key = key;
        owner = coroutineOwner;
    }

    public void Stop()
    {
        if (Source != null)
            Source.Stop();
    }

    public void Pause()
    {
        if (Source != null)
            Source.Pause();
    }

    public void Resume()
    {
        if (Source != null)
            Source.UnPause();
    }

    public void SetVolume(float volume)
    {
        if (Source != null)
            Source.volume = volume;
    }

    /// <summary>Fade out rồi stop.</summary>
    public void FadeOut(float duration)
    {
        if (Source == null || owner == null) return;
        owner.StartCoroutine(CoFade(Source.volume, 0f, duration, stopAfter: true));
    }

    /// <summary>Fade in từ 0.</summary>
    public void FadeIn(float duration, float targetVolume = 1f)
    {
        if (Source == null || owner == null) return;
        Source.volume = 0f;
        owner.StartCoroutine(CoFade(0f, targetVolume, duration, stopAfter: false));
    }

    System.Collections.IEnumerator CoFade(float from, float to, float duration, bool stopAfter)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (Source != null)
                Source.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        if (Source != null)
        {
            Source.volume = to;
            if (stopAfter) Source.Stop();
        }
    }
}
