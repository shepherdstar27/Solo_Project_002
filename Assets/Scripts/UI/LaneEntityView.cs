using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class LaneEntityView : MonoBehaviour
{
    [SerializeField] private Image Image_Body;
    [SerializeField] private Image Image_HpFill;
    [SerializeField] private RectTransform RectTransform_Root;

    private LaneEntity _entity;
    private float _laneHeight;
    private float _laneHalfWidth;

    public LaneEntity Entity { get; private set; }

    public void Bind(LaneEntity entity, float laneHeight, float laneWidth, Color bodyColor)
    {
        _entity = entity;
        Entity = entity;
        _laneHeight = laneHeight;
        _laneHalfWidth = laneWidth * 0.5f - 20f;

        Image_Body.color = bodyColor;
        UpdateView();
    }

    public void UpdateView()
    {
        if (_entity == null)
        {
            return;
        }

        Vector2 anchored = RectTransform_Root.anchoredPosition;
        anchored.x = _entity.LanePositionX * _laneHalfWidth;
        anchored.y = _entity.LanePosition * _laneHeight;
        RectTransform_Root.anchoredPosition = anchored;

        if (Image_HpFill != null && _entity.MaxHp > 0f)
        {
            Image_HpFill.fillAmount = _entity.Hp / _entity.MaxHp;
        }
    }

    public void PlayDefeat()
    {
        PlayDefeatAsync().Forget();
    }

    private async UniTask PlayDefeatAsync()
    {
        // 격파 연출: 살짝 커졌다가 축소되며 사라짐 (별 이펙트는 4주차 폴리싱에서 교체)
        float duration = 0.25f;
        float elapsed = 0f;
        Vector3 startScale = RectTransform_Root.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float scale = Mathf.Lerp(1.3f, 0f, t);
            RectTransform_Root.localScale = startScale * scale;

            await UniTask.Yield();
        }

        Destroy(gameObject);
    }
}