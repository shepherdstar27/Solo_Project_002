using System;
using UnityEngine;

public class ComboSystem
{
    private float _fullDuration = 2f;      // 게이지가 가득 차 보이는 시간
    private float _decayDuration = 3f;     // 서서히 줄어드는 시간
    private float _remainTime;

    public int ComboCount { get; private set; }

    // 한 판 동안 도달한 최고 콤보. 콤보가 끊겨도 유지되고 Setup에서만 초기화된다
    public int MaxComboCount { get; private set; }

    public event Action<int> OnChangeCombo;
    public event Action<float> OnChangeGauge;   // 0~1
    public event Action OnResetCombo;

    public void Setup(float fullDuration, float decayDuration)
    {
        _fullDuration = fullDuration;
        _decayDuration = decayDuration;
        MaxComboCount = 0;
        Reset();
    }

    public void AddCombo()
    {
        ComboCount++;
        if (ComboCount > MaxComboCount)
        {
            MaxComboCount = ComboCount;
        }
        _remainTime = _fullDuration + _decayDuration;

        if (OnChangeCombo != null)
        {
            OnChangeCombo.Invoke(ComboCount);
        }
        if (OnChangeGauge != null)
        {
            OnChangeGauge.Invoke(1f);
        }
    }

    public void UpdateCombo(float deltaTime)
    {
        if (ComboCount <= 0)
        {
            return;
        }

        _remainTime -= deltaTime;

        if (_remainTime <= 0f)
        {
            Reset();
            return;
        }

        if (OnChangeGauge != null)
        {
            OnChangeGauge.Invoke(GetGaugeRatio());
        }
    }

    public float GetGaugeRatio()
    {
        // 앞 2초는 가득 찬 상태로 보이고, 남은 3초 동안 줄어든다
        if (_remainTime >= _decayDuration)
        {
            return 1f;
        }
        return Mathf.Clamp01(_remainTime / _decayDuration);
    }

    public float GetScoreMultiplier(float bonusPerCombo, float maxMultiplier)
    {
        float multiplier = 1f + bonusPerCombo * ComboCount;
        return Mathf.Min(multiplier, maxMultiplier);
    }

    private void Reset()
    {
        ComboCount = 0;
        _remainTime = 0f;

        if (OnChangeCombo != null)
        {
            OnChangeCombo.Invoke(0);
        }
        if (OnChangeGauge != null)
        {
            OnChangeGauge.Invoke(0f);
        }
        if (OnResetCombo != null)
        {
            OnResetCombo.Invoke();
        }
    }
}