using System.Collections.Generic;
using UnityEngine;

public class TruckController : MonoBehaviour
{
    [SerializeField] private List<TruckWheel> Wheels = new List<TruckWheel>();

    [Header("구동")]
    [SerializeField] private float _maxMotorTorque = 2200f;
    [SerializeField] private float _maxReverseTorque = 1200f;
    [SerializeField] private float _brakeTorque = 4000f;
    [SerializeField] private float _idleBrakeTorque = 300f;
    [SerializeField] private float _maxSpeedKph = 70f;

    [Header("조향")]
    [SerializeField] private float _maxSteerAngle = 28f;
    [SerializeField] private float _steerSpeed = 5f;
    [SerializeField] private float _steerSpeedReduction = 0.5f;

    [Header("안정화")]
    [SerializeField] private float _downForce = 120f;
    [SerializeField] private Vector3 _centerOfMassOffset = new Vector3(0f, -0.6f, 0f);

    [SerializeField] private float _bounceSpeedRatio = 0.3f;

    private TruckInput _input;
    private TruckStatus _status;
    private Rigidbody _rigidbody;

    private float _currentSteerAngle;

    public float CurrentSpeed { get { return _rigidbody != null ? _rigidbody.linearVelocity.magnitude : 0f; } }
    public float CurrentSpeedKph { get { return CurrentSpeed * 3.6f; } }

    private void Awake()
    {
        _status = GetComponent<TruckStatus>();
        _input = GetComponent<TruckInput>();
        _rigidbody = GetComponent<Rigidbody>();

        // 무게중심을 낮춰야 전복되지 않는다
        _rigidbody.centerOfMass += _centerOfMassOffset;
    }

    public void SetCamera(Transform cameraTransform)
    {
        // 차량 물리에서는 카메라 기준 이동을 쓰지 않음 (인터페이스 유지)
    }

    private void FixedUpdate()
    {
        UpdateSteer();
        UpdateMotor();
        ApplyDownForce();
        UpdateWheelMesh();
    }

    private void UpdateSteer()
    {
        // 속도가 빠를수록 조향각을 줄여 안정성 확보
        float speedRatio = Mathf.Clamp01(CurrentSpeedKph / _maxSpeedKph);
        float limitAngle = _maxSteerAngle * (1f - speedRatio * _steerSpeedReduction);

        float targetAngle = _input.MoveInput.x * limitAngle;
        _currentSteerAngle = Mathf.Lerp(_currentSteerAngle, targetAngle, _steerSpeed * Time.fixedDeltaTime);

        foreach (TruckWheel wheel in Wheels)
        {
            if (wheel.IsSteerWheel == false || wheel.WheelCollider_Wheel == null)
            {
                continue;
            }
            wheel.WheelCollider_Wheel.steerAngle = _currentSteerAngle;
        }
    }

    private void UpdateMotor()
    {
        float motorTorque = 0f;
        float brakeTorque = 0f;

        bool isMovingForward = IsMovingForward();

        if (_input.IsAccelerating)
        {
            if (CurrentSpeedKph < _maxSpeedKph)
            {
                motorTorque = _maxMotorTorque;
            }
        }
        else if (_input.IsBraking)
        {
            if (isMovingForward && CurrentSpeedKph > 3f)
            {
                brakeTorque = _brakeTorque;   // 전진 중이면 제동
            }
            else
            {
                motorTorque = -_maxReverseTorque;   // 정지 후에는 후진
            }
        }
        else
        {
            brakeTorque = _idleBrakeTorque;   // 엔진 브레이크
        }

        foreach (TruckWheel wheel in Wheels)
        {
            if (wheel.WheelCollider_Wheel == null)
            {
                continue;
            }

            if (wheel.IsDriveWheel)
            {
                wheel.WheelCollider_Wheel.motorTorque = motorTorque;
            }
            wheel.WheelCollider_Wheel.brakeTorque = brakeTorque;
        }
    }

    private bool IsMovingForward()
    {
        return Vector3.Dot(_rigidbody.linearVelocity, transform.forward) > 0f;
    }

    private void ApplyDownForce()
    {
        // 접지력 확보 (속도에 비례)
        _rigidbody.AddForce(-transform.up * (_downForce * CurrentSpeed));
    }

    private void UpdateWheelMesh()
    {
        foreach (TruckWheel wheel in Wheels)
        {
            wheel.UpdateMeshTransform();
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
            _rigidbody.linearVelocity *= _bounceSpeedRatio;
        }
    }
}