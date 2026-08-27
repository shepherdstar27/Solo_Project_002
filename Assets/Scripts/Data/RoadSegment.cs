using System;
using UnityEngine;

public enum RoadDirection
{
    Vertical,     // Z축 방향 (기준 방위 0도)
    Horizontal,   // X축 방향 (기준 방위 90도)
}

[Serializable]
public class RoadSegment
{
    public string Name = "Road";
    public RoadDirection Direction = RoadDirection.Vertical;
    public GameObject Prefab_Road;
    public float StartX;
    public float StartZ;
    public int TileCount = 5;
    public float Width = 30f;        // 배치 규칙 계산용 (프리팹 실제 폭에 맞춰 입력)

    [Header("경사")]
    public float StartY;             // 구간 시작 높이
    public float HeightDrop;         // 타일 1개마다 내려가는 높이. 0이면 평지

    [Header("곡선")]
    public float StartYaw;           // Direction 기준 방위에 더할 시작 각도
    public float YawPerTile;         // 타일 1개마다 꺾이는 각도. 양수는 우회전, 0이면 직선

    [Header("연결")]
    // 켜면 StartX / StartY / StartZ / Direction을 무시하고 앞 구간이 끝난 지점·방위에서 이어 붙인다.
    // 이때 StartYaw는 이어붙인 방위에 더해지는 꺾임 값으로 쓰인다
    public bool IsConnectToPrevious;

    [Header("메시 생성")]
    // 켜면 타일을 이어 붙이지 않고 경로 전체를 하나의 연속 메시로 만든다. 이음새가 생기지 않는다
    public bool IsUseMesh;
    public int MeshSubdivision = 4;        // 타일 하나를 몇 조각으로 쪼개 곡선을 매끄럽게 할지
    public Material Material_Road;
    public float RoadTextureLength = 20f;  // 이 길이(m)마다 도로 텍스처가 한 번 반복

    [Header("메시 가드레일")]
    public Material Material_Wall;
    public float WallHeight = 3f;
    public float WallThickness = 0.6f;
    public float WallTextureLength = 10f;  // 이 길이(m)마다 벽 텍스처가 한 번 반복

    [Header("이음새")]
    // 곡선에서 타일끼리 각도가 달라지면 이음면이 벌어진다.
    // 타일을 진행 방향으로 조금 늘려 그 틈을 덮는 정도. 1이면 딱 맞게, 0이면 보정 없음
    public float SeamOverlap = 1f;

    [Header("가드레일")]
    public GameObject Prefab_Wall;
    public bool IsBuildWall = true;
    public float WallOffset = 20f;       // 중심선에서 벽까지의 거리
    public float WallHeightOffset = 0f;  // 노면 기준 벽의 높이 보정
    public bool IsStretchWall = true;    // 벽 길이를 타일 길이에 맞춰 늘림

    public Vector3 GetStartPosition()
    {
        return new Vector3(StartX, StartY, StartZ);
    }

    public float GetBaseYaw()
    {
        float baseYaw = Direction == RoadDirection.Horizontal ? 90f : 0f;
        return baseYaw + StartYaw;
    }

    public float GetSlopeAngle(float tileLength)
    {
        if (tileLength <= 0.01f)
        {
            return 0f;
        }

        float ratio = Mathf.Clamp(HeightDrop / tileLength, -0.9f, 0.9f);
        return Mathf.Asin(ratio) * Mathf.Rad2Deg;
    }

    // 타일 하나씩 앞으로 걸어가며 경로를 만든다.
    // 진행 방향(회전 * forward)으로 타일 길이만큼 나아가므로 경사와 곡선이 동시에 반영되고,
    // 타일 사이가 벌어지거나 겹치지 않는다.
    public RoadPath BuildPath(float tileLength)
    {
        return BuildPath(tileLength, GetStartPosition(), GetBaseYaw());
    }

    // 시작 지점과 방위를 밖에서 지정하는 형태. 앞 구간에 이어 붙일 때 쓴다
    public RoadPath BuildPath(float tileLength, Vector3 startPosition, float startYaw)
    {
        return BuildPath(tileLength, 1, startPosition, startYaw);
    }

    // subdivision은 타일 하나를 몇 조각으로 쪼갤지. 총 길이와 총 회전각은 그대로 유지된다.
    // 메시로 만들 때 곡선을 매끄럽게 하려고 잘게 쪼갠다
    public RoadPath BuildPath(float tileLength, int subdivision, Vector3 startPosition, float startYaw)
    {
        RoadPath path = new RoadPath();
        path.SetEndYaw(startYaw);

        if (tileLength <= 0.01f || TileCount <= 0)
        {
            return path;
        }

        int stepPerTile = Mathf.Max(1, subdivision);
        float stepLength = tileLength / stepPerTile;
        float yawPerStep = YawPerTile / stepPerTile;
        int stepCount = TileCount * stepPerTile;

        float pitch = GetSlopeAngle(tileLength);
        float yaw = startYaw;
        Vector3 position = startPosition;

        // 조각 개수 + 1 (마지막은 코스 끝 지점)
        for (int i = 0; i <= stepCount; i++)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);
            path.AddPoint(position, rotation, yaw);

            position += rotation * Vector3.forward * stepLength;
            yaw += yawPerStep;
        }

        // 마지막 점에서의 방위. 다음 구간이 이 값을 물려받는다
        path.SetEndYaw(startYaw + YawPerTile * TileCount);
        return path;
    }

    public float GetTotalTurnAngle()
    {
        return YawPerTile * TileCount;
    }

    // 곡선 반경 (참고용). YawPerTile이 0이면 무한대라 0을 돌려준다
    public float GetTurnRadius(float tileLength)
    {
        float perTile = Mathf.Abs(YawPerTile);
        if (perTile < 0.001f || tileLength <= 0.01f)
        {
            return 0f;
        }

        return tileLength / (2f * Mathf.Sin(perTile * 0.5f * Mathf.Deg2Rad));
    }
}
