using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : UIBase
{
    [SerializeField] private Button Button_StartGame;
    [SerializeField] private Button Button_Option;
    [SerializeField] private Button Button_QuitGame;

    public event Action OnClickStartGame;

    private void Awake()
    {
        Button_StartGame.onClick.AddListener(OnClickStart);
        Button_Option.onClick.AddListener(OnClickOption);
        Button_QuitGame.onClick.AddListener(OnClickQuit);
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
        OpenOptionAsync().Forget();
    }

    private async UniTask OpenOptionAsync()
    {
        await UIManager.Instance.OpenUIAsync<OptionUI>(UIAddress.Option);
    }

    private void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}