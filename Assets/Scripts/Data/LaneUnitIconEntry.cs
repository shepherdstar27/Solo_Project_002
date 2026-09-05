using System;
using System.Collections.Generic;
using UnityEngine;

// 레인 유닛 한 종류의 표시 정보.
// DataId는 JSON 테이블의 Id를 그대로 쓴다.
//   아군 : UnitData의 Id (Unit_01 ~ Unit_05)
//   적   : MonsterData의 Id (Monster_01 ~ Monster_04)
//   전향한 보스 : BossTarget의 AllyDataId (기본 Boss_Ally)
[Serializable]
public class LaneUnitIconEntry
{
    public string DataId;

    [Tooltip("정지 이미지. 아래 프레임만 채워도 첫 장이 대신 쓰인다")]
    public Sprite Sprite_Icon;

    [Tooltip("도트 애니메이션 프레임. 2장 이상이면 순서대로 반복 재생한다")]
    public List<Sprite> Sprite_Frames = new List<Sprite>();

    [Tooltip("초당 프레임 수")]
    public float FramePerSecond = 8f;

    [Tooltip("이미지에 곱할 색. 원본 그대로 쓰려면 흰색")]
    public Color Color_Tint = Color.white;

    [Tooltip("이 유닛만 크게/작게 표시하고 싶을 때")]
    public float Scale = 1f;

    // 소환 연출처럼 한 장만 필요한 곳에서 쓴다
    public Sprite GetStaticSprite()
    {
        if (Sprite_Icon != null)
        {
            return Sprite_Icon;
        }
        if (Sprite_Frames.Count > 0)
        {
            return Sprite_Frames[0];
        }
        return null;
    }

    public bool IsAnimated()
    {
        return Sprite_Frames.Count > 1;
    }

    // 인스펙터에서 새 항목을 추가하면 색이 (0,0,0,0)으로 들어온다.
    // 그대로 쓰면 이미지가 완전히 투명해져 아무것도 안 보이므로 흰색으로 되돌린다
    public Color GetTint()
    {
        if (Color_Tint.a <= 0f)
        {
            return Color.white;
        }
        return Color_Tint;
    }
}
