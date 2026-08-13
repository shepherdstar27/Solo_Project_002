using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [SerializeField] private float _maxOffset = 0.9f;
    [SerializeField] private float _decaySpeed = 6f;

    private float _currentPower;

    public void AddShake(float power)
    {
        if (power > _currentPower)
        {
            _currentPower = Mathf.Min(power, 1f);
        }
    }

    private void LateUpdate()
    {
        if (_currentPower <= 0.001f)
        {
            return;
        }

        // CameraFollow가 LateUpdate에서 위치를 잡은 뒤 흔들림을 더한다
        float offsetX = Random.Range(-1f, 1f) * _maxOffset * _currentPower;
        float offsetY = Random.Range(-1f, 1f) * _maxOffset * _currentPower;

        transform.position += transform.right * offsetX + transform.up * offsetY;

        _currentPower -= _decaySpeed * Time.deltaTime * _currentPower;
        if (_currentPower < 0.001f)
        {
            _currentPower = 0f;
        }
    }
}