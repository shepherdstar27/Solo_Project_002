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
    private UISpriteAnimator _animator;

    public LaneEntity Entity { get; private set; }

    // icon이 없거나 이미지가 비어 있으면 예전처럼 bodyColor로 칠한다
    public void Bind(LaneEntity entity, float laneHeight, float laneWidth, Color bodyColor, LaneUnitIconEntry icon)
    {
        _entity = entity;
        Entity = entity;
        _laneHeight = laneHeight;
        _laneHalfWidth = laneWidth * 0.5f - 20f;

        ApplyIcon(bodyColor, icon);
        UpdateView();
    }

    private void ApplyIcon(Color bodyColor, LaneUnitIconEntry icon)
    {
        if (Image_Body == null)
        {
            return;
        }

        if (icon == null || icon.GetStaticSprite() == null)
        {
            Image_Body.color = bodyColor;
            return;
        }

        Image_Body.sprite = icon.GetStaticSprite();

        // 이미지를 쓸 때는 진영 색으로 덮지 않고 지정한 틴트를 그대로 쓴다
        Image_Body.color = icon.GetTint();

        if (RectTransform_Root != null)
        {
            RectTransform_Root.localScale = Vector3.one * icon.Scale;
        }

        if (icon.IsAnimated())
        {
            GetAnimator().Play(Image_Body, icon.Sprite_Frames, icon.FramePerSecond);
        }
    }

    // 프리팹에 미리 붙여 두지 않아도 되도록 필요할 때 만든다
    private UISpriteAnimator GetAnimator()
    {
        if (_animator == null)
        {
            _animator = GetComponent<UISpriteAnimator>();
        }
        if (_animator == null)
        {
            _animator = gameObject.AddComponent<UISpriteAnimator>();
        }
        return _animator;
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