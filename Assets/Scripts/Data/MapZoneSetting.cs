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
}

[Serializable]
public class MapZoneSetting
{
    public string ZoneName = "Zone";
    public float MinZ;
    public float MaxZ;
    public List<ZoneTargetEntry> Targets = new List<ZoneTargetEntry>();
}