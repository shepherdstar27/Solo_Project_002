using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DefenseStripView : MonoBehaviour
{
    [SerializeField] private RectTransform RectTransform_LaneRoot;
    [SerializeField] private GameObject Prefab_LaneEntityView;
    [SerializeField] private Image Image_GateHpFill;
    [SerializeField] private SummonEffectView SummonEffectView_Gate;

    // 아군·적·전향한 보스의 이미지를 모아 둔 테이블. 비워 두면 아래 색으로만 표시된다
    [SerializeField] private LaneUnitIconTable LaneUnitIconTable_Icons;

    [SerializeField] private Color _colorAlly = new Color(0.35f, 0.65f, 1f);
    [SerializeField] private Color _colorEnemy = new Color(1f, 0.45f, 0.45f);
    [SerializeField] private Color _colorBossAlly = new Color(1f, 0.85f, 0.25f);

    private LaneSimulation _simulation;
    private DefenseGate _gate;
    private List<LaneEntityView> _entityViews = new List<LaneEntityView>();

    public void Bind(LaneSimulation simulation, DefenseGate gate)
    {
        _simulation = simulation;
        _gate = gate;

        if (LaneUnitIconTable_Icons == null)
        {
            Debug.LogWarning("[DefenseStripView] LaneUnitIconTable이 연결되지 않아 유닛이 색 박스로 표시됩니다");
        }

        _simulation.OnSpawnEntity += OnSpawnEntity;
        _simulation.OnRemoveEntity += OnRemoveEntity;
        _gate.OnChangeHp += OnChangeGateHp;
    }

    private void OnSpawnEntity(LaneEntity entity)
    {
        GameObject instance = Instantiate(Prefab_LaneEntityView, RectTransform_LaneRoot);
        LaneEntityView view = instance.GetComponent<LaneEntityView>();

        Color color = GetEntityColor(entity);

        // 이미지 조회는 여기서 한 번만 하고, 아이콘과 소환 연출이 같은 결과를 쓴다
        LaneUnitIconEntry icon = FindIcon(entity.DataId);

        view.Bind(entity, RectTransform_LaneRoot.rect.height, RectTransform_LaneRoot.rect.width, color, icon);

        _entityViews.Add(view);

        // 아군 소환 시 게이트에서 "뿅" 연출
        if (entity.Side == EntitySide.Ally && SummonEffectView_Gate != null)
        {
            SummonEffectView_Gate.PlaySummonEffect(icon, color);
        }
    }

    private LaneUnitIconEntry FindIcon(string dataId)
    {
        if (LaneUnitIconTable_Icons == null)
        {
            return null;
        }
        return LaneUnitIconTable_Icons.FindEntry(dataId);
    }

    private Color GetEntityColor(LaneEntity entity)
    {
        if (entity.Side != EntitySide.Ally)
        {
            return _colorEnemy;
        }

        // 전향한 보스는 눈에 띄게 다른 색으로
        if (entity.IsMarching)
        {
            return _colorBossAlly;
        }
        return _colorAlly;
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



}