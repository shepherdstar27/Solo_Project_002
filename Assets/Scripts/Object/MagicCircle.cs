using UnityEngine;

public class MagicCircle : MonoBehaviour
{
    [SerializeField] private TruckStatus TruckStatus_Owner;
    [SerializeField] private Transform Transform_AbsorbPoint;
    [SerializeField] private float _scalePerTier = 0.25f;

    private Vector3 _baseScale;

    private void Awake()
    {
        _baseScale = transform.localScale;

        if (TruckStatus_Owner == null)
        {
            Debug.LogError("[MagicCircle] TruckStatus_Owner가 연결되지 않았습니다");
            return;
        }

        TruckStatus_Owner.OnChangeTier += OnChangeTier;
    }

    private void OnDestroy()
    {
        if (TruckStatus_Owner != null)
        {
            TruckStatus_Owner.OnChangeTier -= OnChangeTier;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (TruckStatus_Owner == null)
        {
            return;
        }

        AbsorbableObject target = other.GetComponent<AbsorbableObject>();
        if (target == null || target.IsAbsorbed())
        {
            return;
        }

        if (TruckStatus_Owner.IsAbsorbable(target) == false)
        {
            return;
        }

        TruckStatus_Owner.AbsorbTarget(target);

        Transform absorbPoint = Transform_AbsorbPoint != null ? Transform_AbsorbPoint : transform;
        target.PlayAbsorb(absorbPoint);
    }

    private void OnChangeTier(int tierNumber)
    {
        float multiplier = 1f + _scalePerTier * (tierNumber - 1);
        transform.localScale = new Vector3(
            _baseScale.x * multiplier,
            _baseScale.y,
            _baseScale.z * multiplier);
    }
}