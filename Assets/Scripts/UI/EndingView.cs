using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 엔딩 컷 몇 장을 순서대로 보여준 뒤 채점표를 띄운다.
public class EndingView : UIBase
{
    [Header("엔딩 컷")]
    [SerializeField] private GameObject GameObject_CutRoot;
    [SerializeField] private Image Image_Cut;
    [SerializeField] private List<Sprite> Sprite_EndingCuts = new List<Sprite>();
    [SerializeField] private TextMeshProUGUI Text_CutCaption;
    [SerializeField] private List<string> _cutCaptions = new List<string>();
    [SerializeField] private float _cutDuration = 2.2f;
    [SerializeField] private float _cutFadeTime = 0.4f;

    [Header("채점표")]
    [SerializeField] private GameObject GameObject_ScoreRoot;
    [SerializeField] private TextMeshProUGUI Text_Grade;
    [SerializeField] private TextMeshProUGUI Text_TotalScore;
    [SerializeField] private TextMeshProUGUI Text_Detail;
    [SerializeField] private Button Button_Retry;

    private void Awake()
    {
        if (Button_Retry != null)
        {
            Button_Retry.onClick.AddListener(OnClickRetry);
        }
    }

    public void ShowEnding(ClashScore score)
    {
        ShowEndingAsync(score).Forget();
    }

    private async UniTask ShowEndingAsync(ClashScore score)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetActive(GameObject_ScoreRoot, false);
        SetActive(GameObject_CutRoot, true);

        await PlayCutsAsync();

        SetActive(GameObject_CutRoot, false);
        SetActive(GameObject_ScoreRoot, true);

        FillScoreBoard(score);
    }

    private async UniTask PlayCutsAsync()
    {
        if (Image_Cut == null || Sprite_EndingCuts.Count == 0)
        {
            // 컷 이미지가 아직 없으면 그냥 넘어간다
            return;
        }

        for (int i = 0; i < Sprite_EndingCuts.Count; i++)
        {
            Image_Cut.sprite = Sprite_EndingCuts[i];
            SetText(Text_CutCaption, i < _cutCaptions.Count ? _cutCaptions[i] : string.Empty);

            await FadeCutAsync(0f, 1f);
            await UniTask.Delay(System.TimeSpan.FromSeconds(_cutDuration), DelayType.UnscaledDeltaTime);
            await FadeCutAsync(1f, 0f);
        }
    }

    private async UniTask FadeCutAsync(float from, float to)
    {
        float elapsed = 0f;

        while (elapsed < _cutFadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _cutFadeTime);

            Color color = Image_Cut.color;
            color.a = Mathf.Lerp(from, to, t);
            Image_Cut.color = color;

            await UniTask.Yield();
        }
    }

    private void FillScoreBoard(ClashScore score)
    {
        if (score == null)
        {
            return;
        }

        SetText(Text_Grade, score.GetGrade());
        SetText(Text_TotalScore, $"{score.GetTotalScore()} P");

        string detail = string.Empty;
        detail += $"흡수 점수\t{score.AbsorbScore}\n";
        detail += $"흡수 횟수\t{score.AbsorbCount}\n";
        detail += $"최대 콤보\t{score.MaxCombo}  (+{score.GetComboBonus()})\n";
        detail += $"전송 유닛\t{score.TransferCount}  (+{score.GetTransferBonus()})\n";
        detail += $"충돌 속도\t{score.ImpactSpeedKph:F0} km/h  (+{score.GetImpactBonus()})\n";
        detail += $"최종 티어\t{score.TierNumber}  (+{score.GetTierBonus()})\n";
        detail += $"왕성 잔여\t{score.GateHpRatio * 100f:F0}%  (+{score.GetGateBonus()})\n";
        detail += $"도달 시간\t{score.ReachTime:F1}초 / {score.TimeLimit:F0}초  (+{score.GetTimeBonus()})\n";
        detail += $"격돌 연타\t{score.PressCount}회 / {score.ClashDuration:F1}초  (+{score.GetClashBonus()})";

        SetText(Text_Detail, detail);
    }

    private void OnClickRetry()
    {
        GameManager.Instance.RestartStage();
    }

    private void SetActive(GameObject target, bool isActive)
    {
        if (target == null)
        {
            return;
        }
        target.SetActive(isActive);
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target == null)
        {
            return;
        }
        target.text = value;
    }
}
