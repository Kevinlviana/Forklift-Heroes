using System.Collections.Generic;
using UnityEngine;

public static class PlacementLog
{
    private static readonly List<string> lines = new List<string>();
    public static IList<string> Lines => lines;

    public static void Add(string s)
    {
        lines.Add(s);
        if (lines.Count > 8) lines.RemoveAt(0);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { lines.Clear(); }
}
