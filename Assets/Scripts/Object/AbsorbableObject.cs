using System;
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

    public void Initialize(int sizeValue, int score, string poolKey)
    {
        _sizeValue = sizeValue;
        _score = score;
        _poolKey = poolKey;
        _isAbsorbed = false;

        Collider bodyCollider = GetComponent<Collider>();
        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
        }
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

        Collider bodyCollider = GetComponent<Collider>();
        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        PlayAbsorbAsync(absorbPoint).Forget();
    }

    private async UniTask PlayAbsorbAsync(Transform absorbPoint)
    {
        float duration = Mathf.Max(_absorbDuration, 0.2f);

        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        int frameCount = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            frameCount++;
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