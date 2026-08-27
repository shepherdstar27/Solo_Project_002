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

    // 구간별로 미리 걸어둔 경로. 곡선 때문에 축 정렬 계산을 쓸 수 없어 전부 이걸로 질의한다
    private Dictionary<RoadSegment, RoadPath> _paths = new Dictionary<RoadSegment, RoadPath>();
    private Dictionary<RoadSegment, float> _tileLengths = new Dictionary<RoadSegment, float>();

    // 실제로 앞 구간에 이어붙었는지 (첫 구간은 이어붙일 대상이 없어 꺼진다)
    private Dictionary<RoadSegment, bool> _isConnected = new Dictionary<RoadSegment, bool>();

    // 구간 첫 타일이 앞 구간과 이루는 꺾임 각도(도). 구간 경계의 이음새 보정에 쓴다
    private Dictionary<RoadSegment, float> _startKinks = new Dictionary<RoadSegment, float>();

    // 경로를 만든 순서대로. 코스 진행도 계산에 쓴다
    private List<RoadSegment> _orderedSegments = new List<RoadSegment>();

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

        if (GUILayout.Button("도로 + 가드레일 생성", GUILayout.Height(30)))
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
        DrawSegmentSummary();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("생성 후 씬을 저장해야 결과가 유지됩니다.", MessageType.None);
    }

    // 경사·곡선 값을 눈으로 확인할 수 있게 요약을 띄운다
    private void DrawSegmentSummary()
    {
        EditorGUILayout.LabelField("구간 요약", EditorStyles.boldLabel);

        // 이어붙이기가 반영된 실제 좌표를 보여주려면 경로를 먼저 만들어야 한다
        BuildAllPaths();

        EditorGUILayout.LabelField("  코스 전체", $"{GetCourseTotalLength():F0}m / 구간 {_orderedSegments.Count}개");
        EditorGUILayout.Space(2);

        foreach (RoadSegment segment in _layout.RoadSegments)
        {
            if (segment.Prefab_Road == null)
            {
                EditorGUILayout.LabelField($"  {segment.Name}", "도로 프리팹 없음");
                continue;
            }

            RoadPath path = GetPath(segment);
            float tileLength = GetTileLength(segment);
            if (path == null || tileLength < 0.01f)
            {
                EditorGUILayout.LabelField($"  {segment.Name}", "길이 측정 실패");
                continue;
            }

            float angle = segment.GetSlopeAngle(tileLength);
            float totalDrop = segment.HeightDrop * segment.TileCount;
            float totalLength = tileLength * segment.TileCount;

            EditorGUILayout.LabelField(
                $"  {segment.Name}",
                $"길이 {totalLength:F0}m / 낙차 {totalDrop:F1}m / 경사 {angle:F1}°");

            string turnText;
            if (Mathf.Abs(segment.YawPerTile) < 0.001f)
            {
                turnText = "직선";
            }
            else
            {
                float radius = segment.GetTurnRadius(tileLength);
                string side = segment.YawPerTile > 0f ? "우" : "좌";
                turnText = $"{side}회전 총 {Mathf.Abs(segment.GetTotalTurnAngle()):F0}° / 반경 {radius:F0}m";
            }

            string wallText = segment.IsBuildWall && segment.Prefab_Wall != null ? "벽 O" : "벽 X";
            EditorGUILayout.LabelField(" ", $"{turnText} / {wallText}");

            EditorGUILayout.LabelField(" ", GetBuildModeText(segment, path, tileLength));

            EditorGUILayout.LabelField(" ", GetConnectionText(segment));

            Vector3 start = path.GetPoint(0);
            Vector3 end = path.GetEndPoint();
            EditorGUILayout.LabelField(
                " ",
                $"({start.x:F0}, {start.y:F0}, {start.z:F0}) → ({end.x:F0}, {end.y:F0}, {end.z:F0}) / 방위 {path.EndYaw:F0}°");
        }
    }

    private string GetBuildModeText(RoadSegment segment, RoadPath path, float tileLength)
    {
        if (segment.IsUseMesh == false)
        {
            return $"프리팹 타일 방식 / {GetSeamText(segment, tileLength)}";
        }

        float stepLength = tileLength / Mathf.Max(1, segment.MeshSubdivision);
        float roadRepeat = segment.RoadTextureLength;

        return $"메시 방식 (이음새 없음) / 조각 {stepLength:F1}m × {path.PointCount - 1}개 / 텍스처 {roadRepeat:F0}m마다 반복";
    }

    private string GetSeamText(RoadSegment segment, float tileLength)
    {
        float angle = Mathf.Abs(segment.YawPerTile);
        if (angle < 0.001f)
        {
            return "이음새 보정 불필요 (직선)";
        }

        float gap = segment.Width * 0.5f * Mathf.Tan(angle * Mathf.Deg2Rad);

        if (segment.SeamOverlap <= 0f)
        {
            return $"⚠ 이음새 보정 꺼짐 — 가장자리가 {gap:F2}m 벌어집니다";
        }

        float extend = segment.Width * Mathf.Tan(angle * Mathf.Deg2Rad) * segment.SeamOverlap;
        float scale = 1f + extend / tileLength;

        return $"이음새 틈 {gap:F2}m → 타일 {scale:F3}배로 덮음";
    }

    private string GetConnectionText(RoadSegment segment)
    {
        if (segment.IsConnectToPrevious == false)
        {
            return "시작 좌표 직접 지정";
        }

        if (IsConnected(segment) == false)
        {
            return "⚠ 이어붙일 앞 구간이 없어 StartX/Y/Z를 씁니다";
        }

        if (Mathf.Abs(segment.StartYaw) < 0.001f)
        {
            return "↳ 앞 구간에 연결";
        }

        return $"↳ 앞 구간에 연결 (꺾임 {segment.StartYaw:+0;-0}°)";
    }

    // ─────────────────────────────────────────────
    // 경로 준비
    // ─────────────────────────────────────────────

    private void BuildAllPaths()
    {
        _paths.Clear();
        _tileLengths.Clear();
        _isConnected.Clear();
        _startKinks.Clear();
        _orderedSegments.Clear();

        RoadPath previousPath = null;

        foreach (RoadSegment segment in _layout.RoadSegments)
        {
            if (segment.Prefab_Road == null)
            {
                continue;
            }

            float tileLength = GetPrefabLength(segment.Prefab_Road);
            if (tileLength < 0.01f)
            {
                continue;
            }

            RoadPath path;
            bool isConnected = segment.IsConnectToPrevious && previousPath != null && previousPath.PointCount > 0;

            // 메시로 만들 때는 곡선을 매끄럽게 하려고 경로를 잘게 쪼갠다
            int subdivision = segment.IsUseMesh ? Mathf.Max(1, segment.MeshSubdivision) : 1;

            if (isConnected)
            {
                // 앞 구간이 끝난 지점과 방위를 그대로 물려받고, StartYaw만 꺾임으로 더한다
                Vector3 startPosition = previousPath.GetEndPoint();
                float startYaw = previousPath.EndYaw + segment.StartYaw;
                path = segment.BuildPath(tileLength, subdivision, startPosition, startYaw);
            }
            else
            {
                path = segment.BuildPath(tileLength, subdivision, segment.GetStartPosition(), segment.GetBaseYaw());
            }

            _tileLengths[segment] = tileLength;
            _paths[segment] = path;
            _isConnected[segment] = isConnected;
            _startKinks[segment] = isConnected ? Mathf.Abs(segment.StartYaw) : 0f;
            _orderedSegments.Add(segment);

            previousPath = path;
        }
    }

    private bool IsConnected(RoadSegment segment)
    {
        bool isConnected;
        if (_isConnected.TryGetValue(segment, out isConnected))
        {
            return isConnected;
        }
        return false;
    }

    // ─────────────────────────────────────────────
    // 이음새 보정
    // ─────────────────────────────────────────────

    // 타일 i가 앞뒤 타일과 이루는 각도 차이 중 큰 쪽(도).
    // 강체 타일은 이 각도만큼 이음면이 벌어지므로, 이 값으로 늘려줄 양을 정한다
    private float GetJointAngle(RoadSegment segment, RoadPath path, int index)
    {
        float current = path.GetYaw(index);

        float backward;
        if (index <= 0)
        {
            // 구간의 첫 타일은 앞 구간과의 꺾임을 본다
            float kink;
            if (_startKinks.TryGetValue(segment, out kink) == false)
            {
                kink = 0f;
            }
            backward = kink;
        }
        else
        {
            backward = Mathf.Abs(current - path.GetYaw(index - 1));
        }

        float forward = Mathf.Abs(path.GetYaw(index + 1) - current);

        return Mathf.Max(backward, forward);
    }

    // 이음면을 덮기 위해 진행 방향으로 늘려야 하는 길이.
    // 중심선에서 폭/2 떨어진 가장자리의 벌어짐이 (폭/2)·tan(각도)이고, 앞뒤 양쪽이라 폭·tan(각도)
    private float GetSeamExtend(RoadSegment segment, RoadPath path, int index)
    {
        if (segment.SeamOverlap <= 0f)
        {
            return 0f;
        }

        float angle = Mathf.Clamp(GetJointAngle(segment, path, index), 0f, 60f);
        if (angle < 0.001f)
        {
            return 0f;
        }

        return segment.Width * Mathf.Tan(angle * Mathf.Deg2Rad) * segment.SeamOverlap;
    }

    private RoadPath GetPath(RoadSegment segment)
    {
        RoadPath path;
        if (_paths.TryGetValue(segment, out path))
        {
            return path;
        }
        return null;
    }

    private float GetTileLength(RoadSegment segment)
    {
        float length;
        if (_tileLengths.TryGetValue(segment, out length))
        {
            return length;
        }
        return 0f;
    }

    // ─────────────────────────────────────────────
    // 도로 + 가드레일 생성
    // ─────────────────────────────────────────────

    private void GenerateRoads()
    {
        PrepareRoots();
        ClearChildren(_roadRoot);
        BuildAllPaths();

        int totalTiles = 0;
        int totalWalls = 0;

        foreach (RoadSegment segment in _layout.RoadSegments)
        {
            if (segment.Prefab_Road == null)
            {
                Debug.LogError($"[맵 생성기] 도로 프리팹이 없습니다: {segment.Name}");
                continue;
            }

            RoadPath path = GetPath(segment);
            float tileLength = GetTileLength(segment);

            if (path == null || tileLength < 0.01f)
            {
                Debug.LogError($"[맵 생성기] 도로 길이를 잴 수 없습니다: {segment.Name}");
                continue;
            }

            if (Mathf.Abs(segment.HeightDrop) >= tileLength)
            {
                Debug.LogWarning($"[맵 생성기] HeightDrop이 타일 길이({tileLength:F1}m)보다 큽니다: {segment.Name}. 경사가 제한됩니다");
            }

            WarnIfCurveTooTight(segment, tileLength);

            // 메시 방식은 타일을 아예 놓지 않는다
            if (segment.IsUseMesh)
            {
                totalTiles += BuildMeshRoad(segment, path);
                continue;
            }

            GameObject groupRoot = new GameObject($"Road_{segment.Name}");
            groupRoot.transform.SetParent(_roadRoot);
            groupRoot.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(groupRoot, "도로 그룹 생성");

            for (int i = 0; i < segment.TileCount; i++)
            {
                GameObject tile = PrefabUtility.InstantiatePrefab(segment.Prefab_Road) as GameObject;
                if (tile == null)
                {
                    continue;
                }

                tile.transform.SetParent(groupRoot.transform);
                tile.transform.position = path.GetPoint(i);
                tile.transform.rotation = path.GetRotation(i);

                // 곡선 이음새를 덮도록 진행 방향으로 살짝 늘린다
                float extend = GetSeamExtend(segment, path, i);
                if (extend > 0.001f)
                {
                    Vector3 scale = tile.transform.localScale;
                    scale.z *= 1f + extend / tileLength;
                    tile.transform.localScale = scale;
                }

                GameObjectUtility.SetStaticEditorFlags(tile, 0);

                Undo.RegisterCreatedObjectUndo(tile, "도로 타일 생성");
                totalTiles++;
            }

            totalWalls += BuildWalls(segment, path, tileLength);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[맵 생성기] 도로 {_layout.RoadSegments.Count}구간 / 조각 {totalTiles}개 / 가드레일 {totalWalls}개 생성");
    }

    // ─────────────────────────────────────────────
    // 메시 방식 도로
    // ─────────────────────────────────────────────

    private const string MeshFolderName = "GeneratedMeshes";

    private int BuildMeshRoad(RoadSegment segment, RoadPath path)
    {
        if (path.PointCount < 2)
        {
            Debug.LogError($"[맵 생성기] 경로가 너무 짧아 메시를 만들 수 없습니다: {segment.Name}");
            return 0;
        }

        int created = 0;

        List<Vector2> roadProfile = RoadMeshBuilder.CreateRoadProfile(segment.Width);
        Mesh roadMesh = RoadMeshBuilder.Build(path, roadProfile, segment.RoadTextureLength, $"RoadMesh_{segment.Name}");

        if (segment.Material_Road == null)
        {
            Debug.LogWarning($"[맵 생성기] Material_Road가 비어 있습니다: {segment.Name}. 분홍색으로 보입니다");
        }

        created += SpawnMeshObject($"Road_{segment.Name}", roadMesh, segment.Material_Road);

        if (segment.IsBuildWall)
        {
            if (segment.Material_Wall == null)
            {
                Debug.LogWarning($"[맵 생성기] Material_Wall이 비어 있습니다: {segment.Name}. 분홍색으로 보입니다");
            }

            List<Vector2> rightProfile = RoadMeshBuilder.CreateWallProfile(
                segment.WallOffset, segment.WallHeightOffset, segment.WallHeight, segment.WallThickness);

            List<Vector2> leftProfile = RoadMeshBuilder.CreateWallProfile(
                -segment.WallOffset, segment.WallHeightOffset, segment.WallHeight, segment.WallThickness);

            Mesh rightMesh = RoadMeshBuilder.Build(path, rightProfile, segment.WallTextureLength, $"WallMesh_{segment.Name}_R");
            Mesh leftMesh = RoadMeshBuilder.Build(path, leftProfile, segment.WallTextureLength, $"WallMesh_{segment.Name}_L");

            created += SpawnMeshObject($"Wall_{segment.Name}_R", rightMesh, segment.Material_Wall);
            created += SpawnMeshObject($"Wall_{segment.Name}_L", leftMesh, segment.Material_Wall);
        }

        Debug.Log($"[맵 생성기] {segment.Name} 메시 생성 / 단면 {path.PointCount}개 / 노면 정점 {roadMesh.vertexCount}개");
        return created;
    }

    private int SpawnMeshObject(string objectName, Mesh mesh, Material material)
    {
        Mesh saved = SaveMeshAsset(mesh, objectName);

        GameObject instance = new GameObject(objectName);
        instance.transform.SetParent(_roadRoot);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;

        MeshFilter filter = instance.AddComponent<MeshFilter>();
        filter.sharedMesh = saved;

        MeshRenderer renderer = instance.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        MeshCollider collider = instance.AddComponent<MeshCollider>();
        collider.sharedMesh = saved;

        GameObjectUtility.SetStaticEditorFlags(instance, 0);
        Undo.RegisterCreatedObjectUndo(instance, "도로 메시 생성");
        return 1;
    }

    // 씬에만 들고 있으면 씬 파일이 비대해지고 관리가 어렵다. 에셋으로 저장한다.
    // Editor 폴더 안에 두면 빌드에서 빠지므로 Assets 바로 아래에 만든다
    private Mesh SaveMeshAsset(Mesh mesh, string fileName)
    {
        string folderPath = $"Assets/{MeshFolderName}";

        if (AssetDatabase.IsValidFolder(folderPath) == false)
        {
            AssetDatabase.CreateFolder("Assets", MeshFolderName);
        }

        string assetPath = $"{folderPath}/{fileName}.asset";
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(mesh, assetPath);

        return mesh;
    }

    private void WarnIfCurveTooTight(RoadSegment segment, float tileLength)
    {
        if (Mathf.Abs(segment.YawPerTile) < 0.001f)
        {
            return;
        }

        float radius = segment.GetTurnRadius(tileLength);
        if (radius < segment.WallOffset * 1.5f)
        {
            Debug.LogWarning(
                $"[맵 생성기] 곡선이 너무 급합니다: {segment.Name} (반경 {radius:F0}m, 벽 간격 {segment.WallOffset:F0}m). " +
                $"YawPerTile을 줄이거나 WallOffset을 줄이세요");
        }
    }

    private int BuildWalls(RoadSegment segment, RoadPath path, float tileLength)
    {
        if (segment.IsBuildWall == false || segment.Prefab_Wall == null)
        {
            return 0;
        }

        GameObject wallGroup = new GameObject($"Wall_{segment.Name}");
        wallGroup.transform.SetParent(_roadRoot);
        wallGroup.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(wallGroup, "가드레일 그룹 생성");

        float wallLength = GetPrefabLength(segment.Prefab_Wall);
        Vector3 up = Vector3.up * segment.WallHeightOffset;

        // 곡선에서는 안쪽 벽이 짧고 바깥쪽 벽이 길어야 이가 맞는다.
        // 호 길이 차이 = 중심선에서의 거리 × 회전각(라디안)
        float yawRadian = segment.YawPerTile * Mathf.Deg2Rad;
        float arcDelta = segment.WallOffset * yawRadian;

        int count = 0;

        for (int i = 0; i < segment.TileCount; i++)
        {
            Quaternion rotation = path.GetRotation(i);
            Vector3 right = rotation * Vector3.right;
            Vector3 center = path.GetPoint(i) + up;

            // 벽도 같은 이유로 이음새가 벌어진다. 두께 기준으로 덮어준다
            float seamExtend = 0f;
            if (segment.SeamOverlap > 0f)
            {
                float angle = Mathf.Clamp(GetJointAngle(segment, path, i), 0f, 60f);
                seamExtend = wallLength * Mathf.Tan(angle * Mathf.Deg2Rad) * segment.SeamOverlap * 0.5f;
            }

            float rightTargetLength = Mathf.Max(0.1f, tileLength - arcDelta + seamExtend);
            float leftTargetLength = Mathf.Max(0.1f, tileLength + arcDelta + seamExtend);

            count += SpawnWall(segment, wallGroup.transform,
                center + right * segment.WallOffset, rotation, rightTargetLength, wallLength);

            count += SpawnWall(segment, wallGroup.transform,
                center - right * segment.WallOffset, rotation, leftTargetLength, wallLength);
        }

        return count;
    }

    private int SpawnWall(RoadSegment segment, Transform parent, Vector3 position, Quaternion rotation, float targetLength, float wallLength)
    {
        GameObject wall = PrefabUtility.InstantiatePrefab(segment.Prefab_Wall) as GameObject;
        if (wall == null)
        {
            return 0;
        }

        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.rotation = rotation;

        if (segment.IsStretchWall && wallLength > 0.01f)
        {
            Vector3 scale = wall.transform.localScale;
            scale.z *= targetLength / wallLength;
            wall.transform.localScale = scale;
        }

        GameObjectUtility.SetStaticEditorFlags(wall, 0);
        Undo.RegisterCreatedObjectUndo(wall, "가드레일 생성");
        return 1;
    }

    private float GetPrefabLength(GameObject prefab)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return 0f;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.size.z;
    }

    // ─────────────────────────────────────────────
    // 오브젝트 배치
    // ─────────────────────────────────────────────

    private void GenerateObjects()
    {
        PrepareRoots();
        ClearChildren(_objectRoot);
        BuildAllPaths();

        _occupiedPositions.Clear();
        _occupiedRadii.Clear();

        int totalCount = 0;
        int requestedCount = 0;

        foreach (MapZoneSetting zone in _layout.Zones)
        {
            List<ZoneTargetEntry> ordered = GetOrderedEntries(zone);

            foreach (ZoneTargetEntry entry in ordered)
            {
                int placed = 0;

                for (int i = 0; i < entry.Count; i++)
                {
                    requestedCount++;
                    if (SpawnOne(zone, entry))
                    {
                        placed++;
                        totalCount++;
                    }
                }

                // 자리를 못 찾으면 조용히 사라지므로 반드시 알린다
                if (placed < entry.Count)
                {
                    Debug.LogWarning(
                        $"[맵 생성기] 배치 실패: {zone.ZoneName} / {entry.TargetId} — {entry.Count}개 중 {placed}개만 놓였습니다. " +
                        $"구역 범위나 간격(MinGapMultiplier)을 확인하세요");
                }
            }
        }

        Debug.Log($"[맵 생성기] 오브젝트 {totalCount}/{requestedCount}개 배치");
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

        GameObjectUtility.SetStaticEditorFlags(instance, 0);

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
        // 코스형 맵은 월드 Z로 구역을 가를 수 없다. 진행도 기준으로 배치한다
        if (zone.IsUseCourseRange)
        {
            return TryFindCoursePosition(zone, entry, out position);
        }

        if (entry.IsOnRoad)
        {
            return TryFindRoadPosition(zone, entry, out position);
        }
        return TryFindRoadsidePosition(zone, entry, out position);
    }

    // ─────────────────────────────────────────────
    // 코스 진행도 기준 배치
    // ─────────────────────────────────────────────

    public float GetCourseTotalLength()
    {
        float total = 0f;

        foreach (RoadSegment segment in _orderedSegments)
        {
            RoadPath path = GetPath(segment);
            if (path == null)
            {
                continue;
            }
            total += path.GetTotalLength();
        }

        return total;
    }

    private bool TryGetCoursePoint(float distance, out RoadSegment segment, out Vector3 center, out Quaternion rotation)
    {
        segment = null;
        center = Vector3.zero;
        rotation = Quaternion.identity;

        float travelled = 0f;

        foreach (RoadSegment current in _orderedSegments)
        {
            RoadPath path = GetPath(current);
            if (path == null)
            {
                continue;
            }

            float length = path.GetTotalLength();
            if (length < 0.01f)
            {
                continue;
            }

            if (distance <= travelled + length || segment == null)
            {
                float progress = path.GetProgressAtDistance(distance - travelled);
                segment = current;
                center = path.GetPointAt(progress);
                rotation = path.GetRotationAt(progress);

                if (distance <= travelled + length)
                {
                    return true;
                }
            }

            travelled += length;
        }

        return segment != null;
    }

    private bool TryFindCoursePosition(MapZoneSetting zone, ZoneTargetEntry entry, out Vector3 position)
    {
        position = Vector3.zero;

        float totalLength = GetCourseTotalLength();
        if (totalLength < 0.01f)
        {
            return false;
        }

        float myRadius = entry.VisualScale * _layout.MinGapMultiplier;

        float rangeStart = Mathf.Clamp01(Mathf.Min(zone.CourseStart, zone.CourseEnd));
        float rangeEnd = Mathf.Clamp01(Mathf.Max(zone.CourseStart, zone.CourseEnd));

        for (int attempt = 0; attempt < 60; attempt++)
        {
            float distance = Random.Range(rangeStart, rangeEnd) * totalLength;

            RoadSegment segment;
            Vector3 center;
            Quaternion rotation;
            if (TryGetCoursePoint(distance, out segment, out center, out rotation) == false)
            {
                continue;
            }

            Vector3 right = rotation * Vector3.right;
            float lateral;

            if (entry.IsOnRoad)
            {
                float spread = segment.WallOffset * entry.RoadSpreadRatio;
                lateral = Random.Range(-spread, spread);
            }
            else
            {
                // 가드레일 바깥. 코스가 공중에 떠 있으면 배경 오브젝트도 같이 뜬다
                float outside = segment.Width * 0.5f + GetTargetRoadDistance(entry.SizeValue);
                lateral = Random.value > 0.5f ? outside : -outside;
            }

            Vector3 candidate = center + right * lateral;
            candidate.y = center.y + entry.VisualScale * 0.5f;

            if (IsPositionFree(candidate, myRadius) == false)
            {
                continue;
            }

            position = candidate;
            return true;
        }

        return false;
    }

    // 가드레일 안쪽 노면 위. 달리면서 흡수할 대상용
    private bool TryFindRoadPosition(MapZoneSetting zone, ZoneTargetEntry entry, out Vector3 position)
    {
        float myRadius = entry.VisualScale * _layout.MinGapMultiplier;

        for (int attempt = 0; attempt < 60; attempt++)
        {
            RoadSegment segment = GetRandomSegment();
            if (segment == null)
            {
                break;
            }

            RoadPath path = GetPath(segment);
            if (path == null || path.TileCount <= 1)
            {
                continue;
            }

            float progress = Random.Range(0.5f, path.TileCount - 0.5f);
            Vector3 center = path.GetPointAt(progress);

            if (center.z < zone.MinZ || center.z > zone.MaxZ)
            {
                continue;
            }

            Quaternion rotation = path.GetRotationAt(progress);
            Vector3 right = rotation * Vector3.right;

            float spread = segment.WallOffset * entry.RoadSpreadRatio;
            float lateral = Random.Range(-spread, spread);

            Vector3 candidate = center + right * lateral;
            candidate.y = center.y + entry.VisualScale * 0.5f;

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

    // 도로 밖. 인도·갓길·건물 자리
    private bool TryFindRoadsidePosition(MapZoneSetting zone, ZoneTargetEntry entry, out Vector3 position)
    {
        float targetDistance = GetTargetRoadDistance(entry.SizeValue);
        float myRadius = entry.VisualScale * _layout.MinGapMultiplier;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            float x = Random.Range(_layout.MapMinX, _layout.MapMaxX);
            float z = Random.Range(zone.MinZ, zone.MaxZ);
            Vector3 candidate = new Vector3(x, 0f, z);

            if (IsNearIntersection(candidate))
            {
                continue;
            }

            RoadSegment nearestSegment;
            float centerDistance;
            float groundHeight;
            if (TryGetNearestRoad(candidate, out nearestSegment, out centerDistance, out groundHeight) == false)
            {
                continue;
            }

            float roadDistance = centerDistance - nearestSegment.Width * 0.5f;

            // 도로 위는 금지
            if (roadDistance < _layout.SidewalkDistance * 0.5f)
            {
                continue;
            }

            // 크기별 목표 거리에서 크게 벗어나면 재시도
            if (Mathf.Abs(roadDistance - targetDistance) > targetDistance * 0.8f)
            {
                continue;
            }

            // 경사를 반영한 지면 높이 위에 얹는다
            candidate.y = groundHeight + entry.VisualScale * 0.5f;

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

    private RoadSegment GetRandomSegment()
    {
        List<RoadSegment> usable = new List<RoadSegment>();

        foreach (RoadSegment segment in _layout.RoadSegments)
        {
            if (GetPath(segment) == null)
            {
                continue;
            }
            usable.Add(segment);
        }

        if (usable.Count == 0)
        {
            return null;
        }

        return usable[Random.Range(0, usable.Count)];
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

    // ─────────────────────────────────────────────
    // 도로 기준 계산 (전부 경로 폴리라인 기준)
    // ─────────────────────────────────────────────

    private bool TryGetNearestRoad(Vector3 position, out RoadSegment segment, out float distance, out float groundHeight)
    {
        segment = null;
        distance = float.MaxValue;
        groundHeight = 0f;

        foreach (RoadSegment current in _layout.RoadSegments)
        {
            RoadPath path = GetPath(current);
            if (path == null)
            {
                continue;
            }

            float currentDistance;
            float currentHeight;
            float currentProgress;
            if (path.TryGetClosest(position, out currentDistance, out currentHeight, out currentProgress) == false)
            {
                continue;
            }

            if (currentDistance >= distance)
            {
                continue;
            }

            distance = currentDistance;
            groundHeight = currentHeight;
            segment = current;
        }

        return segment != null;
    }

    private bool TryGetNearestRoadPoint(Vector3 position, out Vector3 closestPoint, out Quaternion rotation)
    {
        closestPoint = Vector3.zero;
        rotation = Quaternion.identity;

        float nearestDistance = float.MaxValue;
        bool isFound = false;

        foreach (RoadSegment current in _layout.RoadSegments)
        {
            RoadPath path = GetPath(current);
            if (path == null)
            {
                continue;
            }

            float currentDistance;
            float currentHeight;
            float currentProgress;
            if (path.TryGetClosest(position, out currentDistance, out currentHeight, out currentProgress) == false)
            {
                continue;
            }

            if (currentDistance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = currentDistance;
            closestPoint = path.GetPointAt(currentProgress);
            rotation = path.GetRotationAt(currentProgress);
            isFound = true;
        }

        return isFound;
    }

    private bool IsNearIntersection(Vector3 position)
    {
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

                Vector3 crossPoint = new Vector3(a.StartX, 0f, b.StartZ);
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
        Vector3 closestPoint;
        Quaternion roadRotation;
        if (TryGetNearestRoadPoint(position, out closestPoint, out roadRotation) == false)
        {
            return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        // 차량은 도로와 나란히
        if (entry.SizeValue >= 3 && entry.SizeValue <= 8)
        {
            Vector3 roadForward = roadRotation * Vector3.forward;
            roadForward.y = 0f;

            if (roadForward.sqrMagnitude < 0.0001f)
            {
                return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            float flip = Random.value > 0.5f ? 0f : 180f;
            return Quaternion.LookRotation(roadForward.normalized) * Quaternion.Euler(0f, flip, 0f);
        }

        // 건물은 도로를 향하도록
        if (entry.SizeValue > 8)
        {
            Vector3 toRoad = closestPoint - position;
            toRoad.y = 0f;

            if (toRoad.sqrMagnitude < 0.0001f)
            {
                return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            return Quaternion.LookRotation(toRoad.normalized);
        }

        // 작은 대상은 자유 회전
        return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
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

    // ─────────────────────────────────────────────
    // 루트 관리
    // ─────────────────────────────────────────────

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
