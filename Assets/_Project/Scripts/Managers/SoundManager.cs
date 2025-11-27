using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance => s_Instance;
    private static SoundManager s_Instance;

    [Serializable]
    public class SoundButtonClipPair
    {
        public ButtonUIType m_type;
        public AudioClip m_audioClip;
    }

    public enum ButtonUIType
    {
        regular,
        cancel,
        confirm,
        buy
    }

    [Header("Clips")]
    [SerializeField] private List<SoundButtonClipPair> _buttonClipPairs = new();

    [Header("Music")]
    [SerializeField] private AudioClip _menuTheme;
    [SerializeField] private AudioClip _introSceneTheme;
    [SerializeField] private AudioClip _boatIsMovingTheme;
    [SerializeField] private AudioClip _winTheme;

    [Header("Mixer")]
    [SerializeField] private AudioMixer _audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _soundEffectSource;
    [SerializeField] private AudioSource _musicSource1;
    [SerializeField] private AudioSource _musicSource2; 

    [Header("Other")]
    [SerializeField] private float _fadeTime = 1.0f;
    [SerializeField] private float _soundInterval = 0.01f;
    [SerializeField] private float _sceneLoadFadeDuration = 1.0f;

    private const float MixerMinDecibels = -80f;
    private const string GameSceneName = "Game";
    private const string MusicVolumeParameter = "MusicVolume";
    private const string EffectsVolumeParameter = "EffectsVolume";
    private AudioSource _currentMusicSource;
    private AudioSource _nextMusicSource;
    private bool _isCrossfading;
    private Coroutine _crossfadeRoutine;
    private Coroutine _sceneLoadMixerRoutine;
    private float _soundPlayedTime;

    private void OnEnable()
    {

    }

    private void OnDisable()
    {
        StopAllCoroutines();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SimpleEventManager.Unsubscribe(GameEvents.GameStateChanged, OnGameStateChanged);
        SimpleEventManager.Unsubscribe(GameEvents.WaterStateChanged, OnGameStateChanged);
    }

    private void Start()
    {
        // Set initial volume levels
        SetMusicVolume(SaveManager.Instance.MusicVolume);
        SetSoundEffectVolume(SaveManager.Instance.EffectsVolume);
    }

    #region General


    // Play a sound effect

    public void Initialize()
    {
                if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;

        DontDestroyOnLoad(this.gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        HandleSceneLoad(SceneManager.GetActiveScene());

        _currentMusicSource = _musicSource1;
        _nextMusicSource = _musicSource2;

        _currentMusicSource.loop = true;
        _nextMusicSource.loop = true;

        // Set initial volume levels
        SetMusicVolume(SaveManager.Instance.MusicVolume);
        SetSoundEffectVolume(SaveManager.Instance.EffectsVolume);

        //PlayMusic(_gameTheme);

        SimpleEventManager.Subscribe(GameEvents.GameStateChanged, OnGameStateChanged);
        SimpleEventManager.Subscribe(GameEvents.WaterStateChanged, OnGameStateChanged);
    }

    private void SetSoundPlayedTime(float time)
    {
        _soundPlayedTime = time;
    }

    private bool CheckInterval()
    {
        bool check = _soundPlayedTime + _soundInterval > Time.time;
        if (Time.deltaTime == 0)
            return false;
        return check;
    }

    public void PlaySoundEffect(AudioClip clip, bool urgent = false)
    {
        if ((clip == null || CheckInterval()) && !urgent)
        {
            return;
        }
        _soundEffectSource.PlayOneShot(clip);
        SetSoundPlayedTime(Time.time);
    }

    public void PlaySoundEffect(AudioClip clip, float volume)
    {
        if (clip == null || CheckInterval())
        {
            return;
        }
        _soundEffectSource.PlayOneShot(clip, volume);
        SetSoundPlayedTime(Time.time);
    }

    public void PauseAllSounds()
    {
        // Pause music
        _musicSource1.Pause();
        _musicSource2.Pause();

        // Pause sound effects
        if (_soundEffectSource.isPlaying)
        {
            _soundEffectSource.Pause();
        }
    }

    public void UnPauseAllSounds()
    {
        // Unpause music
        _musicSource1.UnPause();
        _musicSource2.UnPause();

        // Unpause sound effects
        if (_soundEffectSource.clip != null)
        {
            _soundEffectSource.UnPause();
        }
    }


    #region Music
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null)
            return;

        Crossfade(clip);
    }


    public void PauseMusic()
    {
        _musicSource1.Pause();
        _musicSource2.Pause();
    }

    public void UnPauseMusic()
    {
        _musicSource1.UnPause();
        _musicSource2.UnPause();
    }

    private void OnGameStateChanged(object gameStateObj)
    {
        if (GameManager.Instance == null)
            return;

        if (gameStateObj is GameManager.GameState gameState)
        {
            switch (gameState)
            {
                // case GameManager.GameState.IntroScene:
                //     PlayMusic(_introSceneTheme);
                //     break;
                case GameManager.GameState.BoatMoving:
                    PlayMusic(_introSceneTheme);
                    break;
                // case GameManager.GameState.Win:
                //     PlayMusic(_winTheme);
                //     break;
                // case GameManager.GameState.Menu:
                //     PlayMusic(_menuTheme);
                //     break;
                default:
                    break;
            }
        }
        else if (gameStateObj is GameManager.WaterState waterState)
        {
            switch (waterState)
            {
                case GameManager.WaterState.CRAZY:
                    PlayMusic(_boatIsMovingTheme);
                    break;
                default:
                    break;
            }
        }
    }

    private void Crossfade(AudioClip musicClip)
    {
        if (musicClip == null)
            return;

        // If already playing this clip and not crossfading, skip
        if (!_isCrossfading && _currentMusicSource.clip == musicClip && _currentMusicSource.isPlaying)
            return;

        // If nothing is playing yet, start immediately without crossfade
        if (!_currentMusicSource.isPlaying || _currentMusicSource.clip == null)
        {
            _currentMusicSource.clip = musicClip;
            _currentMusicSource.volume = 1f;
            _currentMusicSource.Play();
            return;
        }

        if (_crossfadeRoutine != null)
        {
            StopCoroutine(_crossfadeRoutine);
            _crossfadeRoutine = null;
        }
        _crossfadeRoutine = StartCoroutine(CrossfadeCoroutine(musicClip));
    }



    private IEnumerator CrossfadeCoroutine(AudioClip musicClip)
    {
        _isCrossfading = true;

        // Ensure the next music source is playing
        _nextMusicSource.Stop();
        _nextMusicSource.clip = musicClip;
        _nextMusicSource.volume = 0f;
        _nextMusicSource.Play();

        // Fade out the current music source and fade in the next music source simultaneously
        float currentTime = 0.0f;
        float duration = Mathf.Max(0.0001f, _fadeTime);
        float startCurrentVol = _currentMusicSource.volume;
        const float targetVol = 1f;
        while (currentTime < duration)
        {
            currentTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(currentTime / duration);
            _currentMusicSource.volume = Mathf.Lerp(startCurrentVol, 0f, t);
            _nextMusicSource.volume = Mathf.Lerp(0f, targetVol, t);
            yield return null;
        }

        // Stop the current music source and set the volume back to its original value
        _currentMusicSource.Stop();
        _currentMusicSource.volume = targetVol;

        // Swap the current and next music sources
        AudioSource temp = _currentMusicSource;
        _currentMusicSource = _nextMusicSource;
        _nextMusicSource = temp;

        _isCrossfading = false;
        _crossfadeRoutine = null;
    }

    #endregion

    #region Set Volume

    private float ConvertLinearVolumeToDb(float volume)
    {
        float adjustedVolume = Mathf.Clamp(volume, 0.0001f, 1f);
        return Mathf.Lerp(MixerMinDecibels, 0f, Mathf.Pow(adjustedVolume, 0.3f));
    }

    // Set the volume of sound effects
    public void SetSoundEffectVolume(float volume)
    {
        _audioMixer.SetFloat(EffectsVolumeParameter, ConvertLinearVolumeToDb(volume));
        SaveManager.Instance.EffectsVolume = volume;
    }

    // Set the volume of background music
    public void SetMusicVolume(float volume)
    {
        Debug.Log("SetMusicVolume: " + volume);
        _audioMixer.SetFloat(MusicVolumeParameter, ConvertLinearVolumeToDb(volume));
        SaveManager.Instance.MusicVolume = volume;
    }
    #endregion

    #endregion


    #region Play Sound Effects

    public void PlayButtonSound(ButtonUIType type)
    {
        AudioClip _buttonClip = _buttonClipPairs.Find(x => x.m_type == type).m_audioClip;
        PlaySoundEffect(_buttonClip);
        //Vibrate();
    }

    public void Vibrate(int durationMilis = 10)
    {
        if (SaveManager.Instance.Vibration == 0)
            return;

        long[] vibrationPattern = { 0, durationMilis };

        /*if (SaveManager.Instance.Vibration == 1)*/
        if (Application.platform == RuntimePlatform.Android)
        {

            // Get the current activity
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Get the vibrator service from the current activity
            AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");

            // Check if the vibrator service exists
            if (vibrator != null)
            {
                // Vibrate with the specified pattern
                vibrator.Call("vibrate", vibrationPattern, -1);
            }
            else
            {
                Debug.LogWarning("Vibrator service not found.");
            }
        }
        else
        {
            //Debug.LogWarning("Vibration only supported on Android.");
        }
    }

    #endregion


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleSceneLoad(scene);
    }

    private void HandleSceneLoad(Scene scene)
    {
        if (!string.Equals(scene.name, GameSceneName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_audioMixer == null || SaveManager.Instance == null)
        {
            return;
        }

        if (_sceneLoadMixerRoutine != null)
        {
            StopCoroutine(_sceneLoadMixerRoutine);
        }

        _sceneLoadMixerRoutine = StartCoroutine(GameSceneMixerFadeCoroutine());
    }

    private IEnumerator GameSceneMixerFadeCoroutine()
    {
        float duration = Mathf.Max(0.0001f, _sceneLoadFadeDuration);
        float startDb = MixerMinDecibels;
        float targetMusicDb = ConvertLinearVolumeToDb(SaveManager.Instance.MusicVolume);
        float targetEffectsDb = ConvertLinearVolumeToDb(SaveManager.Instance.EffectsVolume);

        _audioMixer.SetFloat(MusicVolumeParameter, startDb);
        _audioMixer.SetFloat(EffectsVolumeParameter, startDb);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _audioMixer.SetFloat(MusicVolumeParameter, Mathf.Lerp(startDb, targetMusicDb, t));
            _audioMixer.SetFloat(EffectsVolumeParameter, Mathf.Lerp(startDb, targetEffectsDb, t));
            yield return null;
        }

        _audioMixer.SetFloat(MusicVolumeParameter, targetMusicDb);
        _audioMixer.SetFloat(EffectsVolumeParameter, targetEffectsDb);
        _sceneLoadMixerRoutine = null;
    }

}
