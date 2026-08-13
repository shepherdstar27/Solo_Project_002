using System;
using System.Collections.Generic;

[Serializable]
public class StageProgress
{
    public List<string> ClearedStageIds = new List<string>();
    public List<int> BestStars = new List<int>();

    public bool IsCleared(string stageId)
    {
        return ClearedStageIds.Contains(stageId);
    }

    public int GetBestStar(string stageId)
    {
        int index = ClearedStageIds.IndexOf(stageId);
        if (index < 0 || index >= BestStars.Count)
        {
            return 0;
        }
        return BestStars[index];
    }

    public void SetResult(string stageId, int star)
    {
        int index = ClearedStageIds.IndexOf(stageId);
        if (index < 0)
        {
            ClearedStageIds.Add(stageId);
            BestStars.Add(star);
            return;
        }

        if (star > BestStars[index])
        {
            BestStars[index] = star;
        }
    }
}