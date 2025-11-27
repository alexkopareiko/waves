using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance => s_Instance;
    private static SaveManager s_Instance;

    const string k_SoundVolume = "SoundVolume";
    const string k_MusicVolume = "MusicVolume";
    const string k_Vibration = "Vibration";
    const string k_PostProcessing = "PostProcessing";
    const string k_Privacy = "Privacy";
    const string k_TutorialWatched = "TutorialWatched";
    const string k_RateUsClicked = "RateUsClicked";
    const string k_LosesCount = "LosesCount";
    const string k_MaxScore = "MaxScore";
    const string k_CurrentLevel = "CurrentLevel";

    private void SetupInstance()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        s_Instance = this;
    }

    public void Initialize()
    {
        SetupInstance();
    }

    #region Reset Prefs

    public void Reset()
    {
        PlayerPrefs.DeleteAll();
    }

    #endregion

    #region Sound Music Volume / Vibration / Post Processing

    public float EffectsVolume
    {
        get => GetFloat(k_SoundVolume, 1f);
        set => SetFloat(k_SoundVolume, value);
    }

    public float MusicVolume
    {
        get => GetFloat(k_MusicVolume, 0.8f);
        set => SetFloat(k_MusicVolume, value);
    }

    public int Vibration
    {
        get => GetInt(k_Vibration, 1);
        set => SetInt(k_Vibration, value);
    }

    public int PostProcessing
    {
        get => GetInt(k_PostProcessing, 1);
        set => SetInt(k_PostProcessing, value);
    }

    #endregion

    #region Privacy

    public bool Privacy
    {
        get => GetBool(k_Privacy, false);
        set => SetBool(k_Privacy, value);
    }

    #endregion

    #region TutorialWatched

    public bool TutorialWatched
    {
        get => GetBool(k_TutorialWatched, false);
        set => SetBool(k_TutorialWatched, value);
    }

    #endregion

    #region RateUsClicked

    public bool RateUsClicked
    {
        get => GetBool(k_RateUsClicked, false);
        set => SetBool(k_RateUsClicked, value);
    }

    #endregion

    #region LosesCount

    public int LosesCount
    {
        get => GetInt(k_LosesCount, 0);
        set => SetInt(k_LosesCount, value);
    }

    #endregion

    #region MaxScore

    public int MaxScore
    {
        get => GetInt(k_MaxScore, 0);
        set => SetInt(k_MaxScore, value);
    }
    #endregion

    #region CurrentLevel
    public static int CurrentLevel
    {
        get => Instance != null ? Instance.GetInt(k_CurrentLevel, 1) : PlayerPrefs.GetInt(k_CurrentLevel, 1);
        set { if (Instance != null) Instance.SetInt(k_CurrentLevel, value); else PlayerPrefs.SetInt(k_CurrentLevel, value); }
    }

    #endregion

    #region PlayerPrefs helpers

    private float GetFloat(string key, float def) => PlayerPrefs.GetFloat(key, def);
    private void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
    private int GetInt(string key, int def) => PlayerPrefs.GetInt(key, def);
    private void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    private bool GetBool(string key, bool def) => PlayerPrefs.GetInt(key, def ? 1 : 0) == 1;
    private void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);

    #endregion
}
