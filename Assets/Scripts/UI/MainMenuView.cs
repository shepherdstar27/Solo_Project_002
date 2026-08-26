using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuView : MonoBehaviour
{
    [SerializeField] private GameObject GameObject_MenuPanel;
    [SerializeField] private GameObject GameObject_OptionPanel;

    [SerializeField] private Button Button_StartGame;
    [SerializeField] private Button Button_Option;
    [SerializeField] private Button Button_QuitGame;

    [SerializeField] private Slider Slider_BgmVolume;
    [SerializeField] private Slider Slider_SfxVolume;
    [SerializeField] private TextMeshProUGUI Text_BgmValue;
    [SerializeField] private TextMeshProUGUI Text_SfxValue;
    [SerializeField] private Button Button_CloseOption;

    public event Action OnClickStartGame;

    private void Awake()
    {
        Button_StartGame.onClick.AddListener(OnClickStart);
        Button_Option.onClick.AddListener(OnClickOption);
        Button_QuitGame.onClick.AddListener(OnClickQuit);
        Button_CloseOption.onClick.AddListener(OnClickCloseOption);

        Slider_BgmVolume.onValueChanged.AddListener(OnChangeBgmVolume);
        Slider_SfxVolume.onValueChanged.AddListener(OnChangeSfxVolume);

        GameObject_OptionPanel.SetActive(false);
    }

    private void Start()
    {
        SettingsData.Load();

        Slider_BgmVolume.SetValueWithoutNotify(SettingsData.BgmVolume);
        Slider_SfxVolume.SetValueWithoutNotify(SettingsData.SfxVolume);

        UpdateVolumeText();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        GameObject_MenuPanel.SetActive(true);
        GameObject_OptionPanel.SetActive(false);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnClickStart()
    {
        if (OnClickStartGame != null)
        {
            OnClickStartGame.Invoke();
        }
    }

    private void OnClickOption()
    {
        GameObject_MenuPanel.SetActive(false);
        GameObject_OptionPanel.SetActive(true);
    }

    private void OnClickCloseOption()
    {
        GameObject_OptionPanel.SetActive(false);
        GameObject_MenuPanel.SetActive(true);
    }

    private void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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