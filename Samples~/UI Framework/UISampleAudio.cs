using UnityEngine;

/// <summary>仅供本 Sample 的提示面板播放 Resources 音频，不依赖其他 Yvsim 业务包。</summary>
internal static class UISampleAudio
{
    internal static void Play(GameObject owner, ref AudioSource source, string resourcePath)
    {
        Stop(source);
        if (string.IsNullOrWhiteSpace(resourcePath)) return;

        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null)
        {
            Debug.LogWarning("[" + owner.name + "] Resources 音频不存在：" + resourcePath, owner);
            return;
        }

        if (source == null)
        {
            source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        source.clip = clip;
        source.Play();
    }

    internal static void Stop(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        source.clip = null;
    }
}
