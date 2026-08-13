using System;
using UnityEngine;

public class DefenseSessionManager : SingletonBase<DefenseSessionManager>
{
    [SerializeField] private float _laneMoveScale = 0.15f;
    [SerializeField] private int _maxAllyCount = 30;

    private LaneSimulation _simulation;
    private DefenseGate _gate;
    private WaveSpawner _waveSpawner;
    private SummonConverter _converter;

    private StageData _stage;
    private float _sessionTime;
    private bool _isRunning;

    public LaneSimulation Simulation { get { return _simulation; } }
    public DefenseGate Gate { get { return _gate; } }
    public WaveSpawner Spawner { get { return _waveSpawner; } }

    public event Action<float, float> OnChangeSessionTime;   // 경과, 제한

    public void StartSession(string stageId)
    {
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
        _simulation.Setup(_gate, _maxAllyCount, _laneMoveScale);

        _waveSpawner = new WaveSpawner();
        _waveSpawner.Setup(_stage, _simulation, _converter);

        _sessionTime = 0f;
        _isRunning = true;

        GameManager.Instance.StartGame();
        Debug.Log($"[DefenseSessionManager] 세션 시작: {_stage.Id} / 제한 {_stage.TimeLimit}초");
    }

    public void SummonUnit(string unitDataId)
    {
        if (_isRunning == false)
        {
            return;
        }

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

    private void Update()
    {
        if (_isRunning == false)
        {
            return;
        }

        Debug.Log($"[Session] 경과 {_sessionTime:F1}초");

        float deltaTime = Time.deltaTime;
        _sessionTime += deltaTime;

        _waveSpawner.UpdateSpawner(_sessionTime);
        _simulation.UpdateSimulation(deltaTime);

        if (OnChangeSessionTime != null)
        {
            OnChangeSessionTime.Invoke(_sessionTime, _stage.TimeLimit);
        }

        if (_sessionTime >= _stage.TimeLimit)
        {
            EndSession(true);
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

        GameManager.Instance.EndGame(isClear);
        Debug.Log($"[DefenseSessionManager] 세션 종료 / 클리어: {isClear} / 남은 게이트 HP {_gate.Hp}");
    }

    private void OnDestroy()
    {
        if (_gate != null)
        {
            _gate.OnBreakGate -= OnBreakGate;
        }
    }
}