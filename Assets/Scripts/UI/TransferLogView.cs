using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class TransferLogView : MonoBehaviour
{
    [SerializeField] private RectTransform RectTransform_LogRoot;
    [SerializeField] private GameObject Prefab_LogEntry;

    [SerializeField] private int _maxEntryCount = 4;
    [SerializeField] private float _lifeTime = 2.2f;
    [SerializeField] private float _fadeTime = 0.5f;

    public void AddLog(string targetName, string unitName)
    {
        if (RectTransform_LogRoot == null || Prefab_LogEntry == null)
        {
            return;
        }

        // 오래된 항목이 넘치면 가장 위 항목 제거
        if (RectTransform_LogRoot.childCount >= _maxEntryCount)
        {
            Destroy(RectTransform_LogRoot.GetChild(0).gameObject);
        }

        GameObject instance = Instantiate(Prefab_LogEntry, RectTransform_LogRoot);
        TextMeshProUGUI text = instance.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"{targetName} → <color=#FFD34D>{unitName}</color>!";
        }

        PlayEntryAsync(instance, text).Forget();
    }

    private async UniTask PlayEntryAsync(GameObject instance, TextMeshProUGUI text)
    {
        RectTransform rect = instance.GetComponent<RectTransform>();

        // 등장: 살짝 튀어오름
        float appearTime = 0.18f;
        float elapsed = 0f;
        while (elapsed < appearTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / appearTime);
            float scale = Mathf.Lerp(0.6f, 1f, t);
            rect.localScale = new Vector3(scale, scale, 1f);
            await UniTask.Yield();
        }
        rect.localScale = Vector3.one;

        await UniTask.Delay(System.TimeSpan.FromSeconds(_lifeTime));

        // 소멸: 페이드 아웃
        elapsed = 0f;
        while (elapsed < _fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _fadeTime);

            if (text != null)
            {
                Color color = text.color;
                color.a = 1f - t;
                text.color = color;
            }
            await UniTask.Yield();
        }

        if (instance != null)
        {
            Destroy(instance);
        }
    }
}