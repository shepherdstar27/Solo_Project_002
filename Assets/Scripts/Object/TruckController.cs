using UnityEngine;

public class TruckController : MonoBehaviour
{
    [SerializeField] private float _maxSpeed = 14f;
    [SerializeField] private float _maxReverseSpeed = -5f;
    [SerializeField] private float _accelRate = 9f;
    [SerializeField] private float _reverseAccelRate = 5f;
    [SerializeField] private float _naturalDecelRate = 4f;
    [SerializeField] private float _brakeRate = 12f;

    [SerializeField] private float _steerSpeedDegree = 90f;
    [SerializeField] private float _steerMinSpeedRatio = 0.15f;
    [SerializeField] private float _rotatePenaltyPerTier = 0.06f;
    [SerializeField] private float _bounceSpeedRatio = 0.3f;

    private TruckInput _input;
    private TruckStatus _status;
    private Rigidbody _rigidbody;

    private float _currentSpeed;
    private float _currentSteerSpeed;

    public float CurrentSpeed { get { return _currentSpeed; } }

    private void Awake()
    {
        _status = GetComponent<TruckStatus>();
        _input = GetComponent<TruckInput>();
        _rigidbody = GetComponent<Rigidbody>();
        _currentSteerSpeed = _steerSpeedDegree;
        _currentSpeed = 0f;
    }

    public void SetCamera(Transform cameraTransform)
    {
        _status.OnChangeTier += OnChangeTier;
    }

    private void OnChangeTier(int tierNumber)
    {
        float penalty = 1f - _rotatePenaltyPerTier * (tierNumber - 1);
        _currentSteerSpeed = _steerSpeedDegree * Mathf.Max(penalty, 0.6f);
    }

    private void Update()
    {
        UpdateSpeed();
        SteerTruck();
    }

    private void FixedUpdate()
    {
        MoveForward();
    }

    private void UpdateSpeed()
    {
        if (_input.IsBraking)
        {
            if (_currentSpeed > 0f)
            {
                _currentSpeed -= _brakeRate * Time.deltaTime;
            }
            else
            {
                _currentSpeed -= _reverseAccelRate * Time.deltaTime;
            }
        }
        else if (_input.IsAccelerating)
        {
            _currentSpeed += _accelRate * Time.deltaTime;
        }
        else
        {
            ApplyNaturalDecel();
        }

        _currentSpeed = Mathf.Clamp(_currentSpeed, _maxReverseSpeed, _maxSpeed);
    }

    private void ApplyNaturalDecel()
    {
        if (_currentSpeed > 0f)
        {
            _currentSpeed -= _naturalDecelRate * Time.deltaTime;
            if (_currentSpeed < 0f)
            {
                _currentSpeed = 0f;
            }
            return;
        }

        if (_currentSpeed < 0f)
        {
            _currentSpeed += _naturalDecelRate * Time.deltaTime;
            if (_currentSpeed > 0f)
            {
                _currentSpeed = 0f;
            }
        }
    }

    private void SteerTruck()
    {
        float steerInput = _input.MoveInput.x;
        if (Mathf.Abs(steerInput) < 0.01f)
        {
            return;
        }

        float speedRatio = Mathf.Abs(_currentSpeed) / _maxSpeed;
        if (speedRatio < _steerMinSpeedRatio)
        {
            return;
        }

        float steerFactor = Mathf.Clamp01(speedRatio);

        if (_currentSpeed < 0f)
        {
            steerInput = -steerInput;
        }

        float angle = steerInput * _currentSteerSpeed * steerFactor * Time.deltaTime;
        transform.Rotate(Vector3.up, angle, Space.World);
    }

    private void MoveForward()
    {
        Vector3 velocity = transform.forward * _currentSpeed;
        velocity.y = _rigidbody.linearVelocity.y;
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

        if (_status.IsAbsorbable(target) == false)
        {
            _currentSpeed *= _bounceSpeedRatio;
        }
    }
}