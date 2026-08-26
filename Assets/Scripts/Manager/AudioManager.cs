using UnityEngine;

public class AudioManager : SingletonBase<AudioManager>
{
    [SerializeField] private AudioSource AudioSource_Bgm;
    [SerializeField] private AudioClip AudioClip_MenuBgm;
    [SerializeField] private AudioClip AudioClip_GameBgm;

    private AudioSource _sfxSource;

    protected override void Awake()
    {
        base.Awake();
        SettingsData.Load();
        ApplyVolume();
    }

    public void RegisterSfxSource(AudioSource source)
    {
        _sfxSource = source;
        ApplyVolume();
    }

    public void ApplyVolume()
    {
        if (AudioSource_Bgm != null)
        {
            AudioSource_Bgm.volume = SettingsData.BgmVolume;
        }

        if (_sfxSource != null)
        {
            _sfxSource.volume = SettingsData.SfxVolume;
        }
    }

    public void PlayMenuBgm()
    {
        PlayBgm(AudioClip_MenuBgm);
    }

    public void PlayGameBgm()
    {
        PlayBgm(AudioClip_GameBgm);
    }

    private void PlayBgm(AudioClip clip)
    {
        if (AudioSource_Bgm == null || clip == null)
        {
            return;
        }

        if (AudioSource_Bgm.clip == clip && AudioSource_Bgm.isPlaying)
        {
            return;
        }

        AudioSource_Bgm.clip = clip;
        AudioSource_Bgm.loop = true;
        AudioSource_Bgm.Play();
    }
}