using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SummonEffectView : MonoBehaviour
{
    [SerializeField] private RectTransform RectTransform_GateAnchor;
    [SerializeField] private GameObject Prefab_SummonPop;

    [SerializeField] private float _popDuration = 0.45f;
    [SerializeField] private float _popRiseDistance = 70f;

    public void PlaySummonEffect(Color color)
    {
        PlaySummonEffectAsync(color).Forget();
    }

    private async UniTask PlaySummonEffectAsync(Color color)
    {
        GameObject instance = Instantiate(Prefab_SummonPop, RectTransform_GateAnchor);
        RectTransform rect = instance.GetComponent<RectTransform>();
        Image image = instance.GetComponent<Image>();

        if (image != null)
        {
            image.color = color;
        }

        Vector2 startPosition = Vector2.zero;
        float elapsed = 0f;

        while (elapsed < _popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _popDuration);

            // 확 커졌다가 작아지며 위로 떠오름
            float scale = Mathf.Sin(t * Mathf.PI) * 1.4f;
            rect.localScale = new Vector3(scale, scale, 1f);
            rect.anchoredPosition = startPosition + new Vector2(0f, _popRiseDistance * t);

            if (image != null)
            {
                Color fade = image.color;
                fade.a = 1f - t;
                image.color = fade;
            }

            await UniTask.Yield();
        }

        Destroy(instance);
    }
}