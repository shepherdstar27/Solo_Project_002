using System;
using System.Collections.Generic;

[Serializable]
public class StageData : GameDataBase
{
    public float TimeLimit;
    public int GateHp;
    public string Wave_List;   // "Wave_01_01,Wave_01_02,Wave_01_03"

    public List<string> GetWaveIds()
    {
        return DataListParser.ParseStringList(Wave_List);
    }
}