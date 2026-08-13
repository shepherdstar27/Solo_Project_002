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