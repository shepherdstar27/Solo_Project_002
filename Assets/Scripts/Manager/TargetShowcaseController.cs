using Cysharp.Threading.Tasks;
using UnityEngine;

public class TargetShowcaseController : SingletonBase<TargetShowcaseController>
{
    private TargetShowcase _showcase;

    public void SetShowcase(TargetShowcase showcase)
    {
        _showcase = showcase;
    }

    public void ShowTarget(string targetId)
    {
        if (_showcase == null)
        {
            return;
        }
        _showcase.ShowTargetAsync(targetId).Forget();
    }
}
