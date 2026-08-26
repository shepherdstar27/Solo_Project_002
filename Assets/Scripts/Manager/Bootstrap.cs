using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Bootstrap : MonoBehaviour
{
    private async void Start()
    {
        await InitializeAsync();
    }

    private async UniTask InitializeAsync()
    {
        GameObject managers = await Addressables.InstantiateAsync("Managers").ToUniTask();
        if (managers == null)
        {
            Debug.LogError("[Bootstrap] Managers 프리팹 로드 실패");
            return;
        }

        GameDataManager.Instance.LoadAllData();

        GameObject environment = await Addressables.InstantiateAsync("Environment").ToUniTask();
        if (environment == null)
        {
            Debug.LogError("[Bootstrap] Environment 프리팹 로드 실패");
            return;
        }

        MainMenuUI mainMenu = await UIManager.Instance.OpenUIAsync<MainMenuUI>(UIAddress.MainMenu);
        if (mainMenu == null)
        {
            return;
        }

        mainMenu.OnClickStartGame += OnClickStartGame;
        AudioManager.Instance.PlayMenuBgm();
    }

    private void OnClickStartGame()
    {
        StartGameAsync().Forget();
    }

    private async UniTask StartGameAsync()
    {
        UIManager.Instance.CloseUI(UIAddress.MainMenu);
        AudioManager.Instance.PlayGameBgm();

        await StageLoader.Instance.LoadStageAsync();

        PauseManager.Instance.SetEnabled(true);
    }
}