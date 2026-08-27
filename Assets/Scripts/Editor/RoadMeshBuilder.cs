using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// RoadPath를 따라 단면(프로파일)을 훑어 하나의 연속 메시를 만든다.
// 타일을 이어 붙이는 방식과 달리 이음새·겹침·단차가 원천적으로 생기지 않는다.
//
// UV 규칙
//   U : 단면을 따라간 거리 비율 (0~1). 도로면이면 좌 0 → 우 1
//   V : 시작점부터 누적한 실제 이동거리 ÷ textureLength
//       → 구간 길이와 상관없이 textureLength(m)마다 텍스처가 정확히 한 번 반복된다
public static class RoadMeshBuilder
{
    // 단면 좌표는 (가로 오프셋, 높이). 가로는 진행 방향 기준 오른쪽이 양수
    public static List<Vector2> CreateRoadProfile(float width)
    {
        float half = Mathf.Max(0.01f, width) * 0.5f;

        List<Vector2> profile = new List<Vector2>();
        profile.Add(new Vector2(-half, 0f));
        profile.Add(new Vector2(half, 0f));
        return profile;
    }

    // centerOffset이 양수면 오른쪽 벽, 음수면 왼쪽 벽.
    // 가로 좌표가 커지는 순서로 넣어야 양쪽 다 법선이 올바르게 나온다
    public static List<Vector2> CreateWallProfile(float centerOffset, float baseHeight, float height, float thickness)
    {
        float half = Mathf.Max(0.01f, thickness) * 0.5f;
        float top = baseHeight + Mathf.Max(0.01f, height);

        List<Vector2> profile = new List<Vector2>();
        profile.Add(new Vector2(centerOffset - half, baseHeight));
        profile.Add(new Vector2(centerOffset - half, top));
        profile.Add(new Vector2(centerOffset + half, top));
        profile.Add(new Vector2(centerOffset + half, baseHeight));
        return profile;
    }

    public static Mesh Build(RoadPath path, List<Vector2> profile, float textureLength, string meshName)
    {
        Mesh mesh = new Mesh();
        mesh.name = meshName;
        mesh.indexFormat = IndexFormat.UInt32;   // 긴 코스에서 65535개 정점 제한을 넘을 수 있다

        int ringCount = path.PointCount;
        int profileCount = profile.Count;

        if (ringCount < 2 || profileCount < 2)
        {
            return mesh;
        }

        // 단면 둘레를 재서 U 좌표를 만든다
        List<float> profileDistance = new List<float>();
        profileDistance.Add(0f);

        float perimeter = 0f;
        for (int j = 1; j < profileCount; j++)
        {
            perimeter += Vector2.Distance(profile[j - 1], profile[j]);
            profileDistance.Add(perimeter);
        }

        if (perimeter < 0.0001f)
        {
            perimeter = 1f;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        float arcLength = 0f;
        Vector3 previousPoint = path.GetPoint(0);

        for (int i = 0; i < ringCount; i++)
        {
            Vector3 center = path.GetPoint(i);
            Quaternion rotation = path.GetRotation(i);

            // 진행 방향 오른쪽. 피치가 걸려 있어도 수평을 유지한다
            Vector3 right = rotation * Vector3.right;

            arcLength += Vector3.Distance(previousPoint, center);
            previousPoint = center;

            float v = textureLength > 0.01f ? arcLength / textureLength : 0f;

            for (int j = 0; j < profileCount; j++)
            {
                // 높이는 항상 월드 기준 수직. 가드레일이 경사에서도 똑바로 선다
                Vector3 position = center + right * profile[j].x + Vector3.up * profile[j].y;

                vertices.Add(position);
                uvs.Add(new Vector2(profileDistance[j] / perimeter, v));
            }
        }

        List<int> triangles = new List<int>();

        for (int i = 0; i < ringCount - 1; i++)
        {
            int a = i * profileCount;
            int b = (i + 1) * profileCount;

            for (int j = 0; j < profileCount - 1; j++)
            {
                triangles.Add(a + j);
                triangles.Add(b + j);
                triangles.Add(b + j + 1);

                triangles.Add(a + j);
                triangles.Add(b + j + 1);
                triangles.Add(a + j + 1);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        return mesh;
    }
}
