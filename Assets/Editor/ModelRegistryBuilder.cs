using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// procura os modelos dos packs em Assets/ pelo nome do arquivo e grava a lista em
// Resources/ForkliftHeroes/ModelRegistry.asset
public static class ModelRegistryBuilder
{
    private const string Dir = "Assets/Resources/ForkliftHeroes";
    private const string AssetPath = Dir + "/ModelRegistry.asset";
    private static readonly string[] Extensions = { ".fbx", ".obj", ".prefab", ".glb", ".gltf", ".blend", ".dae" };

    private class Candidate { public string path, norm, pack; public GameObject go; }

    [MenuItem("Forklift Heroes/Register Imported Models")]
    public static void BuildFromMenu() { Build(true); }

    [InitializeOnLoadMethod]
    private static void AutoRegister()
    {
        EditorApplication.delayCall += () =>
        {
            var reg = AssetDatabase.LoadAssetAtPath<ModelRegistry>(AssetPath);
            if (reg == null || reg.entries.Count == 0) Build(false);
        };
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) Build(false);
        };
    }

    public static string Normalize(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in s.ToLowerInvariant()) if (char.IsLetterOrDigit(c)) sb.Append(c);
        return sb.ToString();
    }

    public static bool IsModelPath(string p)
    {
        if (!p.StartsWith("Assets/") || p.StartsWith(Dir) || p.Contains("/Scripts/") || p.Contains("/Editor/")) return false;
        string ext = Path.GetExtension(p).ToLowerInvariant();
        foreach (var e in Extensions) if (ext == e) return true;
        return false;
    }

    // textura pela pasta do modelo (Assets/Art/Food, /Forklift, /Storage)
    private static string PackOf(string path)
    {
        if (path.Contains("/Food/")) return "food";
        if (path.Contains("/Forklift/")) return "forklift";
        if (path.Contains("/Storage/")) return "storage";
        if (path.Contains("/Market/")) return "market";
        return "";
    }

    public static void Build(bool showDialog)
    {
        var candidates = new List<Candidate>();
        foreach (string guid in AssetDatabase.FindAssets("t:GameObject", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!IsModelPath(path)) continue;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            candidates.Add(new Candidate { path = path, norm = Normalize(Path.GetFileNameWithoutExtension(path)), pack = PackOf(path), go = go });
        }

        var registry = AssetDatabase.LoadAssetAtPath<ModelRegistry>(AssetPath);
        if (registry == null)
        {
            if (candidates.Count == 0) return;  // nada pra registrar ainda
            Directory.CreateDirectory(Dir);
            registry = ScriptableObject.CreateInstance<ModelRegistry>();
            AssetDatabase.CreateAsset(registry, AssetPath);
        }
        registry.entries.Clear();

        var found = new StringBuilder();
        var missing = new StringBuilder();
        foreach (var kv in ModelCatalog.Aliases)
        {
            bool prefix = ModelCatalog.PrefixKeys.Contains(kv.Key);
            int cap = prefix ? 6 : int.MaxValue;
            int count = 0;
            var seen = new HashSet<string>();
            foreach (string alias in kv.Value)
            {
                foreach (var c in candidates)
                {
                    bool match = prefix ? c.norm.StartsWith(alias) : c.norm == alias;
                    if (!match || count >= cap || !seen.Add(c.path)) continue;
                    registry.entries.Add(new ModelRegistry.Entry { key = kv.Key, prefab = c.go, pack = c.pack });
                    count++;
                }
            }
            if (count > 0) found.AppendLine("  " + kv.Key + ": " + count);
            else missing.Append(kv.Key + " ");
        }

        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        ModelCatalog.ResetCache();
        EditorApplication.delayCall += MarketBaker.PromptIfNeeded;  // uma vez, depois de registrar

        string msg = "Registered models (" + registry.entries.Count + "):\n" + (found.Length > 0 ? found.ToString() : "  none\n")
                   + "\nNo model (uses the built-in shape): " + (missing.Length > 0 ? missing.ToString() : "-");
        Debug.Log(msg);
        if (showDialog) EditorUtility.DisplayDialog("Forklift Heroes", msg, "OK");
    }
}

// atualiza a lista ao importar um modelo
public class ModelRegistryPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        bool relevant = false;
        foreach (var p in imported) if (ModelRegistryBuilder.IsModelPath(p)) relevant = true;
        foreach (var p in deleted) if (ModelRegistryBuilder.IsModelPath(p)) relevant = true;
        foreach (var p in moved) if (ModelRegistryBuilder.IsModelPath(p)) relevant = true;
        if (relevant) EditorApplication.delayCall += () => ModelRegistryBuilder.Build(false);
    }
}
