using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System.Linq;

/// <summary>
/// 汎用的な SoundManager
/// - Inspector で SFX / Music を登録して名前で再生できる
/// - SFX プール、BGM クロスフェード、ミキサー連携、ボリューム保存対応
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager I { get; private set; }

    [Header("Audio Mixer (optional)")]
    public AudioMixer masterMixer; // optional: expose parameters "MasterVolume","MusicVolume","SFXVolume"

    [Header("Music Settings")]
    public List<NamedClip> musicClips = new List<NamedClip>(); // InspectorでBGMを登録
    public AudioMixerGroup musicMixerGroup;
    public float musicDefaultVolume = 1f;
    public float musicCrossfadeTime = 1f;

    [Header("SFX Settings")]
    public List<NamedClip> sfxClips = new List<NamedClip>(); // InspectorでSFXを登録
    public AudioMixerGroup sfxMixerGroup;
    public int sfxPoolSize = 16;
    public bool expandPoolIfNeeded = true;

    [Header("General")]
    public bool dontDestroyOnLoad = true;
    [Tooltip("Inspector で設定しておくと便利（未設定可））")]
    public Transform spatialSfxParent;

    // internal
    AudioSource musicSourceA;
    AudioSource musicSourceB;
    bool useA = true;

    List<AudioSource> sfxPool = new List<AudioSource>();

    // PlayerPrefs keys
    const string PREF_MASTER = "SM_Master";
    const string PREF_MUSIC = "SM_Music";
    const string PREF_SFX = "SM_SFX";

    void Awake()
    {
        // singleton
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        // create 2 music sources for crossfade
        musicSourceA = gameObject.AddComponent<AudioSource>();
        musicSourceB = gameObject.AddComponent<AudioSource>();
        SetupMusicSource(musicSourceA);
        SetupMusicSource(musicSourceB);

        // create sfx pool
        for (int i = 0; i < sfxPoolSize; i++) sfxPool.Add(CreatePooledSfxSource());

        // load volumes from prefs
        SetMasterVolume(PlayerPrefs.GetFloat(PREF_MASTER, 1f));
        SetMusicVolume(PlayerPrefs.GetFloat(PREF_MUSIC, 1f));
        SetSFXVolume(PlayerPrefs.GetFloat(PREF_SFX, 1f));
    }

    AudioSource CreatePooledSfxSource()
    {
        var go = new GameObject("SFX_Source");
        if (spatialSfxParent != null) go.transform.SetParent(spatialSfxParent);
        else go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // default 2D. PositionPlayback uses 3D override.
        if (sfxMixerGroup != null) src.outputAudioMixerGroup = sfxMixerGroup;
        go.SetActive(true);
        return src;
    }

    void SetupMusicSource(AudioSource s)
    {
        s.playOnAwake = false;
        s.loop = true;
        s.spatialBlend = 0f;
        if (musicMixerGroup != null) s.outputAudioMixerGroup = musicMixerGroup;
    }

    // ---------------------------
    // Public API: Music control
    // ---------------------------

    /// <summary> 名前でBGMを再生（存在しなければ何もしない） ? クロスフェードあり </summary>
    public void PlayMusic(string name, float crossfade = -1f, bool loop = true)
    {
        if (crossfade < 0) crossfade = musicCrossfadeTime;
        var clip = musicClips.FirstOrDefault(c => c.name == name)?.clip;
        if (clip == null) { Debug.LogWarning($"SoundManager: Music '{name}' not found."); return; }
        StartCoroutine(PlayMusicInternal(clip, crossfade, loop));
    }

    IEnumerator PlayMusicInternal(AudioClip clip, float crossfade, bool loop)
    {
        AudioSource fadingOut = useA ? musicSourceA : musicSourceB;
        AudioSource fadingIn = useA ? musicSourceB : musicSourceA;
        useA = !useA;

        fadingIn.clip = clip;
        fadingIn.loop = loop;
        fadingIn.volume = 0f;
        fadingIn.Play();

        float t = 0f;
        float startOutVol = fadingOut.isPlaying ? fadingOut.volume : 0f;
        float targetVolume = musicDefaultVolume;
        while (t < crossfade)
        {
            t += Time.unscaledDeltaTime;
            float f = Mathf.Clamp01(t / crossfade);
            if (fadingOut.isPlaying) fadingOut.volume = Mathf.Lerp(startOutVol, 0f, f);
            fadingIn.volume = Mathf.Lerp(0f, targetVolume, f);
            yield return null;
        }
        // finalize
        if (fadingOut.isPlaying) fadingOut.Stop();
        fadingIn.volume = targetVolume;
    }

    public void StopMusic(float fadeOut = 0.5f)
    {
        StartCoroutine(StopMusicCoroutine(fadeOut));
    }

    IEnumerator StopMusicCoroutine(float fade)
    {
        AudioSource a = musicSourceA;
        AudioSource b = musicSourceB;
        float t = 0f;
        float aStart = a.isPlaying ? a.volume : 0f;
        float bStart = b.isPlaying ? b.volume : 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float f = Mathf.Clamp01(t / fade);
            if (a.isPlaying) a.volume = Mathf.Lerp(aStart, 0f, f);
            if (b.isPlaying) b.volume = Mathf.Lerp(bStart, 0f, f);
            yield return null;
        }
        if (a.isPlaying) a.Stop();
        if (b.isPlaying) b.Stop();
    }

    // ---------------------------
    // Public API: SFX control
    // ---------------------------

    /// <summary> 名前でSFX再生（2D、ワンショット） </summary>
    public void PlaySFX(string name, float volumeScale = 1f, float pitch = 1f)
    {
        var clip = sfxClips.FirstOrDefault(c => c.name == name)?.clip;
        if (clip == null) { Debug.LogWarning($"SoundManager: SFX '{name}' not found."); return; }
        var src = GetAvailableSfxSource();
        src.transform.position = transform.position;
        src.spatialBlend = 0f;
        src.pitch = pitch;
        src.volume = Mathf.Clamp01(volumeScale);
        src.clip = clip;
        src.loop = false;
        src.Play();
    }

    /// <summary> ワールド座標で SFX を再生（3D） </summary>
    public void PlaySFXAtPosition(string name, Vector3 pos, float volumeScale = 1f, float pitch = 1f, float spatialBlend = 1f)
    {
        var clip = sfxClips.FirstOrDefault(c => c.name == name)?.clip;
        if (clip == null) { Debug.LogWarning($"SoundManager: SFX '{name}' not found."); return; }
        var src = GetAvailableSfxSource();
        src.transform.position = pos;
        src.spatialBlend = Mathf.Clamp01(spatialBlend);
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1f;
        src.maxDistance = 20f;
        src.pitch = pitch;
        src.volume = Mathf.Clamp01(volumeScale);
        src.clip = clip;
        src.loop = false;
        src.Play();
    }

    /// <summary> ループ SFX（例えば近接音など）を再生して AudioSource を返す（停止は returnedSource.Stop()） </summary>
    public AudioSource PlayLoopSFX(string name, Vector3? pos = null, float spatialBlend = 0f, float volume = 1f)
    {
        var clip = sfxClips.FirstOrDefault(c => c.name == name)?.clip;
        if (clip == null) { Debug.LogWarning($"SoundManager: SFX '{name}' not found."); return null; }
        var src = GetAvailableSfxSource();
        if (pos.HasValue) src.transform.position = pos.Value;
        src.spatialBlend = Mathf.Clamp01(spatialBlend);
        src.pitch = 1f;
        src.volume = Mathf.Clamp01(volume);
        src.clip = clip;
        src.loop = true;
        src.Play();
        return src;
    }

    AudioSource GetAvailableSfxSource()
    {
        // まず停止しているものを探す（まだ再生中なら使わない）
        var free = sfxPool.FirstOrDefault(s => !s.isPlaying);
        if (free != null) return free;

        if (expandPoolIfNeeded)
        {
            var add = CreatePooledSfxSource();
            sfxPool.Add(add);
            return add;
        }

        // どれも空いてないなら最初のを reuse（上書き）
        return sfxPool[0];
    }

    // ---------------------------
    // Volume / Mixer control
    // ---------------------------

    /// <summary> マスター音量（0..1） </summary>
    public void SetMasterVolume(float v)
    {
        v = Mathf.Clamp01(v);
        if (masterMixer != null) masterMixer.SetFloat("MasterVolume", LinearToDb(v));
        PlayerPrefs.SetFloat(PREF_MASTER, v);
    }

    public void SetMusicVolume(float v)
    {
        v = Mathf.Clamp01(v);
        if (masterMixer != null) masterMixer.SetFloat("MusicVolume", LinearToDb(v));
        else
        {
            musicSourceA.volume = v;
            musicSourceB.volume = v;
        }
        PlayerPrefs.SetFloat(PREF_MUSIC, v);
    }

    public void SetSFXVolume(float v)
    {
        v = Mathf.Clamp01(v);
        if (masterMixer != null) masterMixer.SetFloat("SFXVolume", LinearToDb(v));
        else
        {
            // apply to pool sources
            foreach (var s in sfxPool) s.volume = v;
        }
        PlayerPrefs.SetFloat(PREF_SFX, v);
    }

    // 音量は DB（-80..0）になる。0->0dB, 1->0dB, 0.5->-6.02db 等の単純変換
    float LinearToDb(float linear)
    {
        if (linear <= 0.0001f) return -80f;
        return 20f * Mathf.Log10(linear);
    }

    // ---------------------------
    // Utility / Debug
    // ---------------------------

    [System.Serializable]
    public class NamedClip
    {
        public string name;
        public AudioClip clip;
    }

    /// <summary> 全サウンドをミュート（Mixer を使っていることを想定） </summary>
    public void MuteAll(bool mute)
    {
        SetMasterVolume(mute ? 0f : 1f);
    }
}
