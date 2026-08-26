using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PauseUI : UIBase
{
    [SerializeField] private Button Button_Resume;
    [SerializeField] private Button Button_Option;
    [SerializeField] private Button Button_Quit;

    public event Action OnClickResume;

    private void Awake()
    {
        Button_Resume.onClick.AddListener(OnClickResumeButton);
        Button_Option.onClick.AddListener(OnClickOptionButton);
        Button_Quit.onClick.AddListener(OnClickQuitButton);
    }

    private void OnClickResumeButton()
    {
        PauseManager.Instance.Resume();
    }

    private void OnClickOptionButton()
    {
        OpenOptionAsync().Forget();
    }

    private async UniTask OpenOptionAsync()
    {
        await UIManager.Instance.OpenUIAsync<OptionUI>(UIAddress.Option);
    }

    private void OnClickQuitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}