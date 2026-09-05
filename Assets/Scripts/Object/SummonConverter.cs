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
            // 최상단 스폰. 한 웨이브가 한꺼번에 쏟아지므로 세로로도 조금 흩어 놓는다.
            // 같은 높이에 겹쳐 놓으면 한 줄로 딱 붙어 보인다
            Random.Range(0.86f, 0.99f));

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