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


        // 파이프라인 검증용 테스트 조회 (확인 후 삭제 예정)
        TierData tier1 = GameDataManager.Instance.GetData<TierData>("Tier_01");
        if (tier1 != null)
        {
            Debug.Log($"[Bootstrap] 검증: {tier1.Id} / SizeValue {tier1.SizeValue} / 소환 유닛 {tier1.SummonUnitId}");
        }

        Debug.Log("[Bootstrap] 초기화 완료");
    }
}