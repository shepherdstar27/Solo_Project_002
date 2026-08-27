using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ZoneTargetEntry
{
    public GameObject Prefab_Target;
    public string TargetId = "Target_01";   // AbsorbTargetData의 Id
    public int SizeValue = 1;
    public int Score = 1;
    public float VisualScale = 3f;
    public int Count = 10;

    // true면 도로 밖(인도·건물 자리)이 아니라 가드레일 안쪽 노면 위에 배치한다.
    // 달리면서 흡수할 대상은 이걸 켜야 한다
    public bool IsOnRoad = false;

    // IsOnRoad일 때 중심선에서 좌우로 벌어지는 최대 비율 (가드레일 간격 기준)
    [Range(0f, 1f)]
    public float RoadSpreadRatio = 0.75f;
}

[Serializable]
public class MapZoneSetting
{
    public string ZoneName = "Zone";

    // 켜면 MinZ/MaxZ 대신 코스 진행도로 구역을 나눈다.
    // 코스가 휘어서 되돌아오면 월드 Z로는 구역을 가를 수 없다
    public bool IsUseCourseRange;

    [Range(0f, 1f)] public float CourseStart = 0f;
    [Range(0f, 1f)] public float CourseEnd = 1f;

    // IsUseCourseRange가 꺼져 있을 때만 쓰는 월드 Z 범위 (격자형 맵용)
    public float MinZ;
    public float MaxZ;

    public List<ZoneTargetEntry> Targets = new List<ZoneTargetEntry>();
}