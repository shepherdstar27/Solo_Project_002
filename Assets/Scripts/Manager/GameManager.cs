using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Ready,
    Playing,
    Clear,
    Fail,
    Result,
}

public class GameManager : SingletonBase<GameManager>
{
    public GameState State { get; private set; } = GameState.Ready;

    public event Action OnStartGame;
    public event Action<bool> OnEndGame;      // true = Clear, false = Fail
    public event Action<GameState> OnChangeState;

    public void StartGame()
    {
        if (State != GameState.Ready)
        {
            return;
        }

        ChangeState(GameState.Playing);
        if (OnStartGame != null)
        {
            OnStartGame.Invoke();
        }
    }

    public void EndGame(bool isClear)
    {
        if (State != GameState.Playing)
        {
            return;
        }

        ChangeState(isClear ? GameState.Clear : GameState.Fail);
        if (OnEndGame != null)
        {
            OnEndGame.Invoke(isClear);
        }
    }

    public void ShowResult()
    {
        if (State != GameState.Clear && State != GameState.Fail)
        {
            return;
        }
        ChangeState(GameState.Result);
    }

    public void ResetGame()
    {
        ChangeState(GameState.Ready);
    }

    // 씬을 다시 로드해 처음부터 시작한다.
    // 매니저들은 DontDestroyOnLoad라 살아남으므로 상태를 직접 되돌려 준다
    public void RestartStage()
    {
        Time.timeScale = 1f;

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.SetEnabled(false);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseUI(UIAddress.Ending);
            UIManager.Instance.CloseUI(UIAddress.Clash);
            UIManager.Instance.CloseUI(UIAddress.Option);
            UIManager.Instance.CloseUI(UIAddress.Pause);
        }

        if (ClashManager.Instance != null)
        {
            ClashManager.Instance.ResetClash();
        }

        ResetGame();

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    private void ChangeState(GameState next)
    {
        State = next;
        Debug.Log($"[GameManager] 상태 전환: {next}");
        if (OnChangeState != null)
        {
            OnChangeState.Invoke(next);
        }
    }
}