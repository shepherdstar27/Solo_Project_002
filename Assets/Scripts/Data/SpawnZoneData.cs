using System;
using System.Collections.Generic;

[Serializable]
public class SpawnZoneData : GameDataBase
{
    public string ZoneName;        // 공원, 도로, 주택가
    public float CenterX;
    public float CenterZ;
    public float InnerRadius;
    public float OuterRadius;
    public string Target_List;     // "Target_01,Target_02"
    public string Count_List;      // "40,25"

    public List<string> GetTargetIds()
    {
        return DataListParser.ParseStringList(Target_List);
    }

    public List<int> GetCounts()
    {
        return DataListParser.ParseIntList(Count_List);
    }
}