using UnityEngine;

// 마우스 커서 잠금/해제를 한 곳에서만 처리한다.
// 여러 스크립트가 각자 Cursor를 직접 만지면 어디서 잠기고 풀렸는지 추적이 안 되므로 이 창구로 모은다
public static class CursorController
{
    public static bool IsLocked { get; private set; }

    // 주행 중처럼 마우스로 시점을 돌려야 할 때
    public static void Lock()
    {
        IsLocked = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 결과창, 일시정지처럼 버튼을 눌러야 할 때
    public static void Unlock()
    {
        IsLocked = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
