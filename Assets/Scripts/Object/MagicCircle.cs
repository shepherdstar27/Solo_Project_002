using UnityEngine;

public class MagicCircle : MonoBehaviour
{
    private TruckStatus _status;

    private void Awake()
    {
        _status = GetComponentInParent<TruckStatus>();
    }

    private void OnTriggerEnter(Collider other)
    {
        AbsorbableObject target = other.GetComponent<AbsorbableObject>();
        Debug.Log($"[MagicCircle] 접촉: {other.name} / AbsorbableObject {target != null}");
        if (target == null || target.IsAbsorbed())
        {
            return;
        }

        if (_status.IsAbsorbable(target) == false)
        {
            return;   // 크기 초과 → 흡수 불가 (트럭 본체 콜라이더에 막혀 자연스럽게 튕김)
        }

        _status.AbsorbTarget(target);
        target.PlayAbsorb(transform);
    }
}