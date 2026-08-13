using System.Collections.Generic;
using UnityEngine;

public static class DataListParser
{
    public static List<string> ParseStringList(string source)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrEmpty(source))
        {
            return result;
        }

        string[] tokens = source.Split(',');
        foreach (string token in tokens)
        {
            string trimmed = token.Trim();
            if (string.IsNullOrEmpty(trimmed) == false)
            {
                result.Add(trimmed);
            }
        }

        return result;
    }

    public static List<int> ParseIntList(string source)
    {
        List<int> result = new List<int>();
        if (string.IsNullOrEmpty(source))
        {
            return result;
        }

        string[] tokens = source.Split(',');
        foreach (string token in tokens)
        {
            string trimmed = token.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            int value;
            if (int.TryParse(trimmed, out value) == false)
            {
                Debug.LogError($"[DataListParser] 정수 변환 실패: {trimmed}");
                continue;
            }
            result.Add(value);
        }

        return result;
    }

    public static List<float> ParseFloatList(string source)
    {
        List<float> result = new List<float>();
        if (string.IsNullOrEmpty(source))
        {
            return result;
        }

        string[] tokens = source.Split(',');
        foreach (string token in tokens)
        {
            string trimmed = token.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            float value;
            if (float.TryParse(trimmed, out value) == false)
            {
                Debug.LogError($"[DataListParser] 실수 변환 실패: {trimmed}");
                continue;
            }
            result.Add(value);
        }

        return result;
    }
}