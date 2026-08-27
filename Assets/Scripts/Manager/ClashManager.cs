using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum ClashState
{
    None,
    Clash,     // 스페이스 연타 중
    Push,      // 보스를 밀어내는 연출 중
    March,     // 전향한 보스가 적 본진으로 진격 중
    Ending,
}

// 보스 격돌 시퀀스만 담당한다. 화면 표시는 ClashView / EndingView가 맡는다.
public class ClashManager : SingletonBase<ClashManager>
{
    [SerializeField] private int _basePressCount = 30;          // 위력 0일 때 필요한 연타 수
    [SerializeField] private int _minPressCount = 10;           // 위력이 최대여도 이만큼은 눌러야 한다
    [SerializeField] private float _impactReduceRatio = 0.6f;   // 위력이 연타 수를 깎는 비율
    [SerializeField] private float _gaugeDecayPerSecond = 0.12f;

    public ClashState State { get; private set; }
    public ClashScore Score { get; private set; }

    public event Action OnBeginClash;
    public event Action OnFinishClash;

    private BossTarget _boss;
    private ClashView _view;
    private TruckInput _truckInput;

    private float _gauge;
    private int _pressCount;
    private int _requiredPressCount = 30;
    private float _clashElapsed;
    private bool _isSubscribed;

    public void ResetClash()
    {
        State = ClashState.None;
        Score = null;
        _boss = null;
        _view = null;
        _gauge = 0f;
        _pressCount = 0;
        _clashElapsed = 0f;
    }

    public void BeginClash(BossTarget boss, TruckStatus status, TruckController controller)
    {
        if (State != ClashState.None)
        {
            return;
        }
        BeginClashAsync(boss, status, controller).Forget();
    }

    private async UniTask BeginClashAsync(BossTarget boss, TruckStatus status, TruckController controller)
    {
        _boss = boss;

        DefenseSessionManager session = DefenseSessionManager.Instance;
        if (session == null)
        {
            Debug.LogError("[ClashManager] DefenseSessionManager가 없습니다");
            return;
        }

        SubscribeSession(session);
        session.PauseSession();

        // 트럭을 그 자리에 세운다
        if (controller != null)
        {
            controller.StopForClash();
        }

        _truckInput = status.GetComponent<TruckInput>();
        if (_truckInput != null)
        {
            _truckInput.SetLocked(true);
        }

        Score = new ClashScore();
        Score.SetImpact(
            controller != null ? controller.CurrentSpeedKph : 0f,
            status.CurrentTierNumber,
            status.CurrentScore,
            status.AbsorbCount,
            status.Combo.MaxComboCount,
            session.GetTransferCount(),
            session.Gate != null ? session.Gate.GetHpRatio() : 0f,
            session.SessionTime,
            session.TimeLimit);

        // 빠르고 큰 상태로 들이받았으면 연타를 덜 해도 된다
        float reduce = Score.GetImpactPower() * _impactReduceRatio;
        _requiredPressCount = Mathf.Max(_minPressCount, Mathf.RoundToInt(_basePressCount * (1f - reduce)));

        _gauge = 0f;
        _pressCount = 0;
        _clashElapsed = 0f;

        if (OnBeginClash != null)
        {
            OnBeginClash.Invoke();
        }

        _view = await UIManager.Instance.OpenUIAsync<ClashView>(UIAddress.Clash);

        // 화면이 아직 없어도 시퀀스는 진행한다. UI 프리팹을 만들기 전에 흐름부터 확인할 수 있다
        if (_view == null)
        {
            Debug.LogWarning($"[ClashManager] ClashView가 없어 화면 없이 진행합니다. SPACE를 {_requiredPressCount}회 누르세요");
        }
        else
        {
            _view.ShowClash(boss.BossName, Score, _requiredPressCount);
        }

        State = ClashState.Clash;

        Debug.Log($"[ClashManager] 격돌 시작 / 속도 {Score.ImpactSpeedKph:F0}km/h / 티어 {Score.TierNumber} / 필요 연타 {_requiredPressCount}회");
    }

    private void Update()
    {
        if (State != ClashState.Clash)
        {
            return;
        }

        _clashElapsed += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _pressCount++;
            _gauge += 1f / _requiredPressCount;

            if (_view != null)
            {
                _view.PlayPressFeedback();
            }
        }

        _gauge -= _gaugeDecayPerSecond * Time.deltaTime;
        if (_gauge < 0f)
        {
            _gauge = 0f;
        }

        if (_view != null)
        {
            _view.SetGauge(Mathf.Clamp01(_gauge), _pressCount, _requiredPressCount);
        }

        if (_gauge >= 1f)
        {
            State = ClashState.Push;
            FinishClashAsync().Forget();
        }
    }

    private async UniTask FinishClashAsync()
    {
        Score.SetClash(_pressCount, _clashElapsed);
        Debug.Log($"[ClashManager] 격돌 성공 / 연타 {_pressCount}회 / {_clashElapsed:F1}초");

        if (_view != null)
        {
            await _view.PlayPushAsync();
            UIManager.Instance.CloseUI(_view);
        }

        if (_truckInput != null)
        {
            _truckInput.SetLocked(false);
        }

        State = ClashState.March;
        DefenseSessionManager.Instance.BeginMarch(_boss);

        if (OnFinishClash != null)
        {
            OnFinishClash.Invoke();
        }
    }

    private void SubscribeSession(DefenseSessionManager session)
    {
        if (_isSubscribed)
        {
            return;
        }
        _isSubscribed = true;
        session.OnReachEnemyBase += OnReachEnemyBase;
    }

    private void OnReachEnemyBase()
    {
        if (State != ClashState.March)
        {
            return;
        }

        State = ClashState.Ending;
        OpenEndingAsync().Forget();
    }

    private async UniTask OpenEndingAsync()
    {
        DefenseSessionManager.Instance.FinishByBoss();

        EndingView view = await UIManager.Instance.OpenUIAsync<EndingView>(UIAddress.Ending);

        // 엔딩 화면이 아직 없으면 채점표를 콘솔에 찍어 흐름만 확인할 수 있게 한다
        if (view == null)
        {
            Debug.LogWarning($"[ClashManager] EndingView가 없어 채점표를 콘솔에 출력합니다\n{GetScoreBoardText()}");
            return;
        }

        view.ShowEnding(Score);
    }

    private string GetScoreBoardText()
    {
        if (Score == null)
        {
            return "채점 기록 없음";
        }

        string text = string.Empty;
        text += $"===== 최종 등급 {Score.GetGrade()} / {Score.GetTotalScore()}P =====\n";
        text += $"흡수 점수   {Score.AbsorbScore}\n";
        text += $"흡수 횟수   {Score.AbsorbCount}\n";
        text += $"최대 콤보   {Score.MaxCombo}  (+{Score.GetComboBonus()})\n";
        text += $"전송 유닛   {Score.TransferCount}  (+{Score.GetTransferBonus()})\n";
        text += $"충돌 속도   {Score.ImpactSpeedKph:F0}km/h  (+{Score.GetImpactBonus()})\n";
        text += $"최종 티어   {Score.TierNumber}  (+{Score.GetTierBonus()})\n";
        text += $"왕성 잔여   {Score.GateHpRatio * 100f:F0}%  (+{Score.GetGateBonus()})\n";
        text += $"도달 시간   {Score.ReachTime:F1}초 / {Score.TimeLimit:F0}초  (+{Score.GetTimeBonus()})\n";
        text += $"격돌 연타   {Score.PressCount}회 / {Score.ClashDuration:F1}초  (+{Score.GetClashBonus()})";
        return text;
    }

    private void OnDestroy()
    {
        if (_isSubscribed == false)
        {
            return;
        }

        DefenseSessionManager session = DefenseSessionManager.Instance;
        if (session != null)
        {
            session.OnReachEnemyBase -= OnReachEnemyBase;
        }
    }
}
