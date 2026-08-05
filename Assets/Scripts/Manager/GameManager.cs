using System;
using UnityEngine;

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