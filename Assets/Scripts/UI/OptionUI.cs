using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionUI : UIBase
{
    [SerializeField] private Slider Slider_BgmVolume;
    [SerializeField] private Slider Slider_SfxVolume;
    [SerializeField] private TextMeshProUGUI Text_BgmValue;
    [SerializeField] private TextMeshProUGUI Text_SfxValue;
    [SerializeField] private Button Button_Close;

    private void Awake()
    {
        Slider_BgmVolume.onValueChanged.AddListener(OnChangeBgmVolume);
        Slider_SfxVolume.onValueChanged.AddListener(OnChangeSfxVolume);
        Button_Close.onClick.AddListener(OnClickClose);
    }

    public override void OnOpen()
    {
        SettingsData.Load();

        Slider_BgmVolume.SetValueWithoutNotify(SettingsData.BgmVolume);
        Slider_SfxVolume.SetValueWithoutNotify(SettingsData.SfxVolume);

        UpdateVolumeText();
    }

    private void OnClickClose()
    {
        Close();
    }

    private void OnChangeBgmVolume(float value)
    {
        SettingsData.SetBgmVolume(value);
        AudioManager.Instance.ApplyVolume();
        UpdateVolumeText();
    }

    private void OnChangeSfxVolume(float value)
    {
        SettingsData.SetSfxVolume(value);
        AudioManager.Instance.ApplyVolume();
        UpdateVolumeText();
    }

    private void UpdateVolumeText()
    {
        if (Text_BgmValue != null)
        {
            Text_BgmValue.text = $"{Mathf.RoundToInt(SettingsData.BgmVolume * 100f)}";
        }
        if (Text_SfxValue != null)
        {
            Text_SfxValue.text = $"{Mathf.RoundToInt(SettingsData.SfxVolume * 100f)}";
        }
    }
}