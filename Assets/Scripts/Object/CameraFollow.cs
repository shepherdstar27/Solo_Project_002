using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 _baseOffset = new Vector3(0f, 14f, -11f);
    [SerializeField] private float _followSmoothTime = 0.25f;
    [SerializeField] private float _zoomPerTier = 0.4f;
    [SerializeField] private float _maxZoomMultiplier = 2.6f;
    [SerializeField] private float _zoomLerpSpeed = 1.5f;   // 1~2초 보간

    private Transform _target;
    private TruckStatus _status;
    private float _currentZoom = 1f;
    private float _targetZoom = 1f;
    private Vector3 _velocity;

    public void SetTarget(Transform target, TruckStatus status)
    {
        _target = target;
        _status = status;
        _status.OnChangeTier += OnChangeTier;

        transform.position = _target.position + _baseOffset;
        transform.LookAt(_target.position);
    }

    private void OnChangeTier(int tierNumber)
    {
        float zoom = 1f + _zoomPerTier * (tierNumber - 1);
        _targetZoom = Mathf.Min(zoom, _maxZoomMultiplier);
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, _zoomLerpSpeed * Time.deltaTime);

        Vector3 desiredPosition = _target.position + _baseOffset * _currentZoom;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, _followSmoothTime);
    }

    private void OnDestroy()
    {
        if (_status != null)
        {
            _status.OnChangeTier -= OnChangeTier;
        }
    }
}