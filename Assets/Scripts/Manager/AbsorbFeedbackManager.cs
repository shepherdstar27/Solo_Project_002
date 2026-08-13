using UnityEngine;

public class AbsorbFeedbackManager : SingletonBase<AbsorbFeedbackManager>
{
    [SerializeField] private AudioSource AudioSource_Absorb;
    [SerializeField] private AudioClip[] AudioClip_AbsorbTiers;   // 티어별 흡수음 (없어도 동작)
    [SerializeField] private AudioClip AudioClip_TierUp;

    [SerializeField] private float _shakeBase = 0.15f;
    [SerializeField] private float _shakeScale = 0.055f;
    [SerializeField] private float _pitchVariation = 0.12f;

    private CameraShaker _cameraShaker;

    public void SetCameraShaker(CameraShaker shaker)
    {
        _cameraShaker = shaker;
    }

    public void PlayAbsorbFeedback(int sizeValue, Vector3 worldPosition, int score)
    {
        // 크기에 비례한 화면 흔들림
        float power = _shakeBase + _shakeScale * sizeValue;
        if (_cameraShaker != null)
        {
            _cameraShaker.AddShake(power);
        }

        PlayAbsorbSound(sizeValue);

        if (ScorePopupSpawner.Instance != null)
        {
            ScorePopupSpawner.Instance.SpawnPopup(worldPosition, score);
        }
    }

    public void PlayTierUpFeedback()
    {
        if (_cameraShaker != null)
        {
            _cameraShaker.AddShake(0.85f);
        }

        if (AudioSource_Absorb != null && AudioClip_TierUp != null)
        {
            AudioSource_Absorb.pitch = 1f;
            AudioSource_Absorb.PlayOneShot(AudioClip_TierUp);
        }
    }

    private void PlayAbsorbSound(int sizeValue)
    {
        if (AudioSource_Absorb == null || AudioClip_AbsorbTiers == null || AudioClip_AbsorbTiers.Length == 0)
        {
            return;
        }

        // sizeValue가 클수록 낮고 묵직한 소리 (클립이 여러 개면 선택, 하나면 피치로 표현)
        int index = GetClipIndex(sizeValue);
        AudioClip clip = AudioClip_AbsorbTiers[index];
        if (clip == null)
        {
            return;
        }

        float pitch = 1.25f - Mathf.Min(sizeValue, 16) * 0.03f;
        AudioSource_Absorb.pitch = pitch + Random.Range(-_pitchVariation, _pitchVariation);
        AudioSource_Absorb.PlayOneShot(clip);
    }

    private int GetClipIndex(int sizeValue)
    {
        int index = 0;
        if (sizeValue >= 2)
        {
            index = 1;
        }
        if (sizeValue >= 8)
        {
            index = 2;
        }

        if (index >= AudioClip_AbsorbTiers.Length)
        {
            index = AudioClip_AbsorbTiers.Length - 1;
        }
        return index;
    }
}