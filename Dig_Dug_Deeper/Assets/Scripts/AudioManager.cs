using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all game audio: background music, sound effects, and level-change cues.
/// Implements a singleton pattern for global access.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // Singleton instance
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get { return _instance; }
        private set { _instance = value; }
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _musicSource;       // AudioSource for background music
    [SerializeField] private AudioSource _sfxSource;         // AudioSource for general sound effects
    [SerializeField] private AudioSource _playerSfxSource;   // AudioSource for player-specific effects
    [SerializeField] private AudioSource _levelChangeSource; // Dedicated source for level-change music

    [Header("Music Clips")]
    [Tooltip("List of music tracks available to play.")]
    [SerializeField] private Sound[] _musicClips;

    [Header("SFX Clips")]
    [Tooltip("List of general sound effects available to play.")]
    [SerializeField] private Sound[] _sfxClips;

    [Header("Player SFX Clips")]
    [Tooltip("List of player-specific sound effects available to play.")]
    [SerializeField] private Sound[] _playerSfxClips;

    [Header("Level Change Clips")]
    [Tooltip("List of level change music clips.")]
    [SerializeField] private Sound[] _levelChangeClips;

    // Lookup dictionaries for quick clip retrieval
    private Dictionary<string, Sound> _musicDict;
    private Dictionary<string, Sound> _sfxDict;
    private Dictionary<string, Sound> _playerSfxDict;
    private Dictionary<string, Sound> _levelChangeDict;

    /// <summary>
    /// Enforce singleton and initialize lookup dictionaries.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicDict = new Dictionary<string, Sound>();
        _sfxDict = new Dictionary<string, Sound>();
        _playerSfxDict = new Dictionary<string, Sound>();
        _levelChangeDict = new Dictionary<string, Sound>();

        foreach (var sound in _musicClips) _musicDict[sound.Name] = sound;
        foreach (var sound in _sfxClips) _sfxDict[sound.Name] = sound;
        foreach (var sound in _playerSfxClips) _playerSfxDict[sound.Name] = sound;
        foreach (var sound in _levelChangeClips) _levelChangeDict[sound.Name] = sound;
    }

    /// <summary>
    /// Play a music track by name in loop mode.
    /// </summary>
    public void PlayMusic(string name)
    {
        if (_musicDict.TryGetValue(name, out Sound sound))
        {
            _musicSource.clip = sound.Clip;
            _musicSource.volume = sound.Volume;
            _musicSource.pitch = sound.Pitch;
            _musicSource.loop = true;
            _musicSource.Play();
        }
        else Debug.LogWarning($"AudioManager: Music '{name}' not found.");
    }

    /// <summary>
    /// Play a music track by name only once (non-looping).
    /// </summary>
    public void PlayMusicOnce(string name)
    {
        if (_musicDict.TryGetValue(name, out Sound sound))
        {
            _musicSource.clip = sound.Clip;
            _musicSource.volume = sound.Volume;
            _musicSource.pitch = sound.Pitch;
            _musicSource.loop = false;
            _musicSource.Play();
        }
        else Debug.LogWarning($"AudioManager: One-shot music '{name}' not found.");
    }

    /// <summary>
    /// Stop the currently playing music.
    /// </summary>
    public void StopMusic()
    {
        if (_musicSource != null)
            _musicSource.Stop();
        else
            Debug.LogWarning("AudioManager: Cannot stop music, source is null or destroyed.");
    }

    /// <summary>
    /// Play a one-shot general sound effect by name.
    /// </summary>
    public void PlaySFX(string name)
    {
        if (_sfxDict.TryGetValue(name, out Sound sound))
            _sfxSource.PlayOneShot(sound.Clip, sound.Volume);
        else Debug.LogWarning($"AudioManager: SFX '{name}' not found.");
    }

    /// <summary>
    /// Play a one-shot player-specific sound effect by name.
    /// </summary>
    public void PlayPlayerSFX(string name)
    {
        if (_playerSfxDict.TryGetValue(name, out Sound sound))
            _playerSfxSource.PlayOneShot(sound.Clip, sound.Volume);
        else Debug.LogWarning($"AudioManager: Player SFX '{name}' not found.");
    }

    /// <summary>
    /// Play level change music once on its dedicated source.
    /// </summary>
    public void PlayLevelChange(string name)
    {
        if (_levelChangeDict.TryGetValue(name, out Sound sound))
        {
            _levelChangeSource.clip = sound.Clip;
            _levelChangeSource.volume = sound.Volume;
            _levelChangeSource.pitch = sound.Pitch;
            _levelChangeSource.loop = false;
            _levelChangeSource.Play();
        }
        else Debug.LogWarning($"AudioManager: Level change music '{name}' not found.");
    }

    /// <summary>
    /// Adjust global music volume.
    /// </summary>
    public void SetMusicVolume(float volume) { _musicSource.volume = Mathf.Clamp01(volume); }

    /// <summary>
    /// Adjust global general SFX volume.
    /// </summary>
    public void SetSFXVolume(float volume) { _sfxSource.volume = Mathf.Clamp01(volume); }

    /// <summary>
    /// Adjust global player SFX volume.
    /// </summary>
    public void SetPlayerSFXVolume(float volume) { _playerSfxSource.volume = Mathf.Clamp01(volume); }

    /// <summary>
    /// Adjust global level change volume.
    /// </summary>
    public void SetLevelChangeVolume(float volume) { _levelChangeSource.volume = Mathf.Clamp01(volume); }
}

/// <summary>
/// Serializable sound data for lookup.
/// </summary>
[System.Serializable]
public class Sound
{
    [Tooltip("Unique name for the sound.")]
    public string Name;

    [Tooltip("Audio clip asset.")]
    public AudioClip Clip;

    [Range(0f, 1f)]
    [Tooltip("Playback volume.")]
    public float Volume = 1f;

    [Range(0.1f, 3f)]
    [Tooltip("Playback pitch.")]
    public float Pitch = 1f;
}
