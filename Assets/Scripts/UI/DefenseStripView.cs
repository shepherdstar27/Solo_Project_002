using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DefenseStripView : MonoBehaviour
{
    [SerializeField] private RectTransform RectTransform_LaneRoot;
    [SerializeField] private GameObject Prefab_LaneEntityView;
    [SerializeField] private Image Image_GateHpFill;

    [SerializeField] private Color _colorAlly = new Color(0.35f, 0.65f, 1f);
    [SerializeField] private Color _colorEnemy = new Color(1f, 0.45f, 0.45f);

    private LaneSimulation _simulation;
    private DefenseGate _gate;
    private List<LaneEntityView> _entityViews = new List<LaneEntityView>();

    public void Bind(LaneSimulation simulation, DefenseGate gate)
    {
        _simulation = simulation;
        _gate = gate;

        _simulation.OnSpawnEntity += OnSpawnEntity;
        _simulation.OnRemoveEntity += OnRemoveEntity;
        _gate.OnChangeHp += OnChangeGateHp;
    }

    private void OnSpawnEntity(LaneEntity entity)
    {
        GameObject instance = Instantiate(Prefab_LaneEntityView, RectTransform_LaneRoot);
        LaneEntityView view = instance.GetComponent<LaneEntityView>();

        Color color = entity.Side == EntitySide.Ally ? _colorAlly : _colorEnemy;
        view.Bind(entity, RectTransform_LaneRoot.rect.height, color);

        _entityViews.Add(view);
        Debug.Log($"[Strip] 생성 {entity.Side} / LanePos {entity.LanePosition} / laneHeight {RectTransform_LaneRoot.rect.height} / anchoredY {instance.GetComponent<RectTransform>().anchoredPosition.y}");
    }

    private void OnRemoveEntity(LaneEntity entity)
    {
        for (int i = _entityViews.Count - 1; i >= 0; i--)
        {
            if (_entityViews[i].Entity != entity)
            {
                continue;
            }

            _entityViews[i].PlayDefeat();
            _entityViews.RemoveAt(i);
            return;
        }
    }

    private void OnChangeGateHp(float hp, float maxHp)
    {
        if (Image_GateHpFill == null || maxHp <= 0f)
        {
            return;
        }
        Image_GateHpFill.fillAmount = hp / maxHp;
    }

    private void LateUpdate()
    {
        foreach (LaneEntityView view in _entityViews)
        {
            view.UpdateView();
        }
    }

    private void OnDestroy()
    {
        if (_simulation != null)
        {
            _simulation.OnSpawnEntity -= OnSpawnEntity;
            _simulation.OnRemoveEntity -= OnRemoveEntity;
        }
        if (_gate != null)
        {
            _gate.OnChangeHp -= OnChangeGateHp;
        }
    }



    private LaneSimulation _testSimulation;

    private void Start()
    {
        DefenseGate gate = new DefenseGate();
        gate.Setup(100f);

        _testSimulation = new LaneSimulation();
        _testSimulation.Setup(gate, 30);
        Bind(_testSimulation, gate);

        // 아군 검사 1기 (게이트 앞)
        LaneEntity ally = new LaneEntity();
        ally.Setup("Unit_01", EntitySide.Ally, 30, 5, 1f, 0.12f, 1f, 0f, 0.15f);
        _testSimulation.AddEntity(ally);

        // 적 몬스터 2기 (상단)
        for (int i = 0; i < 2; i++)
        {
            LaneEntity enemy = new LaneEntity();
            enemy.Setup("Monster_01", EntitySide.Enemy, 20, 5, 1f, 0.1f, 1f, 0f, 1f - i * 0.15f);
            _testSimulation.AddEntity(enemy);
        }
    }

    private void Update()
    {
        if (_testSimulation != null)
        {
            _testSimulation.UpdateSimulation(Time.deltaTime);
        }
    }
}