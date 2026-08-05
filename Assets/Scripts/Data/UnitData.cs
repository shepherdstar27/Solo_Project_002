using System;
using UnityEngine;

public enum UnitPlacementType
{
    Melee,
    Ranged,
    Structure,
    Heal,
}

[Serializable]
public class UnitData : GameDataBase
{
    public string Name;
    public int Hp;
    public int Attack;
    public float AttackInterval;
    public float Range;
    public float MoveSpeed;        // 근접 유닛의 짧은 전진용. 0이면 고정
    public string PlacementType;
    public float LifeTime;         // 유닛 수명(초). 0이면 무제한

    public UnitPlacementType GetPlacementType()
    {
        switch (PlacementType)
        {
            case "Melee": return UnitPlacementType.Melee;
            case "Ranged": return UnitPlacementType.Ranged;
            case "Structure": return UnitPlacementType.Structure;
            case "Heal": return UnitPlacementType.Heal;
            default:
                Debug.LogError($"[UnitData] 알 수 없는 PlacementType: {PlacementType} (Id: {Id})");
                return UnitPlacementType.Melee;
        }
    }
}