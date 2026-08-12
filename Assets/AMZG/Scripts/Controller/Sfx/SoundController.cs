using System;
using System.Collections.Generic;
using UnityEngine;
using VTLTools;


public class SoundController : MonoBehaviour
{
    public static SoundController Instance { get; private set; }

    [Header("Normal SFX Sources")]
    public List<AudioSource> sfxSources = new List<AudioSource>();   // efxSource1..9

    [Header("Exclusive Sources")]
    public List<AudioSource> exclusiveSources = new List<AudioSource>(); // ExclusiveSource..ExclusiveSource4

    [Header("Music")]
    public AudioSource musicSource;
    [SerializeField] private float musicVolume = 0.35f;
    public AudioClip SfxButton;

    public AudioClip StoppedMusic;

    public List<Sound> listSounds;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    //private void Start()
    //{
    //    PlaySound(SFX.MusicBackground, 1, true);
    //}

    // ================================================
    //                GENERIC PLAY LOGIC
    // ================================================
    private AudioSource PlayFromList(List<AudioSource> list, AudioClip clip,
        float pitch, bool loop, float volume)
    {
        if (!StaticVariables.IsSoundOn || clip == null)
            return null;

        foreach (var src in list)
        {
            if (!src.isPlaying)
            {
                AssignAndPlay(src, clip, pitch, loop, volume);
                return src;
            }
        }

        var fallback = list[0];
        AssignAndPlay(fallback, clip, pitch, loop, volume);
        return fallback;
    }

    private void AssignAndPlay(AudioSource src, AudioClip clip,
        float pitch, bool loop, float volume)
    {
        src.clip = clip;
        src.pitch = pitch;
        src.volume = volume;
        src.loop = loop;
        src.Play();
    }

    // ================================================
    //                PUBLIC API
    // ================================================
    public AudioSource PlaySingle(AudioClip clip, float pitch = 1f, bool loop = false, float volume = 1f)
        => PlayFromList(sfxSources, clip, pitch, loop, volume);

    public AudioSource PlaySingleExclusive(AudioClip clip, float pitch = 1f,
        bool loop = false, float volume = 1f)
        => PlayFromList(exclusiveSources, clip, pitch, loop, volume);

    // ================================================
    //                MUSIC CONTROL
    // ================================================
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = StaticVariables.IsMusicOn ? musicVolume : 0f;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        StoppedMusic = musicSource.clip;
        musicSource.Stop();
    }

    public void ReplayMusic()
    {
        if (StoppedMusic != null)
        {
            musicSource.clip = StoppedMusic;
            musicSource.Play();
        }
    }

    public void Mute() => musicSource.volume = 0;
    public void Unmute() => musicSource.volume = musicVolume;

    public void StopAllSounds()
    {
        foreach (var s in sfxSources) s.Stop();
    }

    public void PauseSounds()
    {
        foreach (var s in sfxSources) s.Pause();
    }

    public void UnpauseSounds()
    {
        foreach (var s in sfxSources) s.UnPause();
    }

    public AudioSource PlaySoundInterval(AudioClip clip, float fromSeconds, float toSeconds = -1, float volume = 1f)
    {
        if (!StaticVariables.IsSoundOn || clip == null) return null;

        var src = PlayFromList(sfxSources, clip, 1f, false, volume);
        if (src == null) return null;

        src.time = fromSeconds;
        if (toSeconds < fromSeconds)
        {
            toSeconds = clip.length;
        }
        src.Play();
        src.SetScheduledEndTime(AudioSettings.dspTime + (toSeconds - fromSeconds));

        return src;
    }

    public void PlaySound(SoundType type, float volunm, bool musicSound = false)
    {
        AudioClip clip = null;
        for (var i = 0; i < listSounds.Count; i++)
        {
            var sound = listSounds[i];
            if (sound.sound == type)
            {
                clip = sound.audio;
                break;
            }
        }

        if (clip != null)
        {
            if (musicSound)
                PlayMusic(clip, true);
            else
                PlaySingle(clip, 1, false, volunm);
        }
        else
        {
            Debug.LogError("Missing Sound");
        }
    }

    [Serializable]
    public class Sound
    {
        public SoundType sound;
        public AudioClip audio;

    }

}
