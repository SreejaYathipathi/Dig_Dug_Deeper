using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all game audio: background music and sound effects. Implements a singleton pattern for global access.
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

    [Header("Music Clips")]
    [Tooltip("List of music tracks available to play.")]
    [SerializeField] private Sound[] _musicClips;

    [Header("SFX Clips")]
    [Tooltip("List of general sound effects available to play.")]
    [SerializeField] private Sound[] _sfxClips;

    [Header("Player SFX Clips")]
    [Tooltip("List of player-specific sound effects available to play.")]
    [SerializeField] private Sound[] _playerSfxClips;

    // Lookup dictionaries for quick clip retrieval
    private Dictionary<string, Sound> _musicDict;
    private Dictionary<string, Sound> _sfxDict;
    private Dictionary<string, Sound> _playerSfxDict;

    /// <summary>
    /// Enforce singleton pattern and initialize dictionaries.
    /// </summary>
    private void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize lookup dictionaries
        _musicDict = new Dictionary<string, Sound>();
        _sfxDict = new Dictionary<string, Sound>();
        _playerSfxDict = new Dictionary<string, Sound>();

        foreach (var sound in _musicClips)
        {
            if (!_musicDict.ContainsKey(sound.Name))
                _musicDict.Add(sound.Name, sound);
        }
        foreach (var sound in _sfxClips)
        {
            if (!_sfxDict.ContainsKey(sound.Name))
                _sfxDict.Add(sound.Name, sound);
        }
        foreach (var sound in _playerSfxClips)
        {
            if (!_playerSfxDict.ContainsKey(sound.Name))
                _playerSfxDict.Add(sound.Name, sound);
        }
    }

    /// <summary>
    /// Play a music track by name. Stops current music and loops new track.
    /// </summary>
    /// <param name="name">The key name of the music track.</param>
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
        else
        {
            Debug.LogWarning($"AudioManager: Music '{name}' not found.");
        }
    }

    /// <summary>
    /// Stop the currently playing music.
    /// </summary>
    public void StopMusic()
    {
        _musicSource.Stop();
    }

    /// <summary>
    /// Play a one-shot general sound effect by name.
    /// </summary>
    /// <param name="name">The key name of the general SFX.</param>
    public void PlaySFX(string name)
    {
        if (_sfxDict.TryGetValue(name, out Sound sound))
        {
            _sfxSource.PlayOneShot(sound.Clip, sound.Volume);
        }
        else
        {
            Debug.LogWarning($"AudioManager: SFX '{name}' not found.");
        }
    }

    /// <summary>
    /// Play a one-shot player-specific sound effect by name.
    /// </summary>
    /// <param name="name">The key name of the player SFX.</param>
    public void PlayPlayerSFX(string name)
    {
        if (_playerSfxDict.TryGetValue(name, out Sound sound))
        {
            _playerSfxSource.PlayOneShot(sound.Clip, sound.Volume);
        }
        else
        {
            Debug.LogWarning($"AudioManager: Player SFX '{name}' not found.");
        }
    }

    /// <summary>
    /// Set the global music volume.
    /// </summary>
    /// <param name="volume">Volume (0.0 to 1.0).</param>
    public void SetMusicVolume(float volume)
    {
        _musicSource.volume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Set the global general SFX volume.
    /// </summary>
    /// <param name="volume">Volume (0.0 to 1.0).</param>
    public void SetSFXVolume(float volume)
    {
        _sfxSource.volume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Set the global player SFX volume.
    /// </summary>
    /// <param name="volume">Volume (0.0 to 1.0).</param>
    public void SetPlayerSFXVolume(float volume)
    {
        _playerSfxSource.volume = Mathf.Clamp01(volume);
    }
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
