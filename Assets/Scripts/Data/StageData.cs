using System;
using System.Collections.Generic;

[Serializable]
public class StageData : GameDataBase
{
    public float TimeLimit;
    public int GateHp;
    public List<string> WaveIds;
}