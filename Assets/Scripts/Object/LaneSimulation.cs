using System;
using System.Collections.Generic;
using UnityEngine;

public class LaneSimulation
{
    private const float TargetingInterval = 0.2f;
    private const float MinGap = 0.05f;        // 개체 간 최소 거리 (2D)
    private const float WidthRatio = 0.25f;    // 가로:세로 화면 비율 보정

    private float _moveScale = 0.15f;   // 레인 이동 속도 계수 (밸런싱 값)

    private List<LaneEntity> _allies = new List<LaneEntity>();
    private List<LaneEntity> _enemies = new List<LaneEntity>();
    private List<LaneEntity> _removeBuffer = new List<LaneEntity>();

    private DefenseGate _gate;
    private float _targetingTimer;
    private int _maxAllyCount = 30;

    // 아군은 이 선을 넘지 않고, 적은 사거리 안이라도 이 선까지는 내려온다.
    // 두 값이 벌어져 있으면 사거리가 긴 원거리 적이 아군이 닿지 않는 위치에서 멈춰 교착이 생긴다
    private float _allyFrontLine = 0.5f;
    private float _enemyHoldLine = 0.56f;

    private bool _isMarchReported;

    public event Action<LaneEntity> OnSpawnEntity;
    public event Action<LaneEntity> OnRemoveEntity;
    public event Action<LaneEntity> OnReachEnemyBase;

    public void Setup(DefenseGate gate, int maxAllyCount, float moveScale, float allyFrontLine, float enemyHoldLine)
    {
        _gate = gate;
        _maxAllyCount = maxAllyCount;
        _moveScale = moveScale;
        _allyFrontLine = allyFrontLine;
        _enemyHoldLine = enemyHoldLine;
        _isMarchReported = false;
        _allies.Clear();
        _enemies.Clear();
    }

    public void AddEntity(LaneEntity entity)
    {
        if (entity.Side == EntitySide.Ally)
        {
            // 최대 소환 수 초과 시 가장 오래된 유닛 제거. 진격 중인 보스는 밀어내지 않는다.
            // _maxAllyCount가 0 이하면 상한 없이 계속 쌓는다
            if (_maxAllyCount > 0 && _allies.Count >= _maxAllyCount)
            {
                LaneEntity removable = FindRemovableAlly();
                if (removable != null)
                {
                    RemoveEntity(removable);
                }
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

             LaneEntity target = FindNearestTarget(enemy, _allies);

            // 사거리 내 아군이 있으면 정지 후 공격.
            // 단 전선보다 한참 위에서는 멈추지 않는다. 사거리가 긴 원거리 적이 저 위에 눌러앉으면
            // 전선(0.5)에 묶인 근접 아군이 영영 닿지 못해 일방적으로 얻어맞기만 한다
            if (target != null && GetDistance(enemy, target) <= enemy.Range
                && enemy.LanePosition <= _enemyHoldLine)
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

            // 타겟이 있으면 타겟 방향으로, 없으면 아래로 직진
            MoveToward(enemy, target, -1f, deltaTime);

            // 아래로 직진
            enemy.LanePosition -= enemy.MoveSpeed * _moveScale * deltaTime;
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

            // 전향한 보스는 전선을 넘어 적 본진까지 밀고 올라간다
            if (ally.IsMarching)
            {
                UpdateMarchingAlly(ally, deltaTime);
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

            // 근접 유닛만 짧은 전진 허용 (전선을 넘지 않음)
            if (ally.MoveSpeed > 0f)
            {
                MoveToward(ally, target, 1f, deltaTime);
                if (ally.LanePosition > _allyFrontLine)
                {
                    ally.LanePosition = _allyFrontLine;
                }
            }
        }
    }

    // 멈추지 않고 계속 전진하면서, 사거리에 들어온 적을 때린다
    private void UpdateMarchingAlly(LaneEntity ally, float deltaTime)
    {
        ally.UpdateCooldown(deltaTime);

        LaneEntity target = FindNearestTarget(ally, _enemies);
        if (target != null && GetDistance(ally, target) <= ally.Range && ally.IsAttackReady())
        {
            target.TakeDamage(ally.Attack);
            ally.ConsumeAttack();
        }

        ally.LanePosition += ally.MoveSpeed * _moveScale * deltaTime;

        if (ally.LanePosition < 1f)
        {
            return;
        }

        ally.LanePosition = 1f;

        if (_isMarchReported)
        {
            return;
        }
        _isMarchReported = true;

        if (OnReachEnemyBase != null)
        {
            OnReachEnemyBase.Invoke(ally);
        }
    }

    private LaneEntity FindRemovableAlly()
    {
        foreach (LaneEntity ally in _allies)
        {
            if (ally.IsMarching)
            {
                continue;
            }
            return ally;
        }
        return null;
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
        float dy = a.LanePosition - b.LanePosition;
        float dx = (a.LanePositionX - b.LanePositionX) * WidthRatio;
        return Mathf.Sqrt(dy * dy + dx * dx);
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

    private void MoveToward(LaneEntity self, LaneEntity target, float defaultDirectionY, float deltaTime)
    {
        float step = self.MoveSpeed * _moveScale * deltaTime;

        if (target == null)
        {
            self.LanePosition += defaultDirectionY * step;
            ClampPosition(self);
            return;
        }

        float dy = target.LanePosition - self.LanePosition;
        float dx = target.LanePositionX - self.LanePositionX;
        float length = Mathf.Sqrt(dy * dy + dx * dx);

        if (length < 0.0001f)
        {
            return;
        }

        self.LanePosition += (dy / length) * step;
        self.LanePositionX += (dx / length) * step * 2f;   // 가로는 더 빠르게 좁힘
        ClampPosition(self);
    }

    private void ClampPosition(LaneEntity entity)
    {
        if (entity.LanePosition < 0f)
        {
            entity.LanePosition = 0f;
        }
        if (entity.LanePositionX < -1f)
        {
            entity.LanePositionX = -1f;
        }
        if (entity.LanePositionX > 1f)
        {
            entity.LanePositionX = 1f;
        }
    }

    private void ResolveOverlap()
    {
        PushApart(_enemies);
        PushApart(_allies);
    }

    private void PushApart(List<LaneEntity> entities)
    {
        for (int i = 0; i < entities.Count; i++)
        {
            LaneEntity a = entities[i];
            if (a.IsAlive() == false)
            {
                continue;
            }

            for (int j = i + 1; j < entities.Count; j++)
            {
                LaneEntity b = entities[j];
                if (b.IsAlive() == false)
                {
                    continue;
                }

                float dy = b.LanePosition - a.LanePosition;
                float dx = (b.LanePositionX - a.LanePositionX) * WidthRatio;
                float distance = Mathf.Sqrt(dy * dy + dx * dx);

                if (distance >= MinGap || distance < 0.0001f)
                {
                    continue;
                }

                // 서로 절반씩 밀어냄
                float push = (MinGap - distance) * 0.5f;
                float normalY = dy / distance;
                float normalX = dx / distance;

                a.LanePosition -= normalY * push;
                b.LanePosition += normalY * push;
                a.LanePositionX -= normalX * push / WidthRatio;
                b.LanePositionX += normalX * push / WidthRatio;

                ClampPosition(a);
                ClampPosition(b);
            }
        }
    }
}