using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AbsorbTargetSpawner : MonoBehaviour
{
    [SerializeField] private int _countPerRing = 24;
    [SerializeField] private float _ringSpacing = 22f;

    private async void Start()
    {
        await SpawnAllAsync();
    }

    private async UniTask SpawnAllAsync()
    {
        GameObject prefab = await Addressables.LoadAssetAsync<GameObject>("AbsorbTarget").ToUniTask();

        // 티어 1~5: sizeValue 1/2/4/8/16, 점수는 sizeValue와 동일 (테스트용)
        int[] sizeValues = { 1, 2, 4, 8, 16 };
        float[] visualScales = { 2f, 3f, 4f, 7f, 10f };

        for (int ring = 0; ring < sizeValues.Length; ring++)
        {
            float radius = _ringSpacing * (ring + 1);

            for (int i = 0; i < _countPerRing; i++)
            {
                float angle = (360f / _countPerRing) * i * Mathf.Deg2Rad;
                float radiusJitter = Random.Range(-_ringSpacing * 0.3f, _ringSpacing * 0.3f);
                float finalRadius = radius + radiusJitter;

                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * finalRadius,
                    visualScales[ring] * 0.5f,
                    Mathf.Sin(angle) * finalRadius);

                GameObject instance = Instantiate(prefab, position, Quaternion.identity, transform);
                instance.transform.localScale = Vector3.one * visualScales[ring];

                AbsorbableObject target = instance.GetComponent<AbsorbableObject>();
                target.Initialize(sizeValues[ring], sizeValues[ring]);
            }
        }

        Debug.Log($"[AbsorbTargetSpawner] 테스트 타겟 {sizeValues.Length * _countPerRing}개 배치 완료");
    }
}