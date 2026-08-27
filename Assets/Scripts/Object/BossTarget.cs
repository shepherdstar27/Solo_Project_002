using UnityEngine;

// 코스 끝에 놓이는 보스. 트럭이 닿는 순간 격돌 시퀀스를 시작한다.
// 밀어낸 뒤 디펜스 레인에 아군으로 소환될 때의 스탯도 여기서 들고 있다.
public class BossTarget : MonoBehaviour
{
    [SerializeField] private string _bossName = "이세계 마왕";

    [Header("아군으로 전향했을 때 스탯")]
    [SerializeField] private string _allyDataId = "Boss_Ally";
    [SerializeField] private float _allyHp = 2000f;
    [SerializeField] private int _allyAttack = 200;
    [SerializeField] private float _allyAttackInterval = 0.3f;
    [SerializeField] private float _allyRange = 0.15f;
    [SerializeField] private float _allyMoveSpeed = 1.2f;

    public string BossName { get { return _bossName; } }
    public string AllyDataId { get { return _allyDataId; } }
    public float AllyHp { get { return _allyHp; } }
    public int AllyAttack { get { return _allyAttack; } }
    public float AllyAttackInterval { get { return _allyAttackInterval; } }
    public float AllyRange { get { return _allyRange; } }
    public float AllyMoveSpeed { get { return _allyMoveSpeed; } }

    private bool _isTriggered;

    public void ResetBoss()
    {
        _isTriggered = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isTriggered)
        {
            return;
        }

        // 마법진이든 차체든 트럭에 속한 콜라이더면 모두 받는다
        TruckStatus status = other.GetComponentInParent<TruckStatus>();
        if (status == null)
        {
            return;
        }

        if (ClashManager.Instance == null)
        {
            Debug.LogError("[BossTarget] ClashManager를 찾을 수 없습니다");
            return;
        }

        _isTriggered = true;

        TruckController controller = status.GetComponent<TruckController>();
        ClashManager.Instance.BeginClash(this, status, controller);
    }
}
