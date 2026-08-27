using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 격돌 컷 화면. 좌측 트럭 / 우측 보스 / 가운데 불꽃.
// 스프라이트를 비워두면 색 패널만 보이므로 이미지 없이도 동작한다.
public class ClashView : UIBase
{
    [Header("컷")]
    [SerializeField] private RectTransform RectTransform_TruckRoot;
    [SerializeField] private RectTransform RectTransform_BossRoot;
    [SerializeField] private RectTransform RectTransform_SparkRoot;
    [SerializeField] private Image Image_Spark;

    [Header("연타")]
    [SerializeField] private Image Image_GaugeFill;
    [SerializeField] private TextMeshProUGUI Text_Prompt;
    [SerializeField] private TextMeshProUGUI Text_PressCount;

    [Header("채점 요소")]
    [SerializeField] private TextMeshProUGUI Text_BossName;
    [SerializeField] private TextMeshProUGUI Text_Speed;
    [SerializeField] private TextMeshProUGUI Text_Tier;
    [SerializeField] private TextMeshProUGUI Text_Score;
    [SerializeField] private TextMeshProUGUI Text_Combo;
    [SerializeField] private TextMeshProUGUI Text_Transfer;

    [Header("연출 값")]
    [SerializeField] private float _shakeAmount = 14f;
    [SerializeField] private float _sparkPulseSpeed = 18f;
    [SerializeField] private float _pushDuration = 1.1f;
    [SerializeField] private float _pushDistance = 1400f;

    private Vector2 _truckBasePosition;
    private Vector2 _bossBasePosition;
    private float _shakeTimer;

    public override void OnOpen()
    {
        base.OnOpen();

        if (RectTransform_TruckRoot != null)
        {
            _truckBasePosition = RectTransform_TruckRoot.anchoredPosition;
        }
        if (RectTransform_BossRoot != null)
        {
            _bossBasePosition = RectTransform_BossRoot.anchoredPosition;
        }
    }

    public void ShowClash(string bossName, ClashScore score, int requiredPressCount)
    {
        SetText(Text_BossName, bossName);
        SetText(Text_Speed, $"충돌 속도  {score.ImpactSpeedKph:F0} km/h");
        SetText(Text_Tier, $"티어  {score.TierNumber}");
        SetText(Text_Score, $"누적 점수  {score.AbsorbScore}");
        SetText(Text_Combo, $"최대 콤보  {score.MaxCombo}");
        SetText(Text_Transfer, $"전송 유닛  {score.TransferCount}");
        SetText(Text_Prompt, "SPACE 연타!");

        SetGauge(0f, 0, requiredPressCount);
    }

    public void SetGauge(float ratio, int pressCount, int requiredPressCount)
    {
        if (Image_GaugeFill != null)
        {
            Image_GaugeFill.fillAmount = ratio;
        }

        SetText(Text_PressCount, $"{pressCount} / {requiredPressCount}");

        // 게이지가 찰수록 불꽃이 커진다
        if (RectTransform_SparkRoot != null)
        {
            float scale = Mathf.Lerp(0.7f, 1.6f, ratio);
            RectTransform_SparkRoot.localScale = new Vector3(scale, scale, 1f);
        }
    }

    public void PlayPressFeedback()
    {
        _shakeTimer = 0.12f;
    }

    private void Update()
    {
        UpdateSpark();
        UpdateShake();
    }

    private void UpdateSpark()
    {
        if (Image_Spark == null)
        {
            return;
        }

        // 불꽃이 깜빡이는 느낌
        float alpha = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * _sparkPulseSpeed));
        Color color = Image_Spark.color;
        color.a = alpha;
        Image_Spark.color = color;
    }

    private void UpdateShake()
    {
        if (_shakeTimer <= 0f)
        {
            ApplyOffset(0f);
            return;
        }

        _shakeTimer -= Time.unscaledDeltaTime;
        ApplyOffset(Random.Range(-_shakeAmount, _shakeAmount));
    }

    private void ApplyOffset(float offset)
    {
        if (RectTransform_TruckRoot != null)
        {
            RectTransform_TruckRoot.anchoredPosition = _truckBasePosition + new Vector2(offset, 0f);
        }
        if (RectTransform_BossRoot != null)
        {
            RectTransform_BossRoot.anchoredPosition = _bossBasePosition - new Vector2(offset, 0f);
        }
    }

    // 보스가 오른쪽으로 밀려나는 연출
    public async UniTask PlayPushAsync()
    {
        SetText(Text_Prompt, "밀어냈다!");
        _shakeTimer = 0f;

        float elapsed = 0f;

        while (elapsed < _pushDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _pushDuration);
            float eased = t * t;

            if (RectTransform_BossRoot != null)
            {
                RectTransform_BossRoot.anchoredPosition = _bossBasePosition + new Vector2(_pushDistance * eased, 0f);
            }
            if (RectTransform_TruckRoot != null)
            {
                RectTransform_TruckRoot.anchoredPosition = _truckBasePosition + new Vector2(200f * eased, 0f);
            }
            if (RectTransform_SparkRoot != null)
            {
                float scale = Mathf.Lerp(1.6f, 0f, t);
                RectTransform_SparkRoot.localScale = new Vector3(scale, scale, 1f);
            }

            await UniTask.Yield();
        }
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target == null)
        {
            return;
        }
        target.text = value;
    }
}
