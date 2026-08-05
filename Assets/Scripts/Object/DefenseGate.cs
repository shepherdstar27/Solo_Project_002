using System;
using UnityEngine;

public class DefenseGate
{
    public float Hp { get; private set; }
    public float MaxHp { get; private set; }

    public event Action<float, float> OnChangeHp;   // 현재, 최대
    public event Action OnBreakGate;

    public void Setup(float maxHp)
    {
        MaxHp = maxHp;
        Hp = maxHp;
        NotifyChange();
    }

    public float GetHpRatio()
    {
        if (MaxHp <= 0f)
        {
            return 0f;
        }
        return Hp / MaxHp;
    }

    public bool IsAlive()
    {
        return Hp > 0f;
    }

    public void TakeDamage(float damage)
    {
        Debug.Log($"[Gate] 피격 {damage} / 남은 HP {Hp - damage}");

        if (IsAlive() == false)
        {
            return;
        }

        Hp -= damage;
        if (Hp <= 0f)
        {
            Hp = 0f;
            NotifyChange();

            if (OnBreakGate != null)
            {
                OnBreakGate.Invoke();
            }
            return;
        }

        NotifyChange();
    }

    public void Heal(float amount, float maxHpBonus)
    {
        MaxHp += maxHpBonus;
        Hp += amount;

        if (Hp > MaxHp)
        {
            Hp = MaxHp;
        }
        NotifyChange();
    }

    private void NotifyChange()
    {
        if (OnChangeHp != null)
        {
            OnChangeHp.Invoke(Hp, MaxHp);
        }
    }
}