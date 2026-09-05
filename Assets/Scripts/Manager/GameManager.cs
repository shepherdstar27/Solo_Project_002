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
        // 제한 시간 초과로 이미 Fail이 된 뒤에 보스 격돌을 성공하면 클리어로 덮어쓴다.
        // 반대로 Clear를 Fail로 되돌리는 것은 막는다
        bool isOverwriteFail = isClear && State == GameState.Fail;

        if (State != GameState.Playing && isOverwriteFail == false)
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

        // StageLoader도 DontDestroyOnLoad라 중복 로드 방지 플래그가 남는다
        if (StageLoader.Instance != null)
        {
            StageLoader.Instance.ResetLoader();
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