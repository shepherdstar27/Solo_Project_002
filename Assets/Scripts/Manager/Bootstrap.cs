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
        // 매니저 프리팹 동적 생성
        GameObject managers = await Addressables.InstantiateAsync("Managers").ToUniTask();
        if (managers == null)
        {
            Debug.LogError("[Bootstrap] Managers 프리팹 로드 실패");
            return;
        }

        GameDataManager.Instance.LoadAllData();
        await StageLoader.Instance.LoadStageAsync();

    }
}