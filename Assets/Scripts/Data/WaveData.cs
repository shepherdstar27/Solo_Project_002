using System;
using System.Collections.Generic;

[Serializable]
public class WaveData : GameDataBase
{
    public float SpawnTime;               // 세션 시작 후 스폰 시각(초)
    public List<string> MonsterIds;       // 스폰할 MonsterData Id 목록
    public List<int> MonsterCounts;       // MonsterIds와 인덱스 1:1 대응 수량
    public float SpawnInterval;           // 웨이브 내 개체 간 스폰 간격(초)
}