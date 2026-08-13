using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageResultView : MonoBehaviour
{
    [SerializeField] private GameObject GameObject_ResultPanel;
    [SerializeField] private TextMeshProUGUI Text_ResultTitle;
    [SerializeField] private TextMeshProUGUI Text_Detail;
    [SerializeField] private Image[] Image_Stars;
    [SerializeField] private Button Button_Retry;
    [SerializeField] private Button Button_Next;

    [SerializeField] private Color _colorStarOn = Color.yellow;
    [SerializeField] private Color _colorStarOff = new Color(0.3f, 0.3f, 0.3f);

    public event Action OnClickRetry;
    public event Action OnClickNext;

    private void Awake()
    {
        GameObject_ResultPanel.SetActive(false);
        Button_Retry.onClick.AddListener(OnClickRetryButton);
        Button_Next.onClick.AddListener(OnClickNextButton);
    }

    public void ShowResult(bool isClear, int starCount, int transferCount, int earnedPoint)
    {
        GameObject_ResultPanel.SetActive(true);
        Text_ResultTitle.text = isClear ? "왕국을 지켰다!" : "왕성 함락...";
        Text_Detail.text = $"전송 {transferCount}건 / 획득 {earnedPoint}P";

        for (int i = 0; i < Image_Stars.Length; i++)
        {
            Image_Stars[i].color = i < starCount ? _colorStarOn : _colorStarOff;
        }

        Button_Next.gameObject.SetActive(isClear);
    }

    private void OnClickRetryButton()
    {
        if (OnClickRetry != null)
        {
            OnClickRetry.Invoke();
        }
    }

    private void OnClickNextButton()
    {
        if (OnClickNext != null)
        {
            OnClickNext.Invoke();
        }
    }
}