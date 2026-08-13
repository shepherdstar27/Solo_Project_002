using UnityEngine;

public class TruckController : MonoBehaviour
{
    [SerializeField] private float _maxSpeed = 14f;
    [SerializeField] private float _minSpeed = 0f;          // 완전 정지 없음 (기획 원칙 유지)
    [SerializeField] private float _accelRate = 9f;         // 초당 가속량
    [SerializeField] private float _naturalDecelRate = 4f;  // 입력 없을 때 감속량
    [SerializeField] private float _brakeRate = 12f;        // S 브레이크 감속량
    [SerializeField] private float _rotateSpeedDegree = 200f;
    [SerializeField] private float _rotatePenaltyPerTier = 0.06f;
    [SerializeField] private float _bounceSpeedRatio = 0.3f;

    private TruckInput _input;
    private TruckStatus _status;
    private Transform _cameraTransform;
    private Rigidbody _rigidbody;

    private float _currentSpeed;
    private float _currentRotateSpeed;

    public float CurrentSpeed { get { return _currentSpeed; } }

    private void Awake()
    {
        _status = GetComponent<TruckStatus>();
        _input = GetComponent<TruckInput>();
        _rigidbody = GetComponent<Rigidbody>();
        _currentRotateSpeed = _rotateSpeedDegree;
        _currentSpeed = _minSpeed;
    }

    public void SetCamera(Transform cameraTransform)
    {
        _cameraTransform = cameraTransform;
        _status.OnChangeTier += OnChangeTier;
    }

    private void OnChangeTier(int tierNumber)
    {
        float penalty = 1f - _rotatePenaltyPerTier * (tierNumber - 1);
        _currentRotateSpeed = _rotateSpeedDegree * Mathf.Max(penalty, 0.6f);
    }

    private void Update()
    {
        UpdateSpeed();
        RotateTruck();
    }

    private void FixedUpdate()
    {
        MoveForward();
    }

    private void UpdateSpeed()
    {
        if (_input.IsBraking)
        {
            _currentSpeed -= _brakeRate * Time.deltaTime;
        }
        else if (_input.IsAccelerating)
        {
            _currentSpeed += _accelRate * Time.deltaTime;
        }
        else
        {
            _currentSpeed -= _naturalDecelRate * Time.deltaTime;
        }

        _currentSpeed = Mathf.Clamp(_currentSpeed, _minSpeed, _maxSpeed);
    }

    private void RotateTruck()
    {
        if (_cameraTransform == null)
        {
            return;
        }

        Vector2 input = _input.MoveInput;
        if (input.sqrMagnitude < 0.01f)
        {
            return;
        }

        // 카메라 기준 상대 방향으로 변환 (W = 화면 앞쪽)
        Vector3 cameraForward = _cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = _cameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 targetDirection = cameraForward * input.y + cameraRight * input.x;
        if (targetDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            _currentRotateSpeed * Time.deltaTime);
    }

    private void MoveForward()
    {
        Vector3 velocity = transform.forward * _currentSpeed;
        velocity.y = _rigidbody.linearVelocity.y;   // 중력은 물리에 맡김
        _rigidbody.linearVelocity = velocity;
    }

    private void OnDestroy()
    {
        if (_status != null)
        {
            _status.OnChangeTier -= OnChangeTier;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        AbsorbableObject target = collision.gameObject.GetComponent<AbsorbableObject>();
        if (target == null || target.IsAbsorbed())
        {
            return;
        }

        // 흡수 불가 대상에 부딪히면 속도 감소
        if (_status.IsAbsorbable(target) == false)
        {
            _currentSpeed *= _bounceSpeedRatio;
        }
    }
}