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
        TruckInput input = truck.GetComponent<TruckInput>();

        status.Initialize();

        // 3. 카메라 연결 (Main Camera에 CameraFollow 부착 전제)
        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(truck.transform, status, input);
            controller.SetCamera(Camera.main.transform);
        }
        else
        {
            Debug.LogError("[StageLoader] Main Camera에 CameraFollow가 없습니다");
        }

        // 4. 테스트 타겟 스포너
        GameObject spawnerObject = await Addressables.InstantiateAsync("MapSpawner").ToUniTask();
        MapSpawner mapSpawner = spawnerObject.GetComponent<MapSpawner>();
        await mapSpawner.SpawnMapAsync();


        // 5. 디펜스 세션 시작
        GameObject canvasStrip = canvas.GetComponentInChildren<DefenseStripView>().gameObject;
        DefenseSessionManager.Instance.StartSession("Stage_01");

        DefenseStripView stripView = canvasStrip.GetComponent<DefenseStripView>();
        stripView.Bind(DefenseSessionManager.Instance.Simulation, DefenseSessionManager.Instance.Gate);

        Debug.Log("[StageLoader] 스테이지 로드 완료");

        // 99. 경고 뷰 연결
        DefenseWarningView warningView = canvas.GetComponentInChildren<DefenseWarningView>();
        if (warningView != null)
        {
            warningView.Bind(
                DefenseSessionManager.Instance.Gate,
                DefenseSessionManager.Instance.Spawner,
                DefenseSessionManager.Instance);
        }
    }
}