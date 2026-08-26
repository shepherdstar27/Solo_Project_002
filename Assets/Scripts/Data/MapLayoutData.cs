using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapLayout", menuName = "TruckGame/Map Layout")]
public class MapLayoutData : ScriptableObject
{
    [Header("맵 범위")]
    public float MapMinX = -150f;
    public float MapMaxX = 150f;
    public float MapMinZ = -320f;
    public float MapMaxZ = 560f;

    [Header("도로")]
    public List<RoadSegment> RoadSegments = new List<RoadSegment>();

    [Header("배치 규칙")]
    public float SidewalkDistance = 8f;        // 인도 (작은 대상)
    public float ParkingDistance = 16f;        // 갓길 주차 (승용차)
    public float BuildingMinDistance = 26f;    // 건물 최소 거리
    public float IntersectionClearRadius = 40f; // 교차로 여백
    public float MinGapMultiplier = 1.5f;

    [Header("구역")]
    public List<MapZoneSetting> Zones = new List<MapZoneSetting>();
}