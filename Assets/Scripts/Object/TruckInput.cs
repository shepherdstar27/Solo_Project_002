using UnityEngine;

public class TruckInput : MonoBehaviour
{
    [SerializeField] private float _mouseSensitivity = 3f;
    [SerializeField] private bool _isCursorLocked = true;

    public Vector2 MoveInput { get; private set; }      // x: 좌우(AD), y: 전후(WS)
    public float MouseDeltaX { get; private set; }
    public bool IsAccelerating { get; private set; }
    public bool IsBraking { get; private set; }

    private void Start()
    {
        if (_isCursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // 격돌 연출처럼 조작을 막아야 할 때 잠근다
    public bool IsLocked { get; private set; }

    public void SetLocked(bool isLocked)
    {
        IsLocked = isLocked;
    }

    private void Update()
    {
        if (IsLocked || (PauseManager.Instance != null && PauseManager.Instance.IsPaused))
        {
            MoveInput = Vector2.zero;
            MouseDeltaX = 0f;
            IsAccelerating = false;
            IsBraking = false;
            return;
        }

        UpdateKeyboard();
        UpdateMouse();
    }

    private void UpdateKeyboard()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A))
        {
            horizontal -= 1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            horizontal += 1f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            vertical += 1f;
        }

        MoveInput = new Vector2(horizontal, vertical);
        IsAccelerating = Input.GetKey(KeyCode.W);
        IsBraking = Input.GetKey(KeyCode.S);
    }

    private void UpdateMouse()
    {
        MouseDeltaX = Input.GetAxis("Mouse X") * _mouseSensitivity;
    }
}