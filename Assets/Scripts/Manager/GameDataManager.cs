using System;
using System.Collections.Generic;
using UnityEngine;

public class GameDataManager : SingletonBase<GameDataManager>
{
    [Serializable]
    private class DataListWrapper<T>
    {
        public List<T> datas;
    }

    private Dictionary<Type, Dictionary<string, GameDataBase>> _dataTables
        = new Dictionary<Type, Dictionary<string, GameDataBase>>();

    public bool IsLoaded { get; private set; }

    public void LoadAllData()
    {
        _dataTables.Clear();

        LoadData<TierData>("TierData");
        LoadData<UnitData>("UnitData");
        LoadData<MonsterData>("MonsterData");
        LoadData<WaveData>("WaveData");
        LoadData<StageData>("StageData");
        LoadData<UpgradeData>("UpgradeData");
        LoadData<AbsorbTargetData>("AbsorbTargetData");
        LoadData<SpawnZoneData>("SpawnZoneData");

        IsLoaded = true;
        Debug.Log($"[GameDataManager] 전체 테이블 로드 완료: {_dataTables.Count}개");
    }

    private void LoadData<T>(string tableName) where T : GameDataBase
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>($"JsonOutput/{tableName}");
        if (jsonAsset == null)
        {
            Debug.LogError($"[GameDataManager] 테이블 파일 없음: JsonOutput/{tableName}");
            return;
        }

        string wrappedJson = "{\"datas\":" + jsonAsset.text + "}";
        DataListWrapper<T> wrapper = JsonUtility.FromJson<DataListWrapper<T>>(wrappedJson);

        if (wrapper == null || wrapper.datas == null)
        {
            Debug.LogError($"[GameDataManager] 파싱 실패: {tableName}");
            return;
        }

        Dictionary<string, GameDataBase> table = new Dictionary<string, GameDataBase>();
        foreach (T data in wrapper.datas)
        {
            if (string.IsNullOrEmpty(data.Id))
            {
                Debug.LogError($"[GameDataManager] Id 누락: {tableName}");
                continue;
            }
            if (table.ContainsKey(data.Id))
            {
                Debug.LogError($"[GameDataManager] Id 중복: {tableName} / {data.Id}");
                continue;
            }
            table.Add(data.Id, data);
        }

        _dataTables.Add(typeof(T), table);
        Debug.Log($"[GameDataManager] {tableName} 로드: {table.Count}건");
    }

    public T GetData<T>(string id) where T : GameDataBase
    {
        Dictionary<string, GameDataBase> table;
        if (_dataTables.TryGetValue(typeof(T), out table) == false)
        {
            Debug.LogError($"[GameDataManager] 테이블 없음: {typeof(T).Name}");
            return null;
        }

        GameDataBase data;
        if (table.TryGetValue(id, out data) == false)
        {
            Debug.LogError($"[GameDataManager] 데이터 없음: {typeof(T).Name} / Id {id}");
            return null;
        }

        return data as T;
    }

    public List<T> GetAllData<T>() where T : GameDataBase
    {
        List<T> result = new List<T>();

        Dictionary<string, GameDataBase> table;
        if (_dataTables.TryGetValue(typeof(T), out table) == false)
        {
            Debug.LogError($"[GameDataManager] 테이블 없음: {typeof(T).Name}");
            return result;
        }

        foreach (KeyValuePair<string, GameDataBase> pair in table)
        {
            result.Add(pair.Value as T);
        }
        return result;
    }
}