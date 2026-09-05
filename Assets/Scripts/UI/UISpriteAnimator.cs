using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// UI Image의 스프라이트를 일정 속도로 갈아 끼워 도트 애니메이션을 재생한다.
// Animator를 쓰지 않는 이유는 Aseprite가 만들어 주는 클립이 SpriteRenderer용이라
// Canvas 위의 Image에는 붙지 않기 때문이다.
public class UISpriteAnimator : MonoBehaviour
{
    [SerializeField] private Image Image_Target;
    [SerializeField] private float _framePerSecond = 8f;
    [SerializeField] private bool _isLoop = true;

    private List<Sprite> _frames;
    private float _elapsed;
    private int _index;

    public void Play(Image image, List<Sprite> frames, float framePerSecond)
    {
        Image_Target = image;
        _frames = frames;

        if (framePerSecond > 0f)
        {
            _framePerSecond = framePerSecond;
        }

        _elapsed = 0f;
        _index = 0;
        ApplyFrame();
    }

    public void Stop()
    {
        _frames = null;
    }

    private void Update()
    {
        if (_frames == null || _frames.Count <= 1 || Image_Target == null)
        {
            return;
        }

        _elapsed += Time.deltaTime;

        float frameTime = 1f / _framePerSecond;
        if (_elapsed < frameTime)
        {
            return;
        }
        _elapsed -= frameTime;

        _index++;
        if (_index >= _frames.Count)
        {
            if (_isLoop == false)
            {
                _index = _frames.Count - 1;
                _frames = null;
                return;
            }
            _index = 0;
        }

        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (Image_Target == null || _frames == null)
        {
            return;
        }
        if (_index < 0 || _index >= _frames.Count)
        {
            return;
        }
        Image_Target.sprite = _frames[_index];
    }
}
