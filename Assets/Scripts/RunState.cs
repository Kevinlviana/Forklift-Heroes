using UnityEngine;

public static class RunState
{
    public const int MaxLevel = 5;

    public static int Level = 1;
    public static int BankedScore;  
    public static bool MenuShown;  

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Level = 1; BankedScore = 0; MenuShown = false;
    }

    public static void ResetCampaign()
    {
        Level = 1; BankedScore = 0;
    }

    public static float BestTime(int level) => PlayerPrefs.GetFloat("BestTime_L" + level, 0f);

    public static bool RegisterTime(int level, float timeUsed)
    {
        float best = BestTime(level);
        if (best <= 0f || timeUsed < best)
        {
            PlayerPrefs.SetFloat("BestTime_L" + level, timeUsed);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    public static int[] TopScores()
    {
        var result = new int[5];
        string csv = PlayerPrefs.GetString("TopScores", "");
        if (string.IsNullOrEmpty(csv)) return result;
        string[] parts = csv.Split(',');
        for (int i = 0; i < parts.Length && i < 5; i++)
        {
            int v;
            if (int.TryParse(parts[i], out v)) result[i] = v;
        }
        return result;
    }

    public static void AddTopScore(int score)
    {
        var list = new System.Collections.Generic.List<int>(TopScores());
        list.Add(score);
        list.Sort((a, b) => b.CompareTo(a));
        if (list.Count > 5) list.RemoveRange(5, list.Count - 5);
        PlayerPrefs.SetString("TopScores", string.Join(",", list));
        PlayerPrefs.Save();
    }

    public static string FormatTime(float t)
    {
        int s = Mathf.RoundToInt(t);
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }
}
