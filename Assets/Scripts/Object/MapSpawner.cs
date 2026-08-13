using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class MapSpawner : MonoBehaviour
{
    [SerializeField] private int _poolCountPerTarget = 60;

    private List<GameObject> _spawnedObjects = new List<GameObject>();

    public async UniTask SpawnMapAsync()
    {
        List<AbsorbTargetData> targetDataList = GameDataManager.Instance.GetAllData<AbsorbTargetData>();

        // 1. 타겟 종류별 풀 생성
        foreach (AbsorbTargetData data in targetDataList)
        {
            GameObject prefab = await Addressables.LoadAssetAsync<GameObject>(data.PrefabKey).ToUniTask();
            if (prefab == null)
            {
                Debug.LogError($"[MapSpawner] 프리팹 로드 실패: {data.PrefabKey}");
                continue;
            }
            ObjectPoolManager.Instance.CreatePool(data.Id, prefab, _poolCountPerTarget);
        }

        // 2. 구역별 배치
        List<SpawnZoneData> zoneList = GameDataManager.Instance.GetAllData<SpawnZoneData>();
        foreach (SpawnZoneData zone in zoneList)
        {
            SpawnZone(zone);
        }

        Debug.Log($"[MapSpawner] 맵 배치 완료: 구역 {zoneList.Count}개 / 오브젝트 {_spawnedObjects.Count}개");
    }

    private void SpawnZone(SpawnZoneData zone)
    {
        List<string> targetIds = zone.GetTargetIds();
        List<int> counts = zone.GetCounts();

        for (int i = 0; i < targetIds.Count; i++)
        {
            int count = 0;
            if (i < counts.Count)
            {
                count = counts[i];
            }

            AbsorbTargetData data = GameDataManager.Instance.GetData<AbsorbTargetData>(targetIds[i]);
            if (data == null)
            {
                continue;
            }

            for (int n = 0; n < count; n++)
            {
                SpawnOne(zone, data);
            }
        }
    }

    private void SpawnOne(SpawnZoneData zone, AbsorbTargetData data)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(zone.InnerRadius, zone.OuterRadius);

        Vector3 position = new Vector3(
            zone.CenterX + Mathf.Cos(angle) * radius,
            data.VisualScale * 0.5f,
            zone.CenterZ + Mathf.Sin(angle) * radius);

        GameObject instance = ObjectPoolManager.Instance.GetObject(data.Id);
        if (instance == null)
        {
            return;
        }

        instance.transform.SetParent(transform);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        instance.transform.localScale = Vector3.one * data.VisualScale;

        AbsorbableObject target = instance.GetComponent<AbsorbableObject>();
        target.Initialize(data.SizeValue, data.Score, data.Id);

        _spawnedObjects.Add(instance);
    }
}