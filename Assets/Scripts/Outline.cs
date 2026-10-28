using UnityEngine;

public static class OutlineUtil
{
    private static Material mat;
    private static bool tried;

    private static Material GetMaterial()
    {
        if (!tried)
        {
            tried = true;
            Shader s = Resources.Load<Shader>("ForkliftHeroes/OutlineHull");
            if (s != null) mat = new Material(s);
        }
        return mat;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { mat = null; tried = false; }

    public static void Apply(Renderer r)
    {
        var m = GetMaterial();
        if (m == null || r == null || !(r is MeshRenderer)) return;

        var current = r.sharedMaterials;
        for (int i = 0; i < current.Length; i++) if (current[i] == m) return;

        var next = new Material[current.Length + 1];
        for (int i = 0; i < current.Length; i++) next[i] = current[i];
        next[current.Length] = m;
        r.sharedMaterials = next;
    }
}
