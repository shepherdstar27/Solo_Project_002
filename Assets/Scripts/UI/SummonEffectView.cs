using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// 아군이 소환될 때 게이트 위로 "뿅" 하고 뜨는 연출.
// 유닛별 이미지는 DefenseStripView가 LaneUnitIconTable에서 찾아 넘겨 준다.
public class SummonEffectView : MonoBehaviour
{
    [SerializeField] private RectTransform RectTransform_GateAnchor;
    [SerializeField] private GameObject Prefab_SummonPop;

    [SerializeField] private Sprite Sprite_Default;   // 테이블에 없는 유닛이 쓸 이미지

    [Header("연출")]
    [SerializeField] private float _popDuration = 0.45f;
    [SerializeField] private float _popRiseDistance = 70f;
    [SerializeField] private float _popScale = 1.4f;

    // fallbackColor는 등록된 이미지도 기본 이미지도 없을 때만 쓰인다 (예전 색 박스 동작)
    public void PlaySummonEffect(LaneUnitIconEntry icon, Color fallbackColor)
    {
        if (Prefab_SummonPop == null || RectTransform_GateAnchor == null)
        {
            return;
        }
        PlaySummonEffectAsync(icon, fallbackColor).Forget();
    }

    private async UniTask PlaySummonEffectAsync(LaneUnitIconEntry icon, Color fallbackColor)
    {
        GameObject instance = Instantiate(Prefab_SummonPop, RectTransform_GateAnchor);
        RectTransform rect = instance.GetComponent<RectTransform>();
        Image image = instance.GetComponent<Image>();

        float iconScale = 1f;
        Color baseColor = fallbackColor;

        if (image != null)
        {
            Sprite sprite = icon != null && icon.GetStaticSprite() != null ? icon.GetStaticSprite() : Sprite_Default;

            if (sprite != null)
            {
                image.sprite = sprite;
                // 이미지를 쓸 때는 진영 색으로 덮지 않고 지정한 틴트를 그대로 쓴다
                baseColor = icon != null ? icon.GetTint() : Color.white;
            }

            if (icon != null)
            {
                iconScale = icon.Scale;
            }

            image.color = baseColor;
        }

        Vector2 startPosition = Vector2.zero;
        float elapsed = 0f;

        while (elapsed < _popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _popDuration);

            // 확 커졌다가 작아지며 위로 떠오름
            float scale = Mathf.Sin(t * Mathf.PI) * _popScale * iconScale;
            rect.localScale = new Vector3(scale, scale, 1f);
            rect.anchoredPosition = startPosition + new Vector2(0f, _popRiseDistance * t);

            if (image != null)
            {
                Color fade = baseColor;
                fade.a = baseColor.a * (1f - t);
                image.color = fade;
            }

            await UniTask.Yield();
        }

        Destroy(instance);
    }
}
