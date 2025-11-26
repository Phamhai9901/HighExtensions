using System;
using System.Collections.Generic;
using System.Linq;
public static class RandomHelper
{
    static Random _rnd = new Random();

    static object randomLocker = new object();

    static public int Next(int min, int max)
    {
        lock (randomLocker)
        {
            return _rnd.Next(min, max);
        }
    }

    public static double NextDouble()
    {
        lock (randomLocker)
        {
            return _rnd.NextDouble();
        }
    }
    public static double NextDouble(double min, double max, Random random = null)
    {
        lock (randomLocker)
        {
            return (random == null ? _rnd : random).NextDouble() * (max - min) + min;
        }
    }
    public static double NextDouble(double min, double max)
    {
        lock (randomLocker)
        {
            return _rnd.NextDouble() * (max - min) + min;
        }
    }

    public static int Next(int max)
    {
        lock (randomLocker)
        {
            return _rnd.Next(max);
        }
    }

    public static float GetRandomFloat()
    {
        return (float)NextDouble();
    }

    public static int GetRandomIndexInList(List<float> chances)
    {
        float total = 0;
        foreach (float c in chances)
        {
            total += c;
        }
        float rg = GetRandomFloat() * total;
        int index = 0;
        float checkChance = 0;
        foreach (float c in chances)
        {
            checkChance += c;
            if (rg < checkChance) return index;
            index++;
        }
        return -1;
    }

    public static List<int> GetRandomIndexsInList(List<float> chances, int count)
    {
        List<int> indexs = new List<int>();
        List<int> allIndexs = new List<int>(Enumerable.Range(0, chances.Count));
        for (int i = 0; i < count; i++)
        {
            int rdIndex = GetRandomIndexInList(chances);
            if (rdIndex >= 0)
            {
                indexs.Add(allIndexs[rdIndex]);
                chances.RemoveAt(rdIndex);
                allIndexs.RemoveAt(rdIndex);
            }
        }
        return indexs;
    }

    public static bool CheckRandomChancePercent(float percent)
    {
        if (percent <= 0f) return false;
        var rand = NextDouble() * 100;
        return rand < percent;
    }

    public static string GetRandomKeyFromList(Dictionary<string, float> ratesList)
    {
        var keysList = ratesList.Keys.ToList();
        var chances = new List<float>();
        foreach (var key in ratesList.Keys)
        {
            chances.Add(ratesList[key]);
        }
        float total = 0;
        foreach (float c in chances)
        {
            total += c;
        }
        float rg = GetRandomFloat() * total;
        int index = 0;
        float checkChance = 0;
        foreach (float c in chances)
        {
            checkChance += c;
            if (rg < checkChance) return keysList[index];
            index++;
        }
        return "";
    }
}
