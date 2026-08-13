using System;
using System.Collections.Generic;
using UnityEngine;


//티어·점수·크기 상태와 흡수 판정
public class TruckStatus : MonoBehaviour
{
    private List<TierData> _tierList = new List<TierData>();
    private int _tierIndex;          // _tierList의 현재 인덱스 (0부터)
    private int _currentScore;

    public int TruckSize { get; private set; }         // 현재 흡수 가능 sizeValue 상한
    public int CurrentTierNumber { get; private set; } // 표시용 티어 번호 (1부터)

    public event Action<AbsorbableObject> OnAbsorbTarget;
    public event Action<int> OnChangeTier;             // 새 티어 번호 전달

    public void Initialize()
    {
        _tierList = GameDataManager.Instance.GetAllData<TierData>();
        _tierList.Sort(CompareTierByScore);

        _tierIndex = 0;
        _currentScore = 0;
        ApplyTier();
    }

    public bool IsAbsorbable(AbsorbableObject target)
    {
        return target.SizeValue <= TruckSize;
    }

    public void AbsorbTarget(AbsorbableObject target)
    {
        _currentScore += target.Score;

        // 대상의 sizeValue에 해당하는 티어의 유닛을 소환
        string summonUnitId = FindSummonUnitId(target.SizeValue);
        if (string.IsNullOrEmpty(summonUnitId) == false)
        {
            DefenseSessionManager.Instance.SummonUnit(summonUnitId);
        }

        if (OnAbsorbTarget != null)
        {
            OnAbsorbTarget.Invoke(target);
        }

        CheckPromotion();
    }

    private string FindSummonUnitId(int sizeValue)
    {
        foreach (TierData tier in _tierList)
        {
            if (tier.SizeValue == sizeValue)
            {
                return tier.SummonUnitId;
            }
        }
        return null;
    }

    private void CheckPromotion()
    {
        while (_tierIndex + 1 < _tierList.Count
            && _currentScore >= _tierList[_tierIndex + 1].PromoteScore)
        {
            _tierIndex++;
            ApplyTier();

            Debug.Log($"[밸런스] 티어 {CurrentTierNumber} 도달 / 경과 {Time.timeSinceLevelLoad:F1}초 / 누적 {_currentScore}점");

            if (OnChangeTier != null)
            {
                OnChangeTier.Invoke(CurrentTierNumber);
            }
        }
    }

    private void ApplyTier()
    {
        TierData tier = _tierList[_tierIndex];
        TruckSize = tier.SizeValue;
        CurrentTierNumber = _tierIndex + 1;

        // 티어당 35%씩 스케일 업
        float scale = 1f + 0.35f * _tierIndex;
        transform.localScale = new Vector3(scale, scale, scale);

        Debug.Log($"[TruckStatus] 티어 {CurrentTierNumber} / 흡수 상한 {TruckSize} / 누적 점수 {_currentScore}");
    }

    private static int CompareTierByScore(TierData a, TierData b)
    {
        return a.PromoteScore.CompareTo(b.PromoteScore);
    }


}