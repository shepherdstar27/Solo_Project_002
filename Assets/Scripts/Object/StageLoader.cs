using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class StageLoader : SingletonBase<StageLoader>
{
    public async UniTask LoadStageAsync()
    {
        // 1. 인게임 캔버스 (조이스틱)
        GameObject canvas = await Addressables.InstantiateAsync("Canvas_InGame").ToUniTask();
        FloatingJoystick joystick = canvas.GetComponentInChildren<FloatingJoystick>();

        // 2. 트럭
        GameObject truck = await Addressables.InstantiateAsync("Truck").ToUniTask();
        TruckStatus status = truck.GetComponent<TruckStatus>();
        TruckController controller = truck.GetComponent<TruckController>();

        status.Initialize();
        controller.SetJoystick(joystick);

        // 3. 카메라 연결 (Main Camera에 CameraFollow 부착 전제)
        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(truck.transform, status);
        }
        else
        {
            Debug.LogError("[StageLoader] Main Camera에 CameraFollow가 없습니다");
        }

        // 4. 테스트 타겟 스포너
        await Addressables.InstantiateAsync("TargetSpawner").ToUniTask();

        Debug.Log("[StageLoader] 스테이지 로드 완료");
    }
}