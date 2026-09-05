using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class StageLoader : SingletonBase<StageLoader>
{
    // StageLoader는 DontDestroyOnLoad라 씬을 다시 로드해도 살아남는다.
    // 시작 버튼이 두 번 전달되면 트럭이 두 대 겹쳐 생성되어 서로를 밀어내며 튕겨 나가므로
    // 중복 로드를 여기서 한 번 더 막는다
    private bool _isLoading;
    private bool _isLoaded;

    // 다시하기로 씬을 리로드하기 직전에 호출한다
    public void ResetLoader()
    {
        _isLoading = false;
        _isLoaded = false;
    }

    public async UniTask LoadStageAsync()
    {
        if (_isLoading || _isLoaded)
        {
            Debug.LogWarning("[StageLoader] 이미 스테이지를 불러왔습니다. 중복 호출을 무시합니다");
            return;
        }
        _isLoading = true;

        // 1. 인게임 캔버스 (조이스틱)
        GameObject canvas = await Addressables.InstantiateAsync("Canvas_InGame").ToUniTask();
        FloatingJoystick joystick = canvas.GetComponentInChildren<FloatingJoystick>();

        // 2. 트럭
        GameObject truck = await Addressables.InstantiateAsync("Truck").ToUniTask();
        TruckReference truckReference = truck.GetComponent<TruckReference>();

        if (truckReference == null)
        {
            Debug.LogError("[StageLoader] Truck 프리팹에 TruckReference가 없습니다");
            _isLoading = false;
            return;
        }

        TruckStatus status = truckReference.Status;
        TruckController controller = truckReference.Controller;
        TruckInput input = truckReference.Input;

        status.Initialize();

        //전송 문구 뷰
        TransferLogView transferLogView = canvas.GetComponentInChildren<TransferLogView>();
        if (transferLogView != null)
        {
            status.OnTransfer += transferLogView.AddLog;
        }

        //콤보 뷰
        ComboView comboView = canvas.GetComponentInChildren<ComboView>();
        if (comboView != null)
        {
            comboView.Bind(status.Combo);
        }

        //대시보드 뷰
        DashboardView dashboardView = canvas.GetComponentInChildren<DashboardView>();
        if (dashboardView != null)
        {
            dashboardView.Bind(controller, status);
        }

        GameObject showcaseObject = await Addressables.InstantiateAsync("Showcase").ToUniTask();
        TargetShowcase showcase = showcaseObject.GetComponent<TargetShowcase>();
        TargetShowcaseController.Instance.SetShowcase(showcase);

        // 3. 카메라 연결
        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(truckReference.BodyTransform, status, input);
            controller.SetCamera(Camera.main.transform);
        }
        else
        {
            Debug.LogError("[StageLoader] Main Camera에 CameraFollow가 없습니다");
        }


        // 4. 테스트 타겟 스포너
        //GameObject spawnerObject = await Addressables.InstantiateAsync("MapSpawner").ToUniTask();
        //MapSpawner mapSpawner = spawnerObject.GetComponent<MapSpawner>();
        //await mapSpawner.SpawnMapAsync();


        // 5. 디펜스 세션 시작
        GameObject canvasStrip = canvas.GetComponentInChildren<DefenseStripView>().gameObject;
        DefenseSessionManager.Instance.StartSession("Stage_01");

        DefenseStripView stripView = canvasStrip.GetComponent<DefenseStripView>();
        stripView.Bind(DefenseSessionManager.Instance.Simulation, DefenseSessionManager.Instance.Gate);

        _isLoading = false;
        _isLoaded = true;

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