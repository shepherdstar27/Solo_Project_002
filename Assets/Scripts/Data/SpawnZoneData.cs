using System;
using System.Collections.Generic;

[Serializable]
public class SpawnZoneData : GameDataBase
{
    public string ZoneName;
    public string ZoneType;
    public float MinX;
    public float MaxX;
    public float MinZ;
    public float MaxZ;
    public string Target_List;
    public string Count_List;
    public string PlacementType;

    public List<string> GetTargetIds()
    {
        return DataListParser.ParseStringList(Target_List);
    }

    public List<int> GetCounts()
    {
        return DataListParser.ParseIntList(Count_List);
    }
}