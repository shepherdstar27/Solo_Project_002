using Cysharp.Threading.Tasks;
using UnityEngine;

public class PauseManager : SingletonBase<PauseManager>
{
    private bool _isPaused;
    private bool _isEnabled;

    public bool IsPaused { get { return _isPaused; } }

    public void SetEnabled(bool isEnabled)
    {
        _isEnabled = isEnabled;
    }

    private void Update()
    {
        if (_isEnabled == false)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) == false)
        {
            return;
        }

        if (_isPaused)
        {
            Resume();
        }
        else
        {
            PauseAsync().Forget();
        }
    }

    private async UniTask PauseAsync()
    {
        _isPaused = true;
        Time.timeScale = 0f;

        CursorController.Unlock();

        PauseUI ui = await UIManager.Instance.OpenUIAsync<PauseUI>(UIAddress.Pause);
        if (ui == null)
        {
            Debug.LogError("[PauseManager] PauseUI 로드 실패");
            return;
        }



    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        CursorController.Lock();

        UIManager.Instance.CloseUI(UIAddress.Option);
        UIManager.Instance.CloseUI(UIAddress.Pause);
    }
}