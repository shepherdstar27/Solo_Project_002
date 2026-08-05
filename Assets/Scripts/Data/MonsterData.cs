using System;
using UnityEngine;

public enum MonsterType
{
    Melee,
    Ranged,
    Rush,
    Boss,
}

[Serializable]
public class MonsterData : GameDataBase
{
    public string Name;
    public int Hp;
    public int Attack;
    public float AttackInterval;
    public float Range;
    public float MoveSpeed;
    public string Type;   // JSON에는 문자열로 저장

    public MonsterType GetMonsterType()
    {
        switch (Type)
        {
            case "Melee": return MonsterType.Melee;
            case "Ranged": return MonsterType.Ranged;
            case "Rush": return MonsterType.Rush;
            case "Boss": return MonsterType.Boss;
            default:
                Debug.LogError($"[MonsterData] 알 수 없는 Type: {Type} (Id: {Id})");
                return MonsterType.Melee;
        }
    }
}