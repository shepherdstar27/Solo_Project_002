using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComboView : MonoBehaviour
{
    [SerializeField] private GameObject GameObject_ComboRoot;
    [SerializeField] private TextMeshProUGUI Text_ComboCount;
    [SerializeField] private Image Image_GaugeFill;
    [SerializeField] private RectTransform RectTransform_CountRoot;

    [SerializeField] private Color _colorNormal = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color _colorDanger = new Color(1f, 0.35f, 0.3f);
    [SerializeField] private float _dangerThreshold = 0.35f;
    [SerializeField] private float _punchScale = 1.35f;
    [SerializeField] private float _punchDuration = 0.18f;

    private void Awake()
    {
        GameObject_ComboRoot.SetActive(false);
    }

    public void Bind(ComboSystem comboSystem)
    {
        comboSystem.OnChangeCombo += OnChangeCombo;
        comboSystem.OnChangeGauge += OnChangeGauge;
    }

    private void OnChangeCombo(int comboCount)
    {
        if (comboCount <= 0)
        {
            GameObject_ComboRoot.SetActive(false);
            return;
        }

        GameObject_ComboRoot.SetActive(true);
        Text_ComboCount.text = $"{comboCount}<size=60%> COMBO</size>";

        PlayPunchAsync().Forget();
    }

    private void OnChangeGauge(float ratio)
    {
        if (Image_GaugeFill == null)
        {
            return;
        }

        Image_GaugeFill.fillAmount = ratio;
        Image_GaugeFill.color = ratio <= _dangerThreshold ? _colorDanger : _colorNormal;
    }

    private async UniTask PlayPunchAsync()
    {
        float elapsed = 0f;
        while (elapsed < _punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _punchDuration);

            float scale = Mathf.Lerp(_punchScale, 1f, t);
            RectTransform_CountRoot.localScale = new Vector3(scale, scale, 1f);

            await UniTask.Yield();
        }
        RectTransform_CountRoot.localScale = Vector3.one;
    }
}