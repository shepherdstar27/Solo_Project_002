using System;

[Serializable]
public class AbsorbTargetData : GameDataBase
{
    public string Name;            // 행인, 자전거, 승용차...
    public int SizeValue;
    public int Score;
    public float VisualScale;
    public string PrefabKey;       // Addressables 주소
    public string TierId;          // 소환될 티어 (TierData 참조)
}