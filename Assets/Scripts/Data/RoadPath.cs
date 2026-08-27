using System.Collections.Generic;
using UnityEngine;

// 도로 구간을 타일 단위로 미리 걸어둔 경로.
// 곡선이 들어가면 축 정렬(X/Z) 기준 계산이 성립하지 않아서, 거리·높이·방향 질의는 전부 이 폴리라인으로 한다.
public class RoadPath
{
    private List<Vector3> _points = new List<Vector3>();
    private List<Quaternion> _rotations = new List<Quaternion>();
    private List<float> _yaws = new List<float>();

    public int PointCount { get { return _points.Count; } }

    // 마지막 점은 끝 지점 표시용이라, 타일이 놓이는 개수는 이보다 하나 적다
    public int TileCount { get { return Mathf.Max(0, _points.Count - 1); } }

    // 경로 끝에서의 진행 방위(도). 다음 구간이 이어붙을 때 물려받는다.
    // 쿼터니언에서 뽑으면 각도가 0~360으로 감기고 피치와 섞이므로 따로 들고 있는다
    public float EndYaw { get; private set; }

    public void SetEndYaw(float yaw)
    {
        EndYaw = yaw;
    }

    public Vector3 GetEndPoint()
    {
        return GetPoint(_points.Count - 1);
    }

    public Quaternion GetEndRotation()
    {
        return GetRotation(_rotations.Count - 1);
    }

    public void AddPoint(Vector3 point, Quaternion rotation, float yaw)
    {
        _points.Add(point);
        _rotations.Add(rotation);
        _yaws.Add(yaw);
    }

    // 감기지 않은 원본 방위(도). 이음새 각도 차이를 재는 데 쓴다
    public float GetYaw(int index)
    {
        if (_yaws.Count == 0)
        {
            return 0f;
        }
        int clamped = Mathf.Clamp(index, 0, _yaws.Count - 1);
        return _yaws[clamped];
    }

    public Vector3 GetPoint(int index)
    {
        if (_points.Count == 0)
        {
            return Vector3.zero;
        }
        int clamped = Mathf.Clamp(index, 0, _points.Count - 1);
        return _points[clamped];
    }

    public Quaternion GetRotation(int index)
    {
        if (_rotations.Count == 0)
        {
            return Quaternion.identity;
        }
        int clamped = Mathf.Clamp(index, 0, _rotations.Count - 1);
        return _rotations[clamped];
    }

    // progress는 타일 인덱스 단위. 소수도 허용한다
    public Vector3 GetPointAt(float progress)
    {
        if (_points.Count == 0)
        {
            return Vector3.zero;
        }

        float clamped = Mathf.Clamp(progress, 0f, _points.Count - 1);
        int index = Mathf.FloorToInt(clamped);
        if (index >= _points.Count - 1)
        {
            return _points[_points.Count - 1];
        }

        float t = clamped - index;
        return Vector3.Lerp(_points[index], _points[index + 1], t);
    }

    public Quaternion GetRotationAt(float progress)
    {
        if (_rotations.Count == 0)
        {
            return Quaternion.identity;
        }

        float clamped = Mathf.Clamp(progress, 0f, _rotations.Count - 1);
        int index = Mathf.FloorToInt(clamped);
        if (index >= _rotations.Count - 1)
        {
            return _rotations[_rotations.Count - 1];
        }

        float t = clamped - index;
        return Quaternion.Slerp(_rotations[index], _rotations[index + 1], t);
    }

    // 수평 최단거리와, 그 지점의 노면 높이·진행도를 함께 돌려준다.
    // 경로 앞뒤로 벗어난 위치는 false (도로 범위 밖 판정)
    public bool TryGetClosest(Vector3 position, out float distance, out float height, out float progress)
    {
        distance = float.MaxValue;
        height = 0f;
        progress = 0f;

        if (_points.Count == 0)
        {
            return false;
        }

        if (_points.Count == 1)
        {
            Vector3 only = _points[0];
            Vector3 flat = position - only;
            flat.y = 0f;

            distance = flat.magnitude;
            height = only.y;
            return true;
        }

        bool isFound = false;

        for (int i = 0; i < _points.Count - 1; i++)
        {
            Vector3 a = _points[i];
            Vector3 b = _points[i + 1];

            Vector3 ab = b - a;
            ab.y = 0f;

            Vector3 ap = position - a;
            ap.y = 0f;

            float lengthSq = ab.sqrMagnitude;
            float t = 0f;
            if (lengthSq > 0.0001f)
            {
                t = Mathf.Clamp01(Vector3.Dot(ap, ab) / lengthSq);
            }

            Vector3 closest = a + ab * t;
            Vector3 diff = position - closest;
            diff.y = 0f;

            float currentDistance = diff.magnitude;
            if (currentDistance >= distance)
            {
                continue;
            }

            distance = currentDistance;
            height = Mathf.Lerp(a.y, b.y, t);
            progress = i + t;
            isFound = true;
        }

        if (isFound == false)
        {
            return false;
        }

        // 경로 양 끝을 벗어나 끝점에 붙은 경우는 도로 밖으로 본다
        float endProgress = _points.Count - 1;
        if (progress <= 0.001f && IsBeyondStart(position))
        {
            return false;
        }
        if (progress >= endProgress - 0.001f && IsBeyondEnd(position))
        {
            return false;
        }

        return true;
    }

    private bool IsBeyondStart(Vector3 position)
    {
        Vector3 forward = _rotations[0] * Vector3.forward;
        forward.y = 0f;

        Vector3 toPosition = position - _points[0];
        toPosition.y = 0f;

        return Vector3.Dot(toPosition, forward) < 0f;
    }

    private bool IsBeyondEnd(Vector3 position)
    {
        int last = _points.Count - 1;

        Vector3 forward = _rotations[last] * Vector3.forward;
        forward.y = 0f;

        Vector3 toPosition = position - _points[last];
        toPosition.y = 0f;

        return Vector3.Dot(toPosition, forward) > 0f;
    }

    // 시작점에서 distance(m)만큼 진행한 지점의 진행도(타일 인덱스 단위)
    public float GetProgressAtDistance(float distance)
    {
        if (_points.Count < 2 || distance <= 0f)
        {
            return 0f;
        }

        float travelled = 0f;

        for (int i = 0; i < _points.Count - 1; i++)
        {
            float stepLength = Vector3.Distance(_points[i], _points[i + 1]);
            if (stepLength < 0.0001f)
            {
                continue;
            }

            if (travelled + stepLength >= distance)
            {
                return i + (distance - travelled) / stepLength;
            }

            travelled += stepLength;
        }

        return _points.Count - 1;
    }

    public float GetTotalLength()
    {
        float total = 0f;
        for (int i = 0; i < _points.Count - 1; i++)
        {
            total += Vector3.Distance(_points[i], _points[i + 1]);
        }
        return total;
    }
}
