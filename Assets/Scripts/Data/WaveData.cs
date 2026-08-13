using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WaveData : GameDataBase
{
    public float SpawnTime;          // 세션 시작 후 스폰 시각(초)
    public string Monster_List;      // "Monster_01,Monster_02"
    public string Count_List;        // "3,2" — Monster_List와 인덱스 1:1 대응
    public float SpawnInterval;

    public List<string> GetMonsterIds()
    {
        return DataListParser.ParseStringList(Monster_List);
    }

    public List<int> GetMonsterCounts()
    {
        List<string> ids = GetMonsterIds();
        List<int> counts = DataListParser.ParseIntList(Count_List);

        if (ids.Count != counts.Count)
        {
            Debug.LogError($"[WaveData] 리스트 개수 불일치 (Id: {Id}) — 몬스터 {ids.Count} / 수량 {counts.Count}");
        }

        return counts;
    }
}