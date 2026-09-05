using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 승리 직후, 정산창이 뜨기 전에 엔딩 컷을 순서대로 보여 준다.
// 프리팹도 Addressables 등록도 필요 없도록 IMGUI로 그린다.
public class EndingCutView : MonoBehaviour
{
    [SerializeField] private List<EndingCutPage> _pages = new List<EndingCutPage>();

    [SerializeField] private float _pageFadeOutTime = 0.5f;    // 페이지가 사라지는 시간
    [SerializeField] private float _panelGap = 12f;            // 컷 사이 여백
    [SerializeField] private float _screenMarginRatio = 0.08f; // 화면 가장자리 여백 비율
    [SerializeField] private Color _colorBackground = new Color(0.02f, 0.02f, 0.04f, 1f);
    [SerializeField] private bool _isSkipEnabled = true;       // 클릭 / 엔터로 한 페이지씩 넘기기

    public bool IsPlaying { get; private set; }

    private int _pageIndex;
    private List<float> _panelAlphas = new List<float>();
    private float _pageAlpha;
    private bool _isSkipRequested;

    private GUIStyle _styleHint;
    private bool _isStyleReady;

    // 컷을 전부 보여줄 때까지 기다린다
    public async UniTask PlayAsync()
    {
        if (_pages.Count == 0)
        {
            Debug.LogWarning("[EndingCutView] 등록된 컷이 없어 건너뜁니다");
            return;
        }

        IsPlaying = true;

        for (int i = 0; i < _pages.Count; i++)
        {
            _pageIndex = i;
            await PlayPageAsync(_pages[i]);
        }

        IsPlaying = false;
        _pageIndex = 0;
        _panelAlphas.Clear();

        Debug.Log("[EndingCutView] 엔딩 컷 재생 완료");
    }

    private async UniTask PlayPageAsync(EndingCutPage page)
    {
        _pageAlpha = 1f;
        _isSkipRequested = false;

        _panelAlphas.Clear();
        for (int i = 0; i < page.Texture_Panels.Count; i++)
        {
            _panelAlphas.Add(0f);
        }

        // 컷을 왼쪽부터 하나씩 띄운다
        for (int i = 0; i < page.Texture_Panels.Count; i++)
        {
            await FadeInPanelAsync(i, page.FadeTime);

            float rest = page.PanelInterval - page.FadeTime;
            if (rest > 0f)
            {
                await WaitAsync(rest);
            }
        }

        await WaitAsync(page.HoldTime);
        await FadeOutPageAsync();
    }

    private async UniTask FadeInPanelAsync(int index, float fadeTime)
    {
        if (fadeTime <= 0f || _isSkipRequested)
        {
            _panelAlphas[index] = 1f;
            return;
        }

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            if (_isSkipRequested)
            {
                break;
            }
            elapsed += Time.unscaledDeltaTime;
            _panelAlphas[index] = Mathf.Clamp01(elapsed / fadeTime);
            await UniTask.Yield();
        }

        _panelAlphas[index] = 1f;
    }

    private async UniTask WaitAsync(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (_isSkipRequested)
            {
                return;
            }
            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield();
        }
    }

    private async UniTask FadeOutPageAsync()
    {
        if (_pageFadeOutTime <= 0f)
        {
            _pageAlpha = 0f;
            return;
        }

        float elapsed = 0f;
        while (elapsed < _pageFadeOutTime)
        {
            elapsed += Time.unscaledDeltaTime;
            _pageAlpha = 1f - Mathf.Clamp01(elapsed / _pageFadeOutTime);
            await UniTask.Yield();
        }

        _pageAlpha = 0f;
    }

    private void Update()
    {
        if (IsPlaying == false || _isSkipEnabled == false)
        {
            return;
        }

        // 격돌 연타가 SPACE라 컷까지 딸려 넘어가지 않도록 SPACE는 넣지 않는다
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            _isSkipRequested = true;
        }
    }

    private void OnGUI()
    {
        if (IsPlaying == false)
        {
            return;
        }
        if (_pageIndex < 0 || _pageIndex >= _pages.Count)
        {
            return;
        }

        PrepareStyle();

        EndingCutPage page = _pages[_pageIndex];

        float width = Screen.width;
        float height = Screen.height;

        Color background = _colorBackground;
        background.a *= _pageAlpha;
        DrawRect(new Rect(0f, 0f, width, height), background);

        int count = page.Texture_Panels.Count;
        if (count == 0)
        {
            return;
        }

        float margin = height * _screenMarginRatio;
        Rect area = new Rect(margin, margin, width - margin * 2f, height - margin * 2f);
        float cellWidth = (area.width - _panelGap * (count - 1)) / count;

        for (int i = 0; i < count; i++)
        {
            if (i >= _panelAlphas.Count)
            {
                break;
            }

            Texture2D texture = page.Texture_Panels[i];
            if (texture == null)
            {
                continue;
            }

            float alpha = _panelAlphas[i] * _pageAlpha;
            if (alpha <= 0f)
            {
                continue;
            }

            Rect cell = new Rect(area.x + (cellWidth + _panelGap) * i, area.y, cellWidth, area.height);
            DrawTexture(GetFitRect(cell, texture), texture, alpha);
        }

        if (_isSkipEnabled)
        {
            GUI.Label(new Rect(width - 260f, height - 44f, 240f, 30f), "클릭 / Enter — 건너뛰기", _styleHint);
        }
    }

    private void PrepareStyle()
    {
        if (_isStyleReady)
        {
            return;
        }
        _isStyleReady = true;

        _styleHint = new GUIStyle(GUI.skin.label);
        _styleHint.fontSize = 15;
        _styleHint.alignment = TextAnchor.MiddleRight;
        _styleHint.normal.textColor = new Color(0.8f, 0.8f, 0.85f, 0.7f);
    }

    // 컷 비율을 유지한 채 칸 안쪽에 맞춘다
    private Rect GetFitRect(Rect area, Texture texture)
    {
        if (texture.height <= 0 || area.height <= 0f)
        {
            return area;
        }

        float textureRatio = (float)texture.width / texture.height;
        float areaRatio = area.width / area.height;

        float width = area.width;
        float height = area.height;

        if (textureRatio > areaRatio)
        {
            height = width / textureRatio;
        }
        else
        {
            width = height * textureRatio;
        }

        return new Rect(area.center.x - width * 0.5f, area.center.y - height * 0.5f, width, height);
    }

    private void DrawTexture(Rect rect, Texture texture, float alpha)
    {
        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        GUI.DrawTexture(rect, texture);
        GUI.color = previous;
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
