using System;
using UnityEngine;

public enum UpgradeType
{
    MagicCircleRange,   // 마법진 범위
    MoveSpeed,          // 이동 속도
    TransferSpeed,      // 전송 가속
}

[Serializable]
public class UpgradeData : GameDataBase
{
    public string Name;
    public string Type;          // JSON에는 문자열로 저장
    public int BaseCost;
    public float CostGrowth;     // cost = BaseCost × CostGrowth^level
    public float ValuePerLevel;  // 레벨당 효과 증가량
    public int MaxLevel;

    public UpgradeType GetUpgradeType()
    {
        switch (Type)
        {
            case "MagicCircleRange": return UpgradeType.MagicCircleRange;
            case "MoveSpeed": return UpgradeType.MoveSpeed;
            case "TransferSpeed": return UpgradeType.TransferSpeed;
            default:
                Debug.LogError($"[UpgradeData] 알 수 없는 Type: {Type} (Id: {Id})");
                return UpgradeType.MagicCircleRange;
        }
    }

    public int GetCost(int level)
    {
        return Mathf.RoundToInt(BaseCost * Mathf.Pow(CostGrowth, level));
    }
}