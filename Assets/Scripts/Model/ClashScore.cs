using UnityEngine;

// 한 판의 채점 기록.
// 격돌 순간에 찍히는 값(속도·티어·점수 등)과 격돌 연타 결과를 함께 들고 있다.
public class ClashScore
{
    private const float RatedSpeedKph = 100f;      // 이 속도면 속도 점수 만점
    private const float ClashBaseSeconds = 8f;     // 이 시간보다 빨리 밀어내면 보너스
    private const int MaxTierNumber = 5;

    public float ImpactSpeedKph { get; private set; }
    public int TierNumber { get; private set; }
    public int AbsorbScore { get; private set; }
    public int AbsorbCount { get; private set; }
    public int MaxCombo { get; private set; }
    public int TransferCount { get; private set; }
    public float GateHpRatio { get; private set; }
    public float ReachTime { get; private set; }   // 보스에 도달하기까지 걸린 시간
    public float TimeLimit { get; private set; }

    public int PressCount { get; private set; }
    public float ClashDuration { get; private set; }

    public void SetImpact(float impactSpeedKph, int tierNumber, int absorbScore, int absorbCount,
        int maxCombo, int transferCount, float gateHpRatio, float reachTime, float timeLimit)
    {
        ImpactSpeedKph = impactSpeedKph;
        TierNumber = tierNumber;
        AbsorbScore = absorbScore;
        AbsorbCount = absorbCount;
        MaxCombo = maxCombo;
        TransferCount = transferCount;
        GateHpRatio = gateHpRatio;
        ReachTime = reachTime;
        TimeLimit = timeLimit;
    }

    public void SetClash(int pressCount, float clashDuration)
    {
        PressCount = pressCount;
        ClashDuration = clashDuration;
    }

    // 속도와 티어를 합친 격돌 위력 0~1.
    // 빠르고 크게 키워서 들이받을수록 연타를 덜 해도 보스가 밀린다
    public float GetImpactPower()
    {
        float speedRatio = Mathf.Clamp01(ImpactSpeedKph / RatedSpeedKph);
        float tierRatio = Mathf.Clamp01((TierNumber - 1f) / (MaxTierNumber - 1f));
        return speedRatio * 0.6f + tierRatio * 0.4f;
    }

    public int GetImpactBonus()
    {
        return Mathf.RoundToInt(ImpactSpeedKph * 20f);
    }

    public int GetTierBonus()
    {
        return Mathf.Max(0, TierNumber - 1) * 500;
    }

    public int GetComboBonus()
    {
        return MaxCombo * 30;
    }

    public int GetTransferBonus()
    {
        return TransferCount * 30;
    }

    public int GetGateBonus()
    {
        return Mathf.RoundToInt(Mathf.Clamp01(GateHpRatio) * 1000f);
    }

    public int GetTimeBonus()
    {
        return Mathf.RoundToInt(Mathf.Max(0f, TimeLimit - ReachTime) * 40f);
    }

    public int GetClashBonus()
    {
        return Mathf.RoundToInt(Mathf.Max(0f, ClashBaseSeconds - ClashDuration) * 100f);
    }

    public int GetTotalScore()
    {
        int total = AbsorbScore;
        total += GetImpactBonus();
        total += GetTierBonus();
        total += GetComboBonus();
        total += GetTransferBonus();
        total += GetGateBonus();
        total += GetTimeBonus();
        total += GetClashBonus();
        return total;
    }

    public string GetGrade()
    {
        int total = GetTotalScore();

        if (total >= 12000)
        {
            return "S";
        }
        if (total >= 8000)
        {
            return "A";
        }
        if (total >= 5000)
        {
            return "B";
        }
        if (total >= 2500)
        {
            return "C";
        }
        return "D";
    }
}
