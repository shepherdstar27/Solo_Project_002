using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class ScorePopupSpawner : SingletonBase<ScorePopupSpawner>
{
    [SerializeField] private RectTransform RectTransform_PopupRoot;
    [SerializeField] private GameObject Prefab_ScorePopup;

    [SerializeField] private float _duration = 0.7f;
    [SerializeField] private float _riseDistance = 90f;

    public void SetRoot(RectTransform root)
    {
        RectTransform_PopupRoot = root;
    }

    public void SpawnPopup(Vector3 worldPosition, int score)
    {
        if (RectTransform_PopupRoot == null || Prefab_ScorePopup == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
        if (screenPoint.z < 0f)
        {
            return;   // 카메라 뒤쪽이면 표시하지 않음
        }

        GameObject instance = Instantiate(Prefab_ScorePopup, RectTransform_PopupRoot);
        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.position = screenPoint;

        TextMeshProUGUI text = instance.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"+{score}";
        }

        PlayPopupAsync(rect, text).Forget();
    }

    private async UniTask PlayPopupAsync(RectTransform rect, TextMeshProUGUI text)
    {
        Vector2 startPosition = rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _duration);

            rect.anchoredPosition = startPosition + new Vector2(0f, _riseDistance * t);

            // 처음엔 확 커졌다가 원래대로
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.4f;
            rect.localScale = new Vector3(scale, scale, 1f);

            if (text != null)
            {
                Color color = text.color;
                color.a = 1f - t * t;
                text.color = color;
            }

            await UniTask.Yield();
        }

        Destroy(rect.gameObject);
    }
}