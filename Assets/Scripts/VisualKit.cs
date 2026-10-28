using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class VisualKit
{
    private static Shader shader;
    private static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();

    public static void Init(Shader s) { if (s != null) shader = s; }
    public static Shader LitShader => shader;

    public static void Kill(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }

    public static Material Mat(Color c)
    {
        int key = ((int)(c.r * 255f) << 24) | ((int)(c.g * 255f) << 16) | ((int)(c.b * 255f) << 8) | (int)(c.a * 255f);
        Material m;
        if (cache.TryGetValue(key, out m) && m != null) return m;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            m = PersistMaterial(key, c);
            cache[key] = m;
            return m;
        }
#endif
        m = new Material(shader);
        m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        cache[key] = m;
        return m;
    }

    public static GameObject Prim(PrimitiveType type, string objName, Vector3 pos, Vector3 scale, Color color, bool collider, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = objName;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = Mat(color);
        if (parent != null) go.transform.SetParent(parent, true);
        return go;
    }

#if UNITY_EDITOR
    public static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    private static Material PersistMaterial(int key, Color c)
    {
        EnsureFolder("Assets/Generated/Materials");
        string path = "Assets/Generated/Materials/Mat_" + key.ToString("X8") + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var m = new Material(shader);
        m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { cache.Clear(); shader = null; }
}
