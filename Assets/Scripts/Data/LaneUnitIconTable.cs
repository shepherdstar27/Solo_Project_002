using System.Collections.Generic;
using UnityEngine;

// 디펜스 구간에 등장하는 유닛(아군·적·전향한 보스)의 이미지를 한 곳에 모아 둔다.
// 레인 아이콘과 소환 연출이 같은 목록을 보므로 이미지를 두 번 등록할 필요가 없다.
[CreateAssetMenu(fileName = "LaneUnitIconTable", menuName = "TruckGame/Lane Unit Icon Table")]
public class LaneUnitIconTable : ScriptableObject
{
    [Header("유닛별 이미지")]
    public List<LaneUnitIconEntry> Entries = new List<LaneUnitIconEntry>();

    // 등록되지 않은 DataId면 null. 부르는 쪽에서 색으로 대체한다
    public LaneUnitIconEntry FindEntry(string dataId)
    {
        if (string.IsNullOrEmpty(dataId))
        {
            return null;
        }

        foreach (LaneUnitIconEntry entry in Entries)
        {
            if (entry.DataId == dataId)
            {
                return entry;
            }
        }
        return null;
    }
}
