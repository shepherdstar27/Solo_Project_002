using System;
using System.Collections.Generic;
using UnityEngine;

// 엔딩 컷 한 장(페이지)의 정의.
// 한 페이지가 여러 컷으로 나뉘면 Texture_Panels에 왼쪽부터 순서대로 넣는다.
// 컷이 한 장이면 화면 전체를 채우고, 여러 장이면 가로로 나눠 하나씩 나타난다.
[Serializable]
public class EndingCutPage
{
    public string Name = "컷";

    public List<Texture2D> Texture_Panels = new List<Texture2D>();

    [Tooltip("컷 하나가 완전히 나타나는 데 걸리는 시간")]
    public float FadeTime = 0.4f;

    [Tooltip("다음 컷이 나타나기까지의 간격. 1이면 1초 간격")]
    public float PanelInterval = 1f;

    [Tooltip("마지막 컷까지 나온 뒤 그대로 머무는 시간")]
    public float HoldTime = 1.2f;
}
