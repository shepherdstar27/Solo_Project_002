using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AbsorbableObject : MonoBehaviour
{
    [SerializeField] private float _absorbDuration = 0.35f;

    public int SizeValue { get; private set; }
    public int Score { get; private set; }

    public string PoolKey { get; private set; }

    private bool _isAbsorbed;

    public void Initialize(int sizeValue, int score, string poolKey)
    {
        SizeValue = sizeValue;
        Score = score;
        PoolKey = poolKey;
        _isAbsorbed = false;

        Collider bodyCollider = GetComponent<Collider>();
        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
        }
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

        Collider bodyCollider = GetComponent<Collider>();
        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        PlayAbsorbAsync(absorbPoint).Forget();
    }

    private async UniTask PlayAbsorbAsync(Transform absorbPoint)
    {
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < _absorbDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _absorbDuration);

            // 축소 + 나선(자전) + 흡수 지점으로 이동
            transform.position = Vector3.Lerp(startPosition, absorbPoint.position, t);
            transform.localScale = startScale * (1f - t);
            transform.Rotate(Vector3.up, 720f * Time.deltaTime, Space.World);

            await UniTask.Yield();
        }

        transform.localScale = startScale;   // 풀 반환 전 스케일 복원
        ObjectPoolManager.Instance.ReturnObject(PoolKey, gameObject);
    }
}