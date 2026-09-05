using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Bootstrap : MonoBehaviour
{
    // 다시하기로 씬을 리로드하면 이 오브젝트는 새로 생기지만
    // 메인 메뉴 UI는 UIManager(DontDestroyOnLoad)에 캐시되어 그대로 살아 있다.
    // 구독을 정리하지 않으면 이전 Bootstrap의 핸들러가 그대로 남아
    // 시작 버튼 한 번에 스테이지가 두 번 로드되고 트럭이 두 대 겹쳐 생성된다.
    private MainMenuUI _mainMenu;

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

        _mainMenu = await UIManager.Instance.OpenUIAsync<MainMenuUI>(UIAddress.MainMenu);
        if (_mainMenu == null)
        {
            return;
        }

        // 캐시된 UI를 다시 받은 경우를 대비해 한 번 떼고 붙인다
        _mainMenu.OnClickStartGame -= OnClickStartGame;
        _mainMenu.OnClickStartGame += OnClickStartGame;

        AudioManager.Instance.PlayMenuBgm();
    }

    private void OnDestroy()
    {
        if (_mainMenu == null)
        {
            return;
        }
        _mainMenu.OnClickStartGame -= OnClickStartGame;
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
