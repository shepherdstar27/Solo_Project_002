using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WaveSpawner
{
    private const float NoticeLeadTime = 5f;

    private List<WaveData> _waves = new List<WaveData>();
    private List<bool> _isSpawned = new List<bool>();
    private List<bool> _isNoticed = new List<bool>();

    private LaneSimulation _simulation;
    private SummonConverter _converter;

    public event Action OnNoticeWave;
    public event Action OnStartWave;

    public void Setup(StageData stage, LaneSimulation simulation, SummonConverter converter)
    {
        _simulation = simulation;
        _converter = converter;

        _waves.Clear();
        _isSpawned.Clear();
        _isNoticed.Clear();

        Debug.Log($"[WaveSpawner] Wave_List 원문: '{stage.Wave_List}'");

        List<string> waveIds = stage.GetWaveIds();
        Debug.Log($"[WaveSpawner] 파싱된 웨이브 ID {waveIds.Count}개");

        foreach (string waveId in waveIds)
        {
            WaveData wave = GameDataManager.Instance.GetData<WaveData>(waveId);
            if (wave == null)
            {
                Debug.LogError($"[WaveSpawner] WaveData 조회 실패: '{waveId}'");
                continue;
            }

            _waves.Add(wave);
            _isSpawned.Add(false);
            _isNoticed.Add(false);
        }

        Debug.Log($"[WaveSpawner] 최종 등록 웨이브 {_waves.Count}개");
    }

    public void UpdateSpawner(float sessionTime)
    {
        for (int i = 0; i < _waves.Count; i++)
        {
            WaveData wave = _waves[i];

            if (_isNoticed[i] == false && sessionTime >= wave.SpawnTime - NoticeLeadTime)
            {
                _isNoticed[i] = true;
                if (OnNoticeWave != null)
                {
                    OnNoticeWave.Invoke();
                }
            }

            if (_isSpawned[i] == false && sessionTime >= wave.SpawnTime)
            {
                _isSpawned[i] = true;
                SpawnWave(wave);
            }
        }
    }

    private void SpawnWave(WaveData wave)
    {
        SpawnWaveAsync(wave).Forget();

        if (OnStartWave != null)
        {
            OnStartWave.Invoke();
        }

        Debug.Log($"[WaveSpawner] 웨이브 시작: {wave.Id}");
    }

    private async UniTask SpawnWaveAsync(WaveData wave)
    {
        List<string> monsterIds = wave.GetMonsterIds();
        List<int> monsterCounts = wave.GetMonsterCounts();

        for (int i = 0; i < monsterIds.Count; i++)
        {
            int count = 1;
            if (i < monsterCounts.Count)
            {
                count = monsterCounts[i];
            }

            for (int n = 0; n < count; n++)
            {
                LaneEntity monster = _converter.CreateMonsterEntity(monsterIds[i]);
                if (monster == null)
                {
                    continue;
                }

                // 항상 최상단에서 스폰, 간격은 시간으로 벌린다
                _simulation.AddEntity(monster);

                if (wave.SpawnInterval > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(wave.SpawnInterval));
                }
            }
        }
    }

}