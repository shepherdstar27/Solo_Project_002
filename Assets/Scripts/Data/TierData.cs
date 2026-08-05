using System;

[Serializable]
public class TierData : GameDataBase
{
    public int SizeValue;
    public int PromoteScore;      // 이 티어 도달에 필요한 누적 점수 (시작 티어는 0 권장)
    public string SummonUnitId;   // 흡수 시 소환되는 UnitData Id
}