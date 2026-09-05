using System;
using UnityEngine;

public class DefenseSessionManager : SingletonBase<DefenseSessionManager>
{
    [SerializeField] private float _laneMoveScale = 0.15f;
    [SerializeField] private int _maxAllyCount = 0;   // 0 이하면 소환 수 제한 없음

    [Header("전선")]
    [SerializeField] private float _allyFrontLine = 0.5f;   // 아군이 넘지 않는 위치
    [SerializeField] private float _enemyHoldLine = 0.56f;  // 적이 사거리 안이라도 이 위에서는 멈추지 않음

    private LaneSimulation _simulation;
    private DefenseGate _gate;
    private WaveSpawner _waveSpawner;
    private SummonConverter _converter;

    private StageData _stage;
    private float _sessionTime;
    private bool _isRunning;
    private bool _isPaused;
    private bool _isMarchPhase;
    private int _transferCount;

    public LaneSimulation Simulation { get { return _simulation; } }
    public DefenseGate Gate { get { return _gate; } }
    public WaveSpawner Spawner { get { return _waveSpawner; } }

    public float SessionTime { get { return _sessionTime; } }
    public float TimeLimit { get { return _stage != null ? _stage.TimeLimit : 0f; } }

    public event Action<bool, int> OnFinishSession;          // 클리어 여부, 별 개수
    public event Action<float, float> OnChangeSessionTime;   // 경과, 제한
    public event Action OnReachEnemyBase;

    public void StartSession(string stageId)
    {
        _transferCount = 0;

        _stage = GameDataManager.Instance.GetData<StageData>(stageId);
        if (_stage == null)
        {
            Debug.LogError($"[DefenseSessionManager] 스테이지 없음: {stageId}");
            return;
        }

        _converter = new SummonConverter();

        _gate = new DefenseGate();
        _gate.Setup(_stage.GateHp);
        _gate.OnBreakGate += OnBreakGate;

        _simulation = new LaneSimulation();
        _simulation.Setup(_gate, _maxAllyCount, _laneMoveScale, _allyFrontLine, _enemyHoldLine);
        _simulation.OnReachEnemyBase += OnMarchReachEnemyBase;

        _waveSpawner = new WaveSpawner();
        _waveSpawner.Setup(_stage, _simulation, _converter);

        _sessionTime = 0f;
        _isRunning = true;
        _isPaused = false;
        _isMarchPhase = false;

        if (ClashManager.Instance != null)
        {
            ClashManager.Instance.ResetClash();
        }

        GameManager.Instance.StartGame();
        Debug.Log($"[DefenseSessionManager] 세션 시작: {_stage.Id} / 제한 {_stage.TimeLimit}초");
    }

    // 격돌 연출 동안 타이머와 시뮬레이션을 모두 멈춘다
    public void PauseSession()
    {
        _isPaused = true;
    }

    public void ResumeSession()
    {
        _isPaused = false;
    }

    // 보스를 아군으로 전향시켜 적 본진으로 진격시킨다.
    // 이 시점부터는 웨이브도 타이머도 돌지 않고 시뮬레이션만 돈다
    public void BeginMarch(BossTarget boss)
    {
        if (_simulation == null || boss == null)
        {
            return;
        }

        LaneEntity entity = new LaneEntity();
        entity.Setup(
            boss.AllyDataId,
            EntitySide.Ally,
            boss.AllyHp,
            boss.AllyAttack,
            boss.AllyAttackInterval,
            boss.AllyRange,
            boss.AllyMoveSpeed,
            0f,
            0f);

        entity.LanePositionX = 0f;
        entity.SetMarching(true);

        _simulation.AddEntity(entity);

        _isMarchPhase = true;
        _isPaused = false;

        // 제한 시간이 먼저 끝나 EndSession(false)가 이미 돌았을 수 있다.
        // 그 상태에서는 Update()가 첫 줄에서 빠져나가 진격이 한 프레임도 돌지 않으므로
        // 진격 연출을 위해 세션을 다시 열어 준다
        _isRunning = true;

        Debug.Log($"[DefenseSessionManager] 보스 전향 진격 시작: {boss.BossName}");
    }

    public void SummonUnit(string unitDataId)
    {
        if (_isRunning == false || _isPaused || _isMarchPhase)
        {
            return;
        }

        _transferCount++;

        if (_converter.IsHealType(unitDataId))
        {
            _gate.Heal(_gate.MaxHp * 0.25f, _gate.MaxHp * 0.1f);
            Debug.Log("[DefenseSessionManager] 왕성 회복 + 최대치 강화");
            return;
        }

        LaneEntity unit = _converter.CreateUnitEntity(unitDataId);
        if (unit == null)
        {
            return;
        }

        _simulation.AddEntity(unit);
    }

    public int GetTransferCount()
    {
        return _transferCount;
    }

    // 전향한 보스가 적 본진에 닿았을 때 클리어 처리
    public void FinishByBoss()
    {
        _isMarchPhase = false;
        EndSession(true);
    }

    private void Update()
    {
        if (_isRunning == false || _isPaused)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        // 진격 연출 중에는 시뮬레이션만 돌린다
        if (_isMarchPhase)
        {
            _simulation.UpdateSimulation(deltaTime);
            return;
        }

        _sessionTime += deltaTime;

        _waveSpawner.UpdateSpawner(_sessionTime);
        _simulation.UpdateSimulation(deltaTime);

        if (OnChangeSessionTime != null)
        {
            OnChangeSessionTime.Invoke(_sessionTime, _stage.TimeLimit);
        }

        // 제한 시간 안에 보스에 닿지 못하면 실패
        if (_sessionTime >= _stage.TimeLimit)
        {
            Debug.Log("[DefenseSessionManager] 제한 시간 초과 — 보스에 도달하지 못했습니다");
            EndSession(false);
        }
    }

    private void OnMarchReachEnemyBase(LaneEntity entity)
    {
        Debug.Log("[DefenseSessionManager] 적 본진 도달");

        if (OnReachEnemyBase != null)
        {
            OnReachEnemyBase.Invoke();
        }
    }

    private void OnBreakGate()
    {
        EndSession(false);
    }

    private void EndSession(bool isClear)
    {
        if (_isRunning == false)
        {
            return;
        }
        _isRunning = false;
        _isMarchPhase = false;

        int star = CalculateStar(isClear);
        GameManager.Instance.EndGame(isClear);

        if (OnFinishSession != null)
        {
            OnFinishSession.Invoke(isClear, star);
        }

        Debug.Log($"[DefenseSessionManager] 세션 종료 / 클리어 {isClear} / 별 {star} / 전송 {_transferCount}건");
    }

    private int CalculateStar(bool isClear)
    {
        if (isClear == false)
        {
            return 0;
        }

        float ratio = _gate.GetHpRatio();
        if (ratio >= 0.8f)
        {
            return 3;
        }
        if (ratio >= 0.4f)
        {
            return 2;
        }
        return 1;
    }

    private void OnDestroy()
    {
        if (_gate != null)
        {
            _gate.OnBreakGate -= OnBreakGate;
        }
        if (_simulation != null)
        {
            _simulation.OnReachEnemyBase -= OnMarchReachEnemyBase;
        }
    }
}
