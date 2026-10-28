using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ModelRegistry : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;
        public GameObject prefab;
        public string pack; 
    }

    public List<Entry> entries = new List<Entry>();

    public List<Entry> All(string key)
    {
        var list = new List<Entry>();
        foreach (var e in entries) if (e.key == key && e.prefab != null) list.Add(e);
        return list;
    }
}

public static class ModelCatalog
{
    public static readonly Dictionary<string, string[]> Aliases = new Dictionary<string, string[]>
    {
        { "apple",      new[] { "applered", "applegreen", "apple" } },
        { "banana",     new[] { "banana", "bananagreen" } },
        { "orange",     new[] { "orangesimple", "orange" } },
        { "mango",      new[] { "mango" } },
        { "watermelon", new[] { "watermelon" } },
        { "milk",       new[] { "milk", "milk2", "milkwithtext", "milwithtext", "milk2withtext" } },
        { "yogurt",     new[] { "yaourt", "yogurt" } },
        { "eggs",       new[] { "eggpackage" } },
        { "can",        new[] { "sodared", "sodagreen", "sodablue", "tincan", "tincangradient", "tincanfish" } },
        { "juice",      new[] { "orangejuicegfx", "applejuicegfx", "grapejuicegfx", "orangejuice", "applejuice", "grapejuice" } },
        { "barrel",     new[] { "barrel" } },
        { "pallet",     new[] { "eurpallet", "pallet", "palett" } },
        { "crate",      new[] { "crate", "box" } },
        { "carton",     new[] { "carton" } },
        { "rack",       new[] { "storagerack", "rack" } },
        { "shelfrack",  new[] { "storagerack01" } },
        { "forklift",   new[] { "forklift" } },
        { "mktgondola", new[] { "smgondola" } },
        { "mktfridge",  new[] { "smfridge" } },
        { "mktpos",     new[] { "smpos" } },
        { "floortile",  new[] { "floor01simple" } },
        { "floorstripes", new[] { "floor02stripes" } },
    };

    public static readonly HashSet<string> PrefixKeys = new HashSet<string> { "barrel", "pallet", "crate", "carton", "rack", "forklift", "mktgondola", "mktfridge" };

    private static readonly string[] PaletteNames = { "Palette_Blue", "Palette_Yellow", "Palette_Purple", "Palette_Red", "Palette_Green" };

    private static ModelRegistry registry;
    private static bool loaded;
    private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    private static readonly Dictionary<Texture2D, Material> texMats = new Dictionary<Texture2D, Material>();
    private static readonly Dictionary<Material, Material> converted = new Dictionary<Material, Material>();
    private static readonly Dictionary<string, Material> flipMats = new Dictionary<string, Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { registry = null; loaded = false; textures.Clear(); texMats.Clear(); converted.Clear(); flipMats.Clear(); }

    public static void ResetCache() { registry = null; loaded = false; textures.Clear(); texMats.Clear(); converted.Clear(); flipMats.Clear(); }

    private static ModelRegistry Registry
    {
        get
        {
            if (!loaded) { loaded = true; registry = Resources.Load<ModelRegistry>("ForkliftHeroes/ModelRegistry"); }
            return registry;
        }
    }

    public static bool Has(string key) { return PickEntry(key, 0) != null; }

    private static ModelRegistry.Entry PickEntry(string key, int seed)
    {
        if (Registry == null || key == null) return null;
        var list = Registry.All(key);
        if (list.Count == 0) return null;
        return list[Mathf.Abs(seed) % list.Count];
    }

    private static Texture2D LoadTex(string resPath)
    {
        Texture2D t;
        if (textures.TryGetValue(resPath, out t)) return t;
        t = Resources.Load<Texture2D>(resPath);
        textures[resPath] = t;
        return t;
    }

    private static Texture2D LoadPalette(int index)
    {
        string name = PaletteNames[Mathf.Abs(index) % PaletteNames.Length];
        string cacheKey = "pal:" + name;
        Texture2D t;
        if (textures.TryGetValue(cacheKey, out t)) return t;

        var ta = Resources.Load<TextAsset>("ForkliftHeroes/Textures/Palettes/" + name);
        if (ta != null)
        {
            t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(ta.bytes);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            t.name = name;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                VisualKit.EnsureFolder("Assets/Generated/Textures");
                string p = "Assets/Generated/Textures/" + name + ".asset";
                var ex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                if (ex != null) t = ex; else AssetDatabase.CreateAsset(t, p);
            }
#endif
        }
        textures[cacheKey] = t;
        return t;
    }

    private static Texture2D TextureFor(ModelRegistry.Entry e, int seed)
    {
        switch (e.pack)
        {
            case "forklift": return LoadTex("ForkliftHeroes/Textures/Forklift");
            case "storage": return LoadPalette(seed);
            case "food": return LoadTex("ForkliftHeroes/Textures/Food/" + e.prefab.name);
            default: return null;
        }
    }

    private static Material MatFor(Texture2D tex)
    {
        Material m;
        if (texMats.TryGetValue(tex, out m) && m != null) return m;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            VisualKit.EnsureFolder("Assets/Generated/Materials");
            string path = "Assets/Generated/Materials/Tex_" + tex.name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) { texMats[tex] = existing; return existing; }
            Shader es = VisualKit.LitShader != null ? VisualKit.LitShader : Shader.Find("Universal Render Pipeline/Lit");
            var pm = new Material(es);
            pm.mainTexture = tex;
            pm.color = Color.white;
            if (pm.HasProperty("_Smoothness")) pm.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(pm, path);
            texMats[tex] = pm;
            return pm;
        }
#endif
        Shader s = VisualKit.LitShader != null ? VisualKit.LitShader : Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(s);
        m.mainTexture = tex;
        m.color = Color.white;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        texMats[tex] = m;
        return m;
    }

    public static GameObject Spawn(string key, int seed, Transform parent)
    {
        var e = PickEntry(key, seed);
        if (e == null) return null;

        var inst = UnityEngine.Object.Instantiate(e.prefab);
        foreach (var c in inst.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
        foreach (var rb in inst.GetComponentsInChildren<Rigidbody>()) UnityEngine.Object.DestroyImmediate(rb);
        inst.transform.SetParent(parent, false);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;

        Texture2D tex = TextureFor(e, seed);
        if (e.pack == "market") ApplyMarketMaterials(inst);
        else if (tex != null)
        {
            Material m = MatFor(tex);
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
        }
        else FixMaterials(inst);
        return inst;
    }
    private static void ApplyMarketMaterials(GameObject inst)
    {
        foreach (var r in inst.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                string n = mats[i] != null ? mats[i].name : "";
                if (n.StartsWith("SM_tex_"))
                {
                    var tex = LoadTex("ForkliftHeroes/Textures/Market/mkt_" + n.Substring(7));
                    if (tex != null) mats[i] = MatFor(tex);
                }
                else if (n.StartsWith("SM_col_") && n.Length >= 13)
                {
                    mats[i] = VisualKit.Mat(HexColor(n.Substring(7, 6)));
                }
                else if (mats[i] != null && !IsCompatible(mats[i])) mats[i] = Convert(mats[i]);
            }
            r.sharedMaterials = mats;
        }
    }

    private static Color HexColor(string hex)
    {
        int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color(r / 255f, g / 255f, b / 255f);
    }

    public static Material MarketMat(string name)
    {
        var tex = LoadTex("ForkliftHeroes/Textures/Market/mkt_" + name);
        if (tex == null) return null;
        Material m;
        if (flipMats.TryGetValue(name, out m) && m != null) return m;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            VisualKit.EnsureFolder("Assets/Generated/Materials");
            string path = "Assets/Generated/Materials/TexFlip_mkt_" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) { flipMats[name] = existing; return existing; }
            Shader es = VisualKit.LitShader != null ? VisualKit.LitShader : Shader.Find("Universal Render Pipeline/Lit");
            var pm = new Material(es);
            pm.mainTexture = tex;
            pm.mainTextureScale = new Vector2(1f, -1f);
            pm.mainTextureOffset = new Vector2(0f, 1f);
            if (pm.HasProperty("_Smoothness")) pm.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(pm, path);
            flipMats[name] = pm;
            return pm;
        }
#endif
        Shader s = VisualKit.LitShader != null ? VisualKit.LitShader : Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(s);
        m.mainTexture = tex;
        m.mainTextureScale = new Vector2(1f, -1f);
        m.mainTextureOffset = new Vector2(0f, 1f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        flipMats[name] = m;
        return m;
    }

    public static Vector3 MeasureSize(string key, int seed)
    {
        var e = PickEntry(key, seed);
        if (e == null) return Vector3.zero;
        var inst = UnityEngine.Object.Instantiate(e.prefab);
        inst.SetActive(false);
        Bounds b;
        bool ok = LocalBounds(inst.transform, inst.GetComponentsInChildren<Renderer>(true), out b);
        UnityEngine.Object.DestroyImmediate(inst);
        return ok ? b.size : Vector3.zero;
    }

    private static void FixMaterials(GameObject inst)
    {
        foreach (var r in inst.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null || IsCompatible(m)) continue;
                mats[i] = Convert(m);
                changed = true;
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    private static bool IsCompatible(Material m)
    {
        string n = m.shader != null ? m.shader.name : "";
        return n.StartsWith("Universal Render Pipeline") || n.StartsWith("Shader Graphs") || n.StartsWith("Hidden")
            || n.StartsWith("Sprites") || n.StartsWith("ForkliftHeroes");
    }

    private static Material Convert(Material src)
    {
        Material dst;
        if (converted.TryGetValue(src, out dst) && dst != null) return dst;

        Shader s = VisualKit.LitShader != null ? VisualKit.LitShader : Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) return src;

        dst = new Material(s);
        Color c = Color.white;
        if (src.HasProperty("_Color")) c = src.GetColor("_Color");
        else if (src.HasProperty("_BaseColor")) c = src.GetColor("_BaseColor");
        dst.color = c;
        if (src.mainTexture != null) dst.mainTexture = src.mainTexture;
        if (dst.HasProperty("_Smoothness")) dst.SetFloat("_Smoothness", 0.1f);
        converted[src] = dst;
        return dst;
    }

    public static bool LocalBounds(Transform space, IEnumerable<Renderer> renderers, out Bounds result)
    {
        result = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            Mesh mesh = null;
            var mf = r.GetComponent<MeshFilter>();
            if (mf != null) mesh = mf.sharedMesh;
            else
            {
                var smr = r as SkinnedMeshRenderer;
                if (smr != null) mesh = smr.sharedMesh;
            }
            if (mesh == null) continue;

            Bounds mb = mesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mb.center + new Vector3(
                    (i & 1) == 0 ? -mb.extents.x : mb.extents.x,
                    (i & 2) == 0 ? -mb.extents.y : mb.extents.y,
                    (i & 4) == 0 ? -mb.extents.z : mb.extents.z);
                Vector3 p = space.InverseTransformPoint(r.transform.TransformPoint(corner));
                if (first) { result = new Bounds(p, Vector3.zero); first = false; }
                else result.Encapsulate(p);
            }
        }
        return !first;
    }

    public static Bounds Ground(GameObject inst, Transform space)
    {
        Bounds b;
        var rs = inst.GetComponentsInChildren<Renderer>();
        if (!LocalBounds(space, rs, out b)) return new Bounds(Vector3.zero, Vector3.zero);
        inst.transform.localPosition += new Vector3(-b.center.x, -b.min.y, -b.center.z);
        LocalBounds(space, rs, out b);
        return b;
    }

    public static Bounds FitInto(GameObject inst, Transform space, float maxDim)
    {
        Bounds b;
        var rs = inst.GetComponentsInChildren<Renderer>();
        if (!LocalBounds(space, rs, out b)) return new Bounds(Vector3.zero, Vector3.zero);

        float cur = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (cur > 0.0001f) inst.transform.localScale = inst.transform.localScale * (maxDim / cur);
        return Ground(inst, space);
    }

    public static void FitToBox(GameObject inst, Transform space, Vector3 size)
    {
        Bounds b;
        var rs = inst.GetComponentsInChildren<Renderer>();
        if (!LocalBounds(space, rs, out b)) return;
        Vector3 s = inst.transform.localScale;
        inst.transform.localScale = new Vector3(
            b.size.x > 0.0001f ? s.x * size.x / b.size.x : s.x,
            b.size.y > 0.0001f ? s.y * size.y / b.size.y : s.y,
            b.size.z > 0.0001f ? s.z * size.z / b.size.z : s.z);
        LocalBounds(space, rs, out b);
        inst.transform.localPosition += -b.center;
    }
}
