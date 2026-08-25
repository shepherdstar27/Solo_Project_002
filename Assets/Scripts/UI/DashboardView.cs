using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DashboardView : MonoBehaviour
{
    [SerializeField] private Image Image_SpeedFill;
    [SerializeField] private RectTransform RectTransform_SpeedNeedle;
    [SerializeField] private TextMeshProUGUI Text_SpeedValue;

    [SerializeField] private Image Image_TierFill;
    [SerializeField] private TextMeshProUGUI Text_TierNumber;

    [SerializeField] private TextMeshProUGUI Text_Score;

    [SerializeField] private float _needleMinAngle = 120f;
    [SerializeField] private float _needleMaxAngle = -120f;
    [SerializeField] private float _maxSpeedForGauge = 14f;

    private TruckController _truckController;
    private TruckStatus _truckStatus;

    public void Bind(TruckController controller, TruckStatus status)
    {
        _truckController = controller;
        _truckStatus = status;

        _truckStatus.OnChangeScore += OnChangeScore;
        _truckStatus.OnChangeTierProgress += OnChangeTierProgress;
        _truckStatus.OnChangeTier += OnChangeTier;
    }

    private void OnChangeScore(int score)
    {
        if (Text_Score != null)
        {
            Text_Score.text = $"{score}";
        }
    }

    private void OnChangeTierProgress(float ratio)
    {
        if (Image_TierFill != null)
        {
            Image_TierFill.fillAmount = ratio;
        }
    }

    private void OnChangeTier(int tierNumber)
    {
        if (Text_TierNumber != null)
        {
            Text_TierNumber.text = $"T{tierNumber}";
        }
    }

    private void Update()
    {
        if (_truckController == null)
        {
            return;
        }

        float speed = Mathf.Abs(_truckController.CurrentSpeed);
        float ratio = Mathf.Clamp01(speed / _maxSpeedForGauge);

        if (Image_SpeedFill != null)
        {
            Image_SpeedFill.fillAmount = ratio;
        }

        if (RectTransform_SpeedNeedle != null)
        {
            float angle = Mathf.Lerp(_needleMinAngle, _needleMaxAngle, ratio);
            RectTransform_SpeedNeedle.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (Text_SpeedValue != null)
        {
            Text_SpeedValue.text = $"{Mathf.RoundToInt(speed * 8f)}";   // 표시용 배율
        }
    }

    private void OnDestroy()
    {
        if (_truckStatus == null)
        {
            return;
        }
        _truckStatus.OnChangeScore -= OnChangeScore;
        _truckStatus.OnChangeTierProgress -= OnChangeTierProgress;
        _truckStatus.OnChangeTier -= OnChangeTier;
    }
}