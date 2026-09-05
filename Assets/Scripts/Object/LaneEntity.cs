using System;
using UnityEngine;

public enum EntitySide
{
    Ally,
    Enemy,
}

public class LaneEntity
{
    public string DataId { get; private set; }
    public EntitySide Side { get; private set; }

    public float Hp { get; private set; }
    public float MaxHp { get; private set; }
    public int Attack { get; private set; }
    public float AttackInterval { get; private set; }
    public float Range { get; private set; }
    public float MoveSpeed { get; private set; }

    public float LanePosition { get; set; }    // 0 = 게이트, 1 = 최상단
    public float LanePositionX { get; set; }   // -1(좌) ~ 1(우)
    public float LifeTime { get; private set; }  // 0이면 무제한

    // 전향한 보스처럼 전선(0.5)을 넘어 적 본진(1.0)까지 밀고 올라가는 유닛
    public bool IsMarching { get; private set; }

    // 매 프레임 전수 탐색하지 않도록 현재 노리는 상대를 들고 있는다
    public LaneEntity Target { get; private set; }

    // 시뮬레이션 목록에서 빠진 개체. 살아 있어도 더 이상 타겟이 될 수 없다
    public bool IsRemoved { get; private set; }

    private float _attackCooldown;
    private float _elapsedLifeTime;

    public event Action<LaneEntity> OnDie;

    public void Setup(string dataId, EntitySide side, float maxHp, int attack,
        float attackInterval, float range, float moveSpeed, float lifeTime, float lanePosition)
    {
        DataId = dataId;
        Side = side;
        MaxHp = maxHp;
        Hp = maxHp;
        Attack = attack;
        AttackInterval = attackInterval;
        Range = range;
        MoveSpeed = moveSpeed;
        LifeTime = lifeTime;
        LanePosition = lanePosition;

        _attackCooldown = 0f;
        _elapsedLifeTime = 0f;
        Target = null;
        IsRemoved = false;
    }

    public void SetMarching(bool isMarching)
    {
        IsMarching = isMarching;
    }

    public void SetTarget(LaneEntity target)
    {
        Target = target;
    }

    public void SetRemoved()
    {
        IsRemoved = true;
        Target = null;
    }

    // 타겟이 죽었거나 목록에서 빠졌으면 다시 찾아야 한다
    public bool IsTargetValid()
    {
        if (Target == null)
        {
            return false;
        }
        if (Target.IsRemoved)
        {
            return false;
        }
        return Target.IsAlive();
    }

    public bool IsAlive()
    {
        return Hp > 0f;
    }

    public bool IsAttackReady()
    {
        return _attackCooldown <= 0f && Attack > 0;
    }

    public void UpdateCooldown(float deltaTime)
    {
        if (_attackCooldown > 0f)
        {
            _attackCooldown -= deltaTime;
        }
    }

    public void ConsumeAttack()
    {
        _attackCooldown = AttackInterval;
    }

    public bool UpdateLifeTime(float deltaTime)
    {
        if (LifeTime <= 0f)
        {
            return false;
        }

        _elapsedLifeTime += deltaTime;
        return _elapsedLifeTime >= LifeTime;
    }

    public void TakeDamage(float damage)
    {
        if (IsAlive() == false)
        {
            return;
        }

        Hp -= damage;
        if (Hp <= 0f)
        {
            Hp = 0f;
            if (OnDie != null)
            {
                OnDie.Invoke(this);
            }
        }
    }

    public void Kill()
    {
        TakeDamage(Hp);
    }
}