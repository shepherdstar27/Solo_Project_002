using UnityEngine;

public static class SettingsData
{
    private const string KeyBgmVolume = "Settings_BgmVolume";
    private const string KeySfxVolume = "Settings_SfxVolume";

    public static float BgmVolume { get; private set; } = 0.7f;
    public static float SfxVolume { get; private set; } = 0.8f;

    public static void Load()
    {
        BgmVolume = PlayerPrefs.GetFloat(KeyBgmVolume, 0.7f);
        SfxVolume = PlayerPrefs.GetFloat(KeySfxVolume, 0.8f);
    }

    public static void SetBgmVolume(float value)
    {
        BgmVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeyBgmVolume, BgmVolume);
        PlayerPrefs.Save();
    }

    public static void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeySfxVolume, SfxVolume);
        PlayerPrefs.Save();
    }
}