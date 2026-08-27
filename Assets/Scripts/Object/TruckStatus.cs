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

    // 채점용 집계
    public int CurrentScore { get { return _currentScore; } }
    public int AbsorbCount { get; private set; }

    public event Action<AbsorbableObject> OnAbsorbTarget;
    public event Action<int> OnChangeTier;             // 새 티어 번호 전달
    public event Action<string, string> OnTransfer;   // 대상 이름, 유닛 이름


    //콤보 관련
    [SerializeField] private float _comboFullDuration = 2f;
    [SerializeField] private float _comboDecayDuration = 3f;
    [SerializeField] private float _comboBonusPerCombo = 0.05f;   // 콤보당 5% 증가
    [SerializeField] private float _comboMaxMultiplier = 3f;

    private ComboSystem _comboSystem = new ComboSystem();
    public ComboSystem Combo { get { return _comboSystem; } }

    //트럭 계기판 관련
    public event Action<int> OnChangeScore;
    public event Action<float> OnChangeTierProgress;




    public void Initialize()
    {
        _tierList = GameDataManager.Instance.GetAllData<TierData>();
        _tierList.Sort(CompareTierByScore);

        foreach (TierData tier in _tierList)
        {
            Debug.Log($"[Tier] {tier.Id} / Size {tier.SizeValue} / Unit {tier.SummonUnitId}");
        }

        _comboSystem.Setup(_comboFullDuration, _comboDecayDuration);

        _tierIndex = 0;
        _currentScore = 0;
        AbsorbCount = 0;
        ApplyTier();
    }
    private void Update()
    {
        _comboSystem.UpdateCombo(Time.deltaTime);
    }

    public bool IsAbsorbable(AbsorbableObject target)
    {
        return target.SizeValue <= TruckSize;
    }

    public void AbsorbTarget(AbsorbableObject target)
    {
        AbsorbCount++;
        _comboSystem.AddCombo();

        float multiplier = _comboSystem.GetScoreMultiplier(_comboBonusPerCombo, _comboMaxMultiplier);
        int finalScore = Mathf.RoundToInt(target.Score * multiplier);
        _currentScore += finalScore;

        AbsorbFeedbackManager.Instance.PlayAbsorbFeedback(
            target.SizeValue,
            target.transform.position,
            finalScore);

        string summonUnitId = FindSummonUnitId(target.SizeValue);
        if (string.IsNullOrEmpty(summonUnitId) == false)
        {
            DefenseSessionManager.Instance.SummonUnit(summonUnitId);
            NotifyTransferLog(target.PoolKey, summonUnitId);
            TargetShowcaseController.Instance.ShowTarget(target.PoolKey);
        }

        if (OnAbsorbTarget != null)
        {
            OnAbsorbTarget.Invoke(target);
        }

        if (OnChangeScore != null)
        {
            OnChangeScore.Invoke(_currentScore);
        }
        NotifyTierProgress();

        CheckPromotion();
    }

    private void NotifyTransferLog(string targetId, string unitId)
    {
        AbsorbTargetData targetData = GameDataManager.Instance.GetData<AbsorbTargetData>(targetId);
        UnitData unitData = GameDataManager.Instance.GetData<UnitData>(unitId);

        if (targetData == null || unitData == null)
        {
            return;
        }

        if (OnTransfer != null)
        {
            OnTransfer.Invoke(targetData.Name, unitData.Name);
        }
    }

    private string FindSummonUnitId(int sizeValue)
    {
        foreach (TierData tier in _tierList)
        {
            if (tier.SizeValue == sizeValue)
            {
                Debug.Log($"[TruckStatus] sizeValue {sizeValue} → {tier.SummonUnitId}");
                return tier.SummonUnitId;
            }
        }

        Debug.LogWarning($"[TruckStatus] sizeValue {sizeValue}에 해당하는 티어 없음");
        return null;
    }

    private void CheckPromotion()
    {
        while (_tierIndex + 1 < _tierList.Count
            && _currentScore >= _tierList[_tierIndex + 1].PromoteScore)
        {
            _tierIndex++;
            ApplyTier();
            AbsorbFeedbackManager.Instance.PlayTierUpFeedback();

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

        // 트럭 크기는 고정 — 흡수 상한만 상승 (속도감 유지)

        Debug.Log($"[TruckStatus] 티어 {CurrentTierNumber} / 흡수 상한 {TruckSize} / 누적 점수 {_currentScore}");

        NotifyTierProgress();
    }

    private static int CompareTierByScore(TierData a, TierData b)
    {
        return a.PromoteScore.CompareTo(b.PromoteScore);
    }

    private void NotifyTierProgress()
    {
        if (OnChangeTierProgress == null)
        {
            return;
        }

        // 다음 티어가 없으면 가득 찬 상태
        if (_tierIndex + 1 >= _tierList.Count)
        {
            OnChangeTierProgress.Invoke(1f);
            return;
        }

        int currentThreshold = _tierList[_tierIndex].PromoteScore;
        int nextThreshold = _tierList[_tierIndex + 1].PromoteScore;
        int range = nextThreshold - currentThreshold;

        if (range <= 0)
        {
            OnChangeTierProgress.Invoke(1f);
            return;
        }

        float ratio = (float)(_currentScore - currentThreshold) / range;
        OnChangeTierProgress.Invoke(Mathf.Clamp01(ratio));
    }


}