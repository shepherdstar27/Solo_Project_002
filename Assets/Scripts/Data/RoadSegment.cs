using System;
using UnityEngine;

public enum RoadDirection
{
    Vertical,     // Z축 방향
    Horizontal,   // X축 방향
}

[Serializable]
public class RoadSegment
{
    public string Name = "Road";
    public RoadDirection Direction = RoadDirection.Vertical;
    public GameObject Prefab_Road;
    public float StartX;
    public float StartZ;
    public int TileCount = 5;
    public float Width = 30f;        // 배치 규칙 계산용 (프리팹 실제 폭에 맞춰 입력)

    public Vector3 GetStartPosition()
    {
        return new Vector3(StartX, 0f, StartZ);
    }

    public Vector3 GetDirectionVector()
    {
        if (Direction == RoadDirection.Vertical)
        {
            return Vector3.forward;
        }
        return Vector3.right;
    }

    public float GetTotalLength(float tileLength)
    {
        return tileLength * TileCount;
    }

    public float GetCenterCoordinate()
    {
        return Direction == RoadDirection.Vertical ? StartX : StartZ;
    }

    public float GetDistanceFromCenterLine(Vector3 position, float tileLength)
    {
        float totalLength = GetTotalLength(tileLength);

        if (Direction == RoadDirection.Vertical)
        {
            if (position.z < StartZ || position.z > StartZ + totalLength)
            {
                return float.MaxValue;
            }
            return Mathf.Abs(position.x - StartX);
        }

        if (position.x < StartX || position.x > StartX + totalLength)
        {
            return float.MaxValue;
        }
        return Mathf.Abs(position.z - StartZ);
    }

    public Vector3 GetForwardDirection()
    {
        return GetDirectionVector();
    }
}