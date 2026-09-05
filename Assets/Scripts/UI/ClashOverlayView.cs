using System.Collections.Generic;
using UnityEngine;

// UI 프리팹(UI_Clash / UI_Ending)이 아직 없을 때 화면에 격돌·엔딩 표시를 그린다.
// IMGUI라 프리팹도 Addressables 등록도 필요 없다.
// 이미지 슬롯이 비어 있으면 예전처럼 색 박스로 대신 그린다.
// 정식 UI가 준비되면 _isEnabled를 꺼두면 된다.
public class ClashOverlayView : MonoBehaviour
{
    [SerializeField] private bool _isEnabled = true;

    [Header("격돌 이미지 (비우면 색 박스로 대체)")]
    [SerializeField] private Texture2D Texture_Background;
    [SerializeField] private Texture2D Texture_Truck;
    [SerializeField] private Texture2D Texture_Boss;
    [SerializeField] private List<Texture2D> Texture_SparkFrames = new List<Texture2D>();
    [SerializeField] private Texture2D Texture_GaugeBack;
    [SerializeField] private Texture2D Texture_GaugeFill;

    [Header("격돌 배치")]
    [SerializeField] private float _cutTopRatio = 0.20f;       // 컷 시작 높이
    [SerializeField] private float _cutHeightRatio = 0.42f;    // 컷 높이
    [SerializeField] private float _cutWidthRatio = 0.30f;     // 컷 하나의 너비
    [SerializeField] private float _truckLeftRatio = 0.04f;    // 트럭 컷 왼쪽 위치
    [SerializeField] private float _bossLeftRatio = 0.66f;     // 보스 컷 왼쪽 위치
    [SerializeField] private float _pushDistanceRatio = 0.45f; // 밀려날 때 보스가 이동하는 거리
    [SerializeField] private float _sparkFramePerSecond = 14f; // 불꽃 프레임 속도

    [Header("불꽃 크기")]
    [SerializeField] private float _sparkWidthRatio = 1.0f;    // 컷 너비 대비 불꽃 영역 너비
    [SerializeField] private float _sparkHeightRatio = 1.3f;   // 컷 높이 대비 불꽃 영역 높이
    [SerializeField] private float _sparkScaleMin = 0.6f;      // 게이지가 비었을 때 크기
    [SerializeField] private float _sparkScaleMax = 1.3f;      // 게이지가 가득 찼을 때 크기
    [SerializeField] private float _sparkPulseAmount = 0.15f;  // 깜빡일 때 커졌다 작아지는 폭

    [SerializeField] private Color _colorPanel = new Color(0.05f, 0.05f, 0.08f, 0.88f);
    [SerializeField] private Color _colorGaugeBack = new Color(0.2f, 0.2f, 0.25f, 1f);
    [SerializeField] private Color _colorGaugeFill = new Color(1f, 0.72f, 0.2f, 1f);
    [SerializeField] private Color _colorSpark = new Color(1f, 0.5f, 0.1f, 1f);
    [SerializeField] private Color _colorTruckBox = new Color(0.3f, 0.55f, 0.9f, 0.9f);
    [SerializeField] private Color _colorBossBox = new Color(0.8f, 0.25f, 0.3f, 0.9f);

    private GUIStyle _styleTitle;
    private GUIStyle _styleBody;
    private GUIStyle _styleBig;
    private GUIStyle _styleButton;
    private bool _isStyleReady;

    private void OnGUI()
    {
        if (_isEnabled == false || ClashManager.Instance == null)
        {
            return;
        }

        PrepareStyles();

        ClashState state = ClashManager.Instance.State;

        if (state == ClashState.Clash || state == ClashState.Push)
        {
            DrawClash();
            return;
        }

        if (state == ClashState.March)
        {
            DrawMarch();
            return;
        }

        if (state == ClashState.Ending)
        {
            DrawEnding();
        }
    }

    private void PrepareStyles()
    {
        if (_isStyleReady)
        {
            return;
        }
        _isStyleReady = true;

        _styleTitle = new GUIStyle(GUI.skin.label);
        _styleTitle.fontSize = 30;
        _styleTitle.fontStyle = FontStyle.Bold;
        _styleTitle.normal.textColor = Color.white;

        _styleBody = new GUIStyle(GUI.skin.label);
        _styleBody.fontSize = 18;
        _styleBody.normal.textColor = new Color(0.88f, 0.88f, 0.92f);

        _styleBig = new GUIStyle(GUI.skin.label);
        _styleBig.fontSize = 64;
        _styleBig.fontStyle = FontStyle.Bold;
        _styleBig.alignment = TextAnchor.MiddleCenter;
        _styleBig.normal.textColor = new Color(1f, 0.82f, 0.3f);

        _styleButton = new GUIStyle(GUI.skin.button);
        _styleButton.fontSize = 22;
    }

    // ─────────────────────────────────────────────
    // 격돌
    // ─────────────────────────────────────────────

    private void DrawClash()
    {
        ClashManager clash = ClashManager.Instance;
        ClashScore score = clash.Score;

        float width = Screen.width;
        float height = Screen.height;

        // 배경
        if (Texture_Background != null)
        {
            DrawTexture(new Rect(0f, 0f, width, height), Texture_Background, 1f);
        }
        else
        {
            DrawRect(new Rect(0f, height * 0.15f, width, height * 0.70f), _colorPanel);
        }

        // 게이지를 다 채우면 보스가 오른쪽으로 밀려나며 사라진다
        float pushRatio = clash.State == ClashState.Push ? clash.PushRatio : 0f;
        float pushCurve = pushRatio * pushRatio;   // 처음엔 천천히, 뒤로 갈수록 빠르게

        float cutY = height * _cutTopRatio;
        float cutH = height * _cutHeightRatio;
        float cutW = width * _cutWidthRatio;

        float truckX = width * _truckLeftRatio + width * _pushDistanceRatio * 0.15f * pushCurve;
        float bossX = width * _bossLeftRatio + width * _pushDistanceRatio * pushCurve;
        float bossAlpha = 1f - pushRatio;

        DrawCut(new Rect(truckX, cutY, cutW, cutH), Texture_Truck, _colorTruckBox, 1f, "트럭");
        DrawCut(new Rect(bossX, cutY, cutW, cutH), Texture_Boss, _colorBossBox, bossAlpha, clash.BossName);

        // 두 컷 사이에서 불꽃이 튄다. 컷보다 커도 되므로 영역을 따로 잡는다
        float sparkAreaWidth = cutW * _sparkWidthRatio;
        float sparkAreaHeight = cutH * _sparkHeightRatio;
        float sparkCenterX = (truckX + cutW + bossX) * 0.5f;
        float sparkCenterY = cutY + cutH * 0.5f;

        Rect sparkArea = new Rect(
            sparkCenterX - sparkAreaWidth * 0.5f,
            sparkCenterY - sparkAreaHeight * 0.5f,
            sparkAreaWidth,
            sparkAreaHeight);

        DrawSpark(sparkArea, clash.GaugeRatio, 1f - pushRatio);

        DrawScoreLines(score, width, height);
        DrawGauge(clash, width, height);
    }

    // 이미지가 있으면 이미지를, 없으면 색 박스를 그린다
    private void DrawCut(Rect rect, Texture2D texture, Color boxColor, float alpha, string label)
    {
        if (alpha <= 0f)
        {
            return;
        }

        if (texture != null)
        {
            DrawTexture(GetFitRect(rect, texture), texture, alpha);
        }
        else
        {
            Color color = boxColor;
            color.a *= alpha;
            DrawRect(rect, color);
        }

        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        GUIStyle style = new GUIStyle(_styleTitle);
        style.alignment = TextAnchor.MiddleCenter;
        Color textColor = style.normal.textColor;
        textColor.a = alpha;
        style.normal.textColor = textColor;

        GUI.Label(new Rect(rect.x, rect.yMax + 6f, rect.width, 34f), label, style);
    }

    private void DrawSpark(Rect area, float gaugeRatio, float alpha)
    {
        if (alpha <= 0f)
        {
            return;
        }

        // 게이지가 찰수록 불꽃이 커지고 빠르게 깜빡인다.
        // 크기는 게이지가 정하고, 깜빡임은 그 위에서 _sparkPulseAmount만큼만 흔든다
        float pulse = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.Lerp(8f, 24f, gaugeRatio)));
        float scale = Mathf.Lerp(_sparkScaleMin, _sparkScaleMax, gaugeRatio)
            * Mathf.Lerp(1f - _sparkPulseAmount, 1f + _sparkPulseAmount, pulse);

        float sparkWidth = area.width * scale;
        float sparkHeight = area.height * scale;
        Rect rect = new Rect(
            area.center.x - sparkWidth * 0.5f,
            area.center.y - sparkHeight * 0.5f,
            sparkWidth,
            sparkHeight);

        Texture2D frame = GetSparkFrame();
        if (frame != null)
        {
            DrawTexture(GetFitRect(rect, frame), frame, alpha * (0.6f + 0.4f * pulse));
            return;
        }

        Color color = _colorSpark;
        color.a = (0.45f + 0.55f * pulse) * alpha;
        DrawRect(rect, color);
    }

    private Texture2D GetSparkFrame()
    {
        if (Texture_SparkFrames.Count == 0)
        {
            return null;
        }
        if (Texture_SparkFrames.Count == 1)
        {
            return Texture_SparkFrames[0];
        }

        int index = Mathf.FloorToInt(Time.unscaledTime * _sparkFramePerSecond) % Texture_SparkFrames.Count;
        return Texture_SparkFrames[index];
    }

    private void DrawScoreLines(ClashScore score, float width, float height)
    {
        if (score == null)
        {
            return;
        }

        float infoY = height * 0.66f;
        float lineHeight = 24f;

        GUI.Label(new Rect(width * 0.08f, infoY, 400f, lineHeight),
            $"충돌 속도   {score.ImpactSpeedKph:F0} km/h", _styleBody);
        GUI.Label(new Rect(width * 0.08f, infoY + lineHeight, 400f, lineHeight),
            $"티어   {score.TierNumber}", _styleBody);
        GUI.Label(new Rect(width * 0.08f, infoY + lineHeight * 2f, 400f, lineHeight),
            $"누적 점수   {score.AbsorbScore}", _styleBody);

        GUI.Label(new Rect(width * 0.45f, infoY, 400f, lineHeight),
            $"최대 콤보   {score.MaxCombo}", _styleBody);
        GUI.Label(new Rect(width * 0.45f, infoY + lineHeight, 400f, lineHeight),
            $"전송 유닛   {score.TransferCount}", _styleBody);
        GUI.Label(new Rect(width * 0.45f, infoY + lineHeight * 2f, 400f, lineHeight),
            $"왕성 잔여   {score.GateHpRatio * 100f:F0}%", _styleBody);
    }

    private void DrawGauge(ClashManager clash, float width, float height)
    {
        Rect back = new Rect(width * 0.20f, height * 0.78f, width * 0.60f, 34f);

        if (Texture_GaugeBack != null)
        {
            DrawTexture(back, Texture_GaugeBack, 1f);
        }
        else
        {
            DrawRect(back, _colorGaugeBack);
        }

        float ratio = Mathf.Clamp01(clash.GaugeRatio);
        Rect fill = new Rect(back.x, back.y, back.width * ratio, back.height);

        if (Texture_GaugeFill != null)
        {
            // 이미지를 늘리지 않고 채운 만큼만 잘라 그린다
            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(fill, Texture_GaugeFill, new Rect(0f, 0f, ratio, 1f));
            GUI.color = previous;
        }
        else
        {
            DrawRect(fill, _colorGaugeFill);
        }

        string prompt = clash.State == ClashState.Push
            ? "밀어냈다!"
            : $"SPACE 연타!   {clash.PressCount} / {clash.RequiredPressCount}";

        GUI.Label(new Rect(0f, height * 0.825f, width, 40f), prompt, CenteredTitle());
    }

    // ─────────────────────────────────────────────
    // 진격 / 엔딩
    // ─────────────────────────────────────────────

    private void DrawMarch()
    {
        float width = Screen.width;
        DrawRect(new Rect(0f, 40f, width, 70f), _colorPanel);
        GUI.Label(new Rect(0f, 50f, width, 50f), "전향한 보스가 적 본진으로 진격 중...", CenteredTitle());
    }

    private void DrawEnding()
    {
        ClashScore score = ClashManager.Instance.Score;
        if (score == null)
        {
            return;
        }

        float width = Screen.width;
        float height = Screen.height;

        Rect panel = new Rect(width * 0.22f, height * 0.10f, width * 0.56f, height * 0.80f);
        DrawRect(panel, _colorPanel);

        GUI.Label(new Rect(panel.x, panel.y + 20f, panel.width, 80f), score.GetGrade(), _styleBig);
        GUI.Label(new Rect(panel.x, panel.y + 100f, panel.width, 40f),
            $"{score.GetTotalScore()} P", CenteredTitle());

        float y = panel.y + 160f;
        float lineHeight = 26f;
        float labelX = panel.x + 40f;
        float labelWidth = panel.width - 80f;

        DrawScoreLine(labelX, ref y, labelWidth, lineHeight, "흡수 점수", $"{score.AbsorbScore}");
        DrawScoreLine(labelX, ref y, labelWidth, lineHeight, "흡수 횟수", $"{score.AbsorbCount}");
        DrawScoreLine(labelX, ref y, labelWidth, lineHeight, "최대 콤보", $"{score.MaxCombo}  (+{score.GetComboBonus()})");
        DrawScoreLine(labelX, ref y, labelWidth, lineHeight, "전송 유닛", $"{score.TransferCount}  (+{score.GetTransferBonus()})");
        DrawScoreLine(labelX, ref y, labelWidth, lineHeight, "충돌 속도", $"{score.ImpactSpeedKph:F0} km/h  (+{score.GetImpactBonus()})");
        DrawScoreLine(labelX, ref y, labelWidth, lineHeight, "최종 티어", $"{score.TierNumber}  (+{score.GetTierBonus()})");
        DrawScoreLine(labelX, ref y, labelWidth, lineHeight, "왕성 잔여", $"{score.GateHpRatio * 100f:F0}%  (+{score.GetGateBonus()})");
        DrawScoreLine(labelX, ref y, labelWidth, lineHeight, "도달 시간", $"{score.ReachTime:F1}초 / {score.TimeLimit:F0}초  (+{score.GetTimeBonus()})");
        DrawScoreLine(labelX, ref y, labelWidth, lineHeight, "격돌 연타", $"{score.PressCount}회 / {score.ClashDuration:F1}초  (+{score.GetClashBonus()})");

        Rect button = new Rect(panel.center.x - 90f, panel.yMax - 70f, 180f, 44f);
        if (GUI.Button(button, "다시하기", _styleButton))
        {
            GameManager.Instance.RestartStage();
        }
    }

    private void DrawScoreLine(float x, ref float y, float width, float lineHeight, string label, string value)
    {
        GUI.Label(new Rect(x, y, 160f, lineHeight), label, _styleBody);
        GUI.Label(new Rect(x + 170f, y, width - 170f, lineHeight), value, _styleBody);
        y += lineHeight;
    }

    private GUIStyle CenteredTitle()
    {
        GUIStyle style = new GUIStyle(_styleTitle);
        style.alignment = TextAnchor.MiddleCenter;
        return style;
    }

    // 이미지 비율을 유지한 채 칸 안쪽에 맞춘다
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
