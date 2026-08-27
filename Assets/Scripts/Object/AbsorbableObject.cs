using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AbsorbableObject : MonoBehaviour
{
    [SerializeField] private float _absorbDuration = 0.35f;
    [SerializeField] private int _sizeValue = 1;
    [SerializeField] private int _score = 1;
    [SerializeField] private string _poolKey;

    public int SizeValue { get { return _sizeValue; } }
    public int Score { get { return _score; } }
    public string PoolKey { get { return _poolKey; } }

    private bool _isAbsorbed;

    // 자식 모델 프리팹이 콜라이더를 들고 오는 경우가 있어 전부 모아둔다
    private List<Collider> _colliders = new List<Collider>();
    private Rigidbody _rigidbody;
    private bool _isCached;

    private void Awake()
    {
        CacheComponents();
    }

    private void CacheComponents()
    {
        if (_isCached)
        {
            return;
        }
        _isCached = true;

        _colliders.Clear();
        Collider[] found = GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in found)
        {
            _colliders.Add(collider);
        }

        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(int sizeValue, int score, string poolKey)
    {
        _sizeValue = sizeValue;
        _score = score;
        _poolKey = poolKey;
        _isAbsorbed = false;

        CacheComponents();
        SetCollisionEnabled(true);
    }

    // 에디터 배치용
    public void SetupForEditor(int sizeValue, int score, string poolKey)
    {
        _sizeValue = sizeValue;
        _score = score;
        _poolKey = poolKey;
    }

    public bool IsAbsorbed()
    {
        return _isAbsorbed;
    }

    public void PlayAbsorb(Transform absorbPoint)
    {
        if (_isAbsorbed)
        {
            return;
        }
        _isAbsorbed = true;

        CacheComponents();

        // 빨려들어가는 동안 트럭과 부딪혀 감속되지 않도록 물리에서 완전히 빠진다
        SetCollisionEnabled(false);

        PlayAbsorbAsync(absorbPoint).Forget();
    }

    private void SetCollisionEnabled(bool isEnabled)
    {
        foreach (Collider collider in _colliders)
        {
            if (collider == null)
            {
                continue;
            }
            collider.enabled = isEnabled;
        }

        // 콜라이더를 꺼도 리지드바디가 남아 있으면 접촉 계산이 한 프레임 더 살아있는 경우가 있다
        if (_rigidbody != null)
        {
            _rigidbody.detectCollisions = isEnabled;
        }
    }

    private async UniTask PlayAbsorbAsync(Transform absorbPoint)
    {
        float duration = Mathf.Max(_absorbDuration, 0.2f);

        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.position = Vector3.Lerp(startPosition, absorbPoint.position, t);
            transform.localScale = startScale * (1f - t);
            transform.Rotate(Vector3.up, 720f * Time.deltaTime, Space.World);

            await UniTask.Yield();
        }

        transform.localScale = startScale;

        if (string.IsNullOrEmpty(_poolKey))
        {
            Destroy(gameObject);
            return;
        }

        ObjectPoolManager.Instance.ReturnObject(_poolKey, gameObject);
    }
}
