using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float _baseDistance = 11f;
    [SerializeField] private float _baseHeight = 6f;
    [SerializeField] private float _lookAtHeightOffset = 2f;
    [SerializeField] private float _zoomLerpSpeed = 1.5f;

    private Transform _target;
    private TruckStatus _status;
    private TruckInput _input;

    private float _yaw;
    private float _currentZoom = 1f;
    private float _targetZoom = 1f;

    public void SetTarget(Transform target, TruckStatus status, TruckInput input)
    {
        _target = target;
        _status = status;
        _input = input;
        _status.OnChangeTier += OnChangeTier;

        _yaw = target.eulerAngles.y;
        transform.position = CalculateDesiredPosition();
    }

    private void OnChangeTier(int tierNumber)
    {
        _targetZoom = 1f;
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        if (_input != null)
        {
            _yaw += _input.MouseDeltaX;
        }

        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, _zoomLerpSpeed * Time.deltaTime);

        Vector3 desiredPosition = CalculateDesiredPosition();
        transform.position = desiredPosition;

        Vector3 lookAtPoint = _target.position + Vector3.up * _lookAtHeightOffset;
        transform.rotation = Quaternion.LookRotation(lookAtPoint - transform.position);
    }

    private Vector3 CalculateDesiredPosition()
    {
        Quaternion rotation = Quaternion.Euler(0f, _yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, _baseHeight, -_baseDistance);
        return _target.position + offset * _currentZoom;
    }

    private void OnDestroy()
    {
        if (_status != null)
        {
            _status.OnChangeTier -= OnChangeTier;
        }
    }
}