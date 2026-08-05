using System;
using System.Collections.Generic;
using UnityEngine;

public class LaneSimulation
{
    private const float TargetingInterval = 0.2f;

    private List<LaneEntity> _allies = new List<LaneEntity>();
    private List<LaneEntity> _enemies = new List<LaneEntity>();
    private List<LaneEntity> _removeBuffer = new List<LaneEntity>();

    private DefenseGate _gate;
    private float _targetingTimer;
    private int _maxAllyCount = 30;

    public event Action<LaneEntity> OnSpawnEntity;
    public event Action<LaneEntity> OnRemoveEntity;

    public void Setup(DefenseGate gate, int maxAllyCount)
    {
        _gate = gate;
        _maxAllyCount = maxAllyCount;
        _allies.Clear();
        _enemies.Clear();
    }

    public void AddEntity(LaneEntity entity)
    {
        if (entity.Side == EntitySide.Ally)
        {
            // 최대 소환 수 초과 시 가장 오래된 유닛 제거
            if (_allies.Count >= _maxAllyCount)
            {
                RemoveEntity(_allies[0]);
            }
            _allies.Add(entity);
        }
        else
        {
            _enemies.Add(entity);
        }

        if (OnSpawnEntity != null)
        {
            OnSpawnEntity.Invoke(entity);
        }
    }

    public void UpdateSimulation(float deltaTime)
    {
        _targetingTimer -= deltaTime;
        bool isTargetingFrame = _targetingTimer <= 0f;
        if (isTargetingFrame)
        {
            _targetingTimer = TargetingInterval;
        }

        UpdateEnemies(deltaTime, isTargetingFrame);
        UpdateAllies(deltaTime, isTargetingFrame);
        CleanupDeadEntities();
    }

    private void UpdateEnemies(float deltaTime, bool isTargetingFrame)
    {
        foreach (LaneEntity enemy in _enemies)
        {
            if (enemy.IsAlive() == false)
            {
                continue;
            }

            enemy.UpdateCooldown(deltaTime);

            LaneEntity target = null;
            if (isTargetingFrame)
            {
                target = FindNearestTarget(enemy, _allies);
            }
            else
            {
                target = FindNearestTarget(enemy, _allies);
            }

            // 사거리 내 아군이 있으면 정지 후 공격
            if (target != null && GetDistance(enemy, target) <= enemy.Range)
            {
                if (enemy.IsAttackReady())
                {
                    target.TakeDamage(enemy.Attack);
                    enemy.ConsumeAttack();
                }
                continue;
            }

            // 게이트 도달 판정
            if (enemy.LanePosition <= 0f)
            {
                _gate.TakeDamage(enemy.Attack);
                enemy.Kill();
                continue;
            }

            // 아래로 직진
            enemy.LanePosition -= enemy.MoveSpeed * 0.1f * deltaTime;
            if (enemy.LanePosition < 0f)
            {
                enemy.LanePosition = 0f;
            }
        }
    }

    private void UpdateAllies(float deltaTime, bool isTargetingFrame)
    {
        foreach (LaneEntity ally in _allies)
        {
            if (ally.IsAlive() == false)
            {
                continue;
            }

            if (ally.UpdateLifeTime(deltaTime))
            {
                ally.Kill();
                continue;
            }

            ally.UpdateCooldown(deltaTime);

            LaneEntity target = FindNearestTarget(ally, _enemies);
            if (target == null)
            {
                continue;
            }

            float distance = GetDistance(ally, target);

            if (distance <= ally.Range)
            {
                if (ally.IsAttackReady())
                {
                    target.TakeDamage(ally.Attack);
                    ally.ConsumeAttack();
                }
                continue;
            }

            // 근접 유닛만 짧은 전진 허용
            if (ally.MoveSpeed > 0f)
            {
                ally.LanePosition += ally.MoveSpeed * 0.1f * deltaTime;
                if (ally.LanePosition > 1f)
                {
                    ally.LanePosition = 1f;
                }
            }
        }
    }

    private LaneEntity FindNearestTarget(LaneEntity self, List<LaneEntity> candidates)
    {
        LaneEntity nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (LaneEntity candidate in candidates)
        {
            if (candidate.IsAlive() == false)
            {
                continue;
            }

            float distance = GetDistance(self, candidate);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private float GetDistance(LaneEntity a, LaneEntity b)
    {
        return Mathf.Abs(a.LanePosition - b.LanePosition);
    }

    private void CleanupDeadEntities()
    {
        _removeBuffer.Clear();

        foreach (LaneEntity ally in _allies)
        {
            if (ally.IsAlive() == false)
            {
                _removeBuffer.Add(ally);
            }
        }
        foreach (LaneEntity enemy in _enemies)
        {
            if (enemy.IsAlive() == false)
            {
                _removeBuffer.Add(enemy);
            }
        }

        foreach (LaneEntity dead in _removeBuffer)
        {
            RemoveEntity(dead);
        }
    }

    private void RemoveEntity(LaneEntity entity)
    {
        if (entity.Side == EntitySide.Ally)
        {
            _allies.Remove(entity);
        }
        else
        {
            _enemies.Remove(entity);
        }

        if (OnRemoveEntity != null)
        {
            OnRemoveEntity.Invoke(entity);
        }
    }
}