using UnityEngine;

public class TruckController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 8f;
    [SerializeField] private float _rotateSpeedDegree = 180f;   // 초당 회전 각도
    [SerializeField] private float _rotatePenaltyPerTier = 0.06f; // 티어당 회전 속도 감소율

    private FloatingJoystick _joystick;
    private TruckStatus _status;
    private float _currentRotateSpeed;

    private void Awake()
    {
        _status = GetComponent<TruckStatus>();
        _currentRotateSpeed = _rotateSpeedDegree;
    }

    public void SetJoystick(FloatingJoystick joystick)
    {
        _joystick = joystick;
        _status.OnChangeTier += OnChangeTier;
    }

    private void OnChangeTier(int tierNumber)
    {
        // 커질수록 회전 반경 미세 증가 (회전 속도 감소)
        float penalty = 1f - _rotatePenaltyPerTier * (tierNumber - 1);
        _currentRotateSpeed = _rotateSpeedDegree * Mathf.Max(penalty, 0.6f);
    }

    private void Update()
    {
        RotateTruck();
        MoveForward();
    }

    private void RotateTruck()
    {
        if (_joystick == null || _joystick.IsActive == false)
        {
            return;
        }

        Vector2 input = _joystick.Direction;
        if (input.sqrMagnitude < 0.01f)
        {
            return;
        }

        // 조이스틱 위 = 월드 +Z (카메라 회전 없음 전제)
        Vector3 targetDirection = new Vector3(input.x, 0f, input.y);
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            _currentRotateSpeed * Time.deltaTime);
    }

    private void MoveForward()
    {
        transform.position += transform.forward * (_moveSpeed * Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (_status != null)
        {
            _status.OnChangeTier -= OnChangeTier;
        }
    }
}