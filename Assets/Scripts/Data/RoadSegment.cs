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
    public float CenterX;
    public float CenterZ;
    public float Length = 200f;
    public float Width = 30f;

    public Vector3 GetCenter()
    {
        return new Vector3(CenterX, 0f, CenterZ);
    }

    public Vector3 GetSize()
    {
        if (Direction == RoadDirection.Vertical)
        {
            return new Vector3(Width, 1f, Length);
        }
        return new Vector3(Length, 1f, Width);
    }

    public float GetDistanceFromCenterLine(Vector3 position)
    {
        if (Direction == RoadDirection.Vertical)
        {
            // 세로 도로: 구간 범위 밖이면 매우 먼 값
            float halfLength = Length * 0.5f;
            if (position.z < CenterZ - halfLength || position.z > CenterZ + halfLength)
            {
                return float.MaxValue;
            }
            return Mathf.Abs(position.x - CenterX);
        }

        float halfLengthX = Length * 0.5f;
        if (position.x < CenterX - halfLengthX || position.x > CenterX + halfLengthX)
        {
            return float.MaxValue;
        }
        return Mathf.Abs(position.z - CenterZ);
    }

    public Vector3 GetForwardDirection()
    {
        if (Direction == RoadDirection.Vertical)
        {
            return Vector3.forward;
        }
        return Vector3.right;
    }
}