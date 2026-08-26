using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MapGeneratorWindow : EditorWindow
{
    private MapLayoutData _layout;
    private Transform _roadRoot;
    private Transform _objectRoot;

    private List<Vector3> _occupiedPositions = new List<Vector3>();
    private List<float> _occupiedRadii = new List<float>();

    [MenuItem("Tools/맵 생성기")]
    public static void OpenWindow()
    {
        GetWindow<MapGeneratorWindow>("맵 생성기");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("맵 레이아웃", EditorStyles.boldLabel);
        _layout = EditorGUILayout.ObjectField("레이아웃 에셋", _layout, typeof(MapLayoutData), false) as MapLayoutData;

        EditorGUILayout.Space();

        if (_layout == null)
        {
            EditorGUILayout.HelpBox("MapLayoutData 에셋을 지정하세요.\n프로젝트 창 우클릭 → Create → TruckGame → Map Layout", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("생성", EditorStyles.boldLabel);

        if (GUILayout.Button("도로 생성", GUILayout.Height(30)))
        {
            GenerateRoads();
        }

        if (GUILayout.Button("오브젝트 배치", GUILayout.Height(30)))
        {
            GenerateObjects();
        }

        if (GUILayout.Button("전체 생성 (도로 + 오브젝트)", GUILayout.Height(36)))
        {
            GenerateRoads();
            GenerateObjects();
        }

        EditorGUILayout.Space();

        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("전체 삭제", GUILayout.Height(26)))
        {
            ClearGenerated();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("생성 후 씬을 저장해야 결과가 유지됩니다.", MessageType.None);
    }

    private void GenerateRoads()
    {
        PrepareRoots();
        ClearChildren(_roadRoot);

        foreach (RoadSegment segment in _layout.RoadSegments)
        {
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = $"Road_{segment.Name}";
            road.transform.SetParent(_roadRoot);

            Vector3 size = segment.GetSize();
            road.transform.position = segment.GetCenter() + new Vector3(0f, 0.05f, 0f);
            road.transform.localScale = new Vector3(size.x, 0.1f, size.z);

            Collider roadCollider = road.GetComponent<Collider>();
            if (roadCollider != null)
            {
                DestroyImmediate(roadCollider);
            }

            if (_layout.Material_Road != null)
            {
                road.GetComponent<Renderer>().sharedMaterial = _layout.Material_Road;
            }

            Undo.RegisterCreatedObjectUndo(road, "도로 생성");
        }

        Debug.Log($"[맵 생성기] 도로 {_layout.RoadSegments.Count}개 생성");
    }

    private void GenerateObjects()
    {
        PrepareRoots();
        ClearChildren(_objectRoot);

        _occupiedPositions.Clear();
        _occupiedRadii.Clear();

        int totalCount = 0;

        foreach (MapZoneSetting zone in _layout.Zones)
        {
            List<ZoneTargetEntry> ordered = GetOrderedEntries(zone);

            foreach (ZoneTargetEntry entry in ordered)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    if (SpawnOne(zone, entry))
                    {
                        totalCount++;
                    }
                }
            }
        }

        Debug.Log($"[맵 생성기] 오브젝트 {totalCount}개 배치");
    }

    private List<ZoneTargetEntry> GetOrderedEntries(MapZoneSetting zone)
    {
        // 큰 것부터 배치해야 자리를 확보한다
        List<ZoneTargetEntry> ordered = new List<ZoneTargetEntry>();

        foreach (ZoneTargetEntry entry in zone.Targets)
        {
            if (entry.Prefab_Target == null)
            {
                continue;
            }

            int insertIndex = ordered.Count;
            for (int i = 0; i < ordered.Count; i++)
            {
                if (entry.VisualScale > ordered[i].VisualScale)
                {
                    insertIndex = i;
                    break;
                }
            }
            ordered.Insert(insertIndex, entry);
        }

        return ordered;
    }

    private bool SpawnOne(MapZoneSetting zone, ZoneTargetEntry entry)
    {
        Vector3 position;
        if (TryFindPosition(zone, entry, out position) == false)
        {
            return false;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(entry.Prefab_Target) as GameObject;
        if (instance == null)
        {
            return false;
        }

        instance.transform.SetParent(_objectRoot);
        instance.transform.position = position;
        instance.transform.localScale = Vector3.one * entry.VisualScale;
        instance.transform.rotation = CalculateRotation(position, entry);

        AbsorbableObject absorbable = instance.GetComponent<AbsorbableObject>();
        if (absorbable != null)
        {
            absorbable.SetupForEditor(entry.SizeValue, entry.Score, entry.TargetId);
            EditorUtility.SetDirty(absorbable);
        }

        _occupiedPositions.Add(position);
        _occupiedRadii.Add(entry.VisualScale * _layout.MinGapMultiplier);

        Undo.RegisterCreatedObjectUndo(instance, "오브젝트 배치");
        return true;
    }

    private bool TryFindPosition(MapZoneSetting zone, ZoneTargetEntry entry, out Vector3 position)
    {
        float targetDistance = GetTargetRoadDistance(entry.SizeValue);
        float myRadius = entry.VisualScale * _layout.MinGapMultiplier;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            float x = Random.Range(_layout.MapMinX, _layout.MapMaxX);
            float z = Random.Range(zone.MinZ, zone.MaxZ);
            Vector3 candidate = new Vector3(x, entry.VisualScale * 0.5f, z);

            if (IsNearIntersection(candidate))
            {
                continue;
            }

            float roadDistance = GetNearestRoadDistance(candidate);

            // 도로 위는 금지
            if (roadDistance < _layout.SidewalkDistance * 0.5f)
            {
                continue;
            }

            // 크기별 목표 거리에서 벗어나면 재시도 (여유 폭 허용)
            if (Mathf.Abs(roadDistance - targetDistance) > targetDistance * 0.8f)
            {
                continue;
            }

            if (IsPositionFree(candidate, myRadius) == false)
            {
                continue;
            }

            position = candidate;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    private float GetTargetRoadDistance(int sizeValue)
    {
        if (sizeValue <= 2)
        {
            return _layout.SidewalkDistance;
        }
        if (sizeValue <= 4)
        {
            return _layout.ParkingDistance;
        }
        return _layout.BuildingMinDistance + sizeValue * 1.5f;
    }

    private float GetNearestRoadDistance(Vector3 position)
    {
        float nearest = float.MaxValue;

        foreach (RoadSegment segment in _layout.RoadSegments)
        {
            float distance = segment.GetDistanceFromCenterLine(position) - segment.Width * 0.5f;
            if (distance < nearest)
            {
                nearest = distance;
            }
        }

        return nearest;
    }

    private RoadSegment GetNearestRoad(Vector3 position)
    {
        RoadSegment nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (RoadSegment segment in _layout.RoadSegments)
        {
            float distance = segment.GetDistanceFromCenterLine(position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = segment;
            }
        }

        return nearest;
    }

    private bool IsNearIntersection(Vector3 position)
    {
        // 세로 도로와 가로 도로가 만나는 지점 근처인지 검사
        foreach (RoadSegment a in _layout.RoadSegments)
        {
            if (a.Direction != RoadDirection.Vertical)
            {
                continue;
            }

            foreach (RoadSegment b in _layout.RoadSegments)
            {
                if (b.Direction != RoadDirection.Horizontal)
                {
                    continue;
                }

                Vector3 crossPoint = new Vector3(a.CenterX, 0f, b.CenterZ);
                Vector3 diff = position - crossPoint;
                diff.y = 0f;

                if (diff.sqrMagnitude < _layout.IntersectionClearRadius * _layout.IntersectionClearRadius)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private Quaternion CalculateRotation(Vector3 position, ZoneTargetEntry entry)
    {
        RoadSegment nearest = GetNearestRoad(position);
        if (nearest == null)
        {
            return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        // 차량(sizeValue 3~4)은 도로와 나란히
        if (entry.SizeValue >= 3 && entry.SizeValue <= 8)
        {
            Vector3 roadForward = nearest.GetForwardDirection();
            float flip = Random.value > 0.5f ? 0f : 180f;
            return Quaternion.LookRotation(roadForward) * Quaternion.Euler(0f, flip, 0f);
        }

        // 건물은 도로를 향하도록
        if (entry.SizeValue > 8)
        {
            Vector3 toRoad = GetDirectionToRoad(position, nearest);
            return Quaternion.LookRotation(toRoad);
        }

        // 작은 대상은 자유 회전
        return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }

    private Vector3 GetDirectionToRoad(Vector3 position, RoadSegment segment)
    {
        if (segment.Direction == RoadDirection.Vertical)
        {
            float dx = segment.CenterX - position.x;
            return new Vector3(Mathf.Sign(dx), 0f, 0f);
        }

        float dz = segment.CenterZ - position.z;
        return new Vector3(0f, 0f, Mathf.Sign(dz));
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

    private void PrepareRoots()
    {
        GameObject mapRoot = GameObject.Find("GeneratedMap");
        if (mapRoot == null)
        {
            mapRoot = new GameObject("GeneratedMap");
            Undo.RegisterCreatedObjectUndo(mapRoot, "맵 루트 생성");
        }

        _roadRoot = FindOrCreateChild(mapRoot.transform, "Roads");
        _objectRoot = FindOrCreateChild(mapRoot.transform, "Objects");
    }

    private Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject newChild = new GameObject(childName);
        newChild.transform.SetParent(parent);
        Undo.RegisterCreatedObjectUndo(newChild, "루트 생성");
        return newChild.transform;
    }

    private void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(root.GetChild(i).gameObject);
        }
    }

    private void ClearGenerated()
    {
        GameObject mapRoot = GameObject.Find("GeneratedMap");
        if (mapRoot != null)
        {
            DestroyImmediate(mapRoot);
            Debug.Log("[맵 생성기] 전체 삭제 완료");
        }
    }
}