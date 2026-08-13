using UnityEngine;


//흡수된 대상의 티어를 유닛으로 변환하고 소환 위치를 결정
public class SummonConverter
{
    public LaneEntity CreateUnitEntity(string unitDataId)
    {
        UnitData data = GameDataManager.Instance.GetData<UnitData>(unitDataId);
        if (data == null)
        {
            return null;
        }

        LaneEntity entity = new LaneEntity();
        entity.Setup(
            data.Id,
            EntitySide.Ally,
            data.Hp,
            data.Attack,
            data.AttackInterval,
            data.Range,
            data.MoveSpeed,
            data.LifeTime,
            GetSpawnPosition(data.GetPlacementType()));

        entity.LanePositionX = Random.Range(-0.7f, 0.7f);
        return entity;
    }

    public LaneEntity CreateMonsterEntity(string monsterDataId)
    {
        MonsterData data = GameDataManager.Instance.GetData<MonsterData>(monsterDataId);
        if (data == null)
        {
            return null;
        }

        LaneEntity entity = new LaneEntity();
        entity.Setup(
            data.Id,
            EntitySide.Enemy,
            data.Hp,
            data.Attack,
            data.AttackInterval,
            data.Range,
            data.MoveSpeed,
            0f,
            0.95f);   // 최상단 스폰

        entity.LanePositionX = Random.Range(-0.85f, 0.85f);
        return entity;
    }

    public bool IsHealType(string unitDataId)
    {
        UnitData data = GameDataManager.Instance.GetData<UnitData>(unitDataId);
        if (data == null)
        {
            return false;
        }
        return data.GetPlacementType() == UnitPlacementType.Heal;
    }

    private float GetSpawnPosition(UnitPlacementType type)
    {
        switch (type)
        {
            case UnitPlacementType.Ranged: return 0.05f;      // 후방
            case UnitPlacementType.Melee: return 0.15f;       // 전방
            case UnitPlacementType.Structure: return 0.45f;   // 최전선
            case UnitPlacementType.Heal: return 0f;
            default: return 0.15f;
        }
    }
}