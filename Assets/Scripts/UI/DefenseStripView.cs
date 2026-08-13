using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DefenseStripView : MonoBehaviour
{
    [SerializeField] private RectTransform RectTransform_LaneRoot;
    [SerializeField] private GameObject Prefab_LaneEntityView;
    [SerializeField] private Image Image_GateHpFill;
    [SerializeField] private SummonEffectView SummonEffectView_Gate;

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
        view.Bind(entity, RectTransform_LaneRoot.rect.height, RectTransform_LaneRoot.rect.width, color);

        _entityViews.Add(view);

        // 아군 소환 시 게이트에서 "뿅" 연출
        if (entity.Side == EntitySide.Ally && SummonEffectView_Gate != null)
        {
            SummonEffectView_Gate.PlaySummonEffect(color);
        }
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