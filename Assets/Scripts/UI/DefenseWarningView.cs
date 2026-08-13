using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DefenseWarningView : MonoBehaviour
{
    [SerializeField] private Image Image_DangerBorder;
    [SerializeField] private GameObject GameObject_WarningText;
    [SerializeField] private GameObject GameObject_WaveNotice;
    [SerializeField] private TextMeshProUGUI Text_Timer;

    [SerializeField] private float _dangerThreshold = 0.3f;
    [SerializeField] private float _blinkSpeed = 4f;
    [SerializeField] private float _noticeDuration = 2f;

    private bool _isDanger;

    private void Awake()
    {
        Image_DangerBorder.gameObject.SetActive(false);
        GameObject_WarningText.SetActive(false);
        GameObject_WaveNotice.SetActive(false);
    }

    public void Bind(DefenseGate gate, WaveSpawner spawner, DefenseSessionManager session)
    {
        gate.OnChangeHp += OnChangeGateHp;
        spawner.OnNoticeWave += OnNoticeWave;
        session.OnChangeSessionTime += OnChangeSessionTime;
    }

    private void OnChangeGateHp(float hp, float maxHp)
    {
        if (maxHp <= 0f)
        {
            return;
        }

        bool isDanger = (hp / maxHp) <= _dangerThreshold && hp > 0f;
        if (isDanger == _isDanger)
        {
            return;
        }

        _isDanger = isDanger;
        Image_DangerBorder.gameObject.SetActive(isDanger);
        GameObject_WarningText.SetActive(isDanger);
    }

    private void OnNoticeWave()
    {
        ShowNoticeAsync().Forget();
    }

    private async UniTask ShowNoticeAsync()
    {
        GameObject_WaveNotice.SetActive(true);
        await UniTask.Delay(System.TimeSpan.FromSeconds(_noticeDuration));
        GameObject_WaveNotice.SetActive(false);
    }

    private void OnChangeSessionTime(float elapsed, float limit)
    {
        if (Text_Timer == null)
        {
            return;
        }

        float remain = Mathf.Max(limit - elapsed, 0f);
        Text_Timer.text = $"{Mathf.CeilToInt(remain)}";
    }

    private void Update()
    {
        if (_isDanger == false)
        {
            return;
        }

        // 적색 테두리 점멸
        float alpha = (Mathf.Sin(Time.time * _blinkSpeed) + 1f) * 0.5f;
        Color color = Image_DangerBorder.color;
        color.a = alpha * 0.8f;
        Image_DangerBorder.color = color;
    }
}