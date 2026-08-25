using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class MapSpawner : MonoBehaviour
{
    [SerializeField] private int _poolCountPerTarget = 60;
    [SerializeField] private int _positionRetryCount = 12;
    [SerializeField] private float _minGapMultiplier = 1.4f;

    [SerializeField] private float _gridSize = 16f;
    [SerializeField] private float _gridJitter = 3f;
    [SerializeField] private float _clusterRadius = 34f;

    private List<GameObject> _spawnedObjects = new List<GameObject>();
    private List<Vector3> _occupiedPositions = new List<Vector3>();
    private List<float> _occupiedRadii = new List<float>();

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

        // 큰 오브젝트부터 배치해야 자리를 확보할 수 있다
        List<AbsorbTargetData> orderedData = new List<AbsorbTargetData>();
        List<int> orderedCounts = new List<int>();

        for (int i = 0; i < targetIds.Count; i++)
        {
            AbsorbTargetData data = GameDataManager.Instance.GetData<AbsorbTargetData>(targetIds[i]);
            if (data == null)
            {
                continue;
            }

            int count = 0;
            if (i < counts.Count)
            {
                count = counts[i];
            }

            int insertIndex = orderedData.Count;
            for (int j = 0; j < orderedData.Count; j++)
            {
                if (data.VisualScale > orderedData[j].VisualScale)
                {
                    insertIndex = j;
                    break;
                }
            }
            orderedData.Insert(insertIndex, data);
            orderedCounts.Insert(insertIndex, count);
        }

        for (int i = 0; i < orderedData.Count; i++)
        {
            for (int n = 0; n < orderedCounts[i]; n++)
            {
                SpawnOne(zone, orderedData[i]);
            }
        }
    }

    private void SpawnOne(SpawnZoneData zone, AbsorbTargetData data)
    {
        Vector3 position;
        if (TryFindPosition(zone, data, out position) == false)
        {
            return;
        }

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
        _occupiedPositions.Add(position);
        _occupiedRadii.Add(data.VisualScale * _minGapMultiplier);
    }

    private bool TryFindPosition(SpawnZoneData zone, AbsorbTargetData data, out Vector3 position)
    {
        float myRadius = data.VisualScale * _minGapMultiplier;

        for (int attempt = 0; attempt < _positionRetryCount; attempt++)
        {
            Vector3 candidate = GetCandidatePosition(zone, data);

            if (IsPositionFree(candidate, myRadius))
            {
                position = candidate;
                return true;
            }
        }

        position = Vector3.zero;
        return false;
    }

    private Vector3 GetCandidatePosition(SpawnZoneData zone, AbsorbTargetData data)
    {
        float x;
        float z;

        switch (zone.PlacementType)
        {
            case "Grid":
                x = SnapToGrid(Random.Range(zone.MinX, zone.MaxX));
                z = SnapToGrid(Random.Range(zone.MinZ, zone.MaxZ));
                x += Random.Range(-_gridJitter, _gridJitter);
                z += Random.Range(-_gridJitter, _gridJitter);
                break;

            case "Cluster":
                Vector2 center = GetClusterCenter(zone);
                x = center.x + Random.Range(-_clusterRadius, _clusterRadius);
                z = center.y + Random.Range(-_clusterRadius, _clusterRadius);
                break;

            default:
                x = Random.Range(zone.MinX, zone.MaxX);
                z = Random.Range(zone.MinZ, zone.MaxZ);
                break;
        }

        x = Mathf.Clamp(x, zone.MinX, zone.MaxX);
        z = Mathf.Clamp(z, zone.MinZ, zone.MaxZ);

        return new Vector3(x, data.VisualScale * 0.5f, z);
    }

    private float SnapToGrid(float value)
    {
        return Mathf.Round(value / _gridSize) * _gridSize;
    }

    private Vector2 GetClusterCenter(SpawnZoneData zone)
    {
        float unitX = (zone.MaxX - zone.MinX) / 4f;
        float unitZ = (zone.MaxZ - zone.MinZ) / 4f;

        int index = Random.Range(0, 3);
        if (index == 0)
        {
            return new Vector2(zone.MinX + unitX, zone.MinZ + unitZ);
        }
        if (index == 1)
        {
            return new Vector2(zone.MinX + unitX * 2f, zone.MinZ + unitZ * 3f);
        }
        return new Vector2(zone.MinX + unitX * 3f, zone.MinZ + unitZ * 2f);
    }

    private bool IsPositionFree(Vector3 candidate, float myRadius)
    {
        for (int i = 0; i < _occupiedPositions.Count; i++)
        {
            float requiredDistance = myRadius + _occupiedRadii[i];

            Vector3 diff = candidate - _occupiedPositions[i];
            diff.y = 0f;

            if (diff.sqrMagnitude < requiredDistance * requiredDistance)
            {
                return false;
            }
        }
        return true;
    }
}