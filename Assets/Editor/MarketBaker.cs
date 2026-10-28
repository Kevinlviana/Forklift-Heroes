using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// monta o supermercado inteiro na cena aberta:
// racks, piso, paredes, decoração, placas e etiquetas, dentro de um objeto Market
// também remove o ambiente original, as estantes cinza e a carga antiga
public static class MarketBaker
{
    private const string PromptKey = "ForkliftHeroes.BakePromptDone";

    [MenuItem("Forklift Heroes/Build Market In Scene")]
    public static void BuildFromMenu() { Build(true); }

    // pergunta uma vez, logo depois de registrar os modelos
    public static void PromptIfNeeded()
    {
        if (EditorPrefs.GetBool(PromptKey, false)) return;
        if (Application.isPlaying) return;
        if (Object.FindFirstObjectByType<GameManager>() == null) return;  // não é a cena do jogo
        if (Object.FindFirstObjectByType<MarketRoot>() != null) { EditorPrefs.SetBool(PromptKey, true); return; }
        if (!ModelCatalog.Has("shelfrack")) return;  // assets ainda não registrados
        EditorPrefs.SetBool(PromptKey, true);
        Build(true);
    }

    public static void Build(bool confirm)
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Forklift Heroes", "Exit Play mode first.", "OK");
            return;
        }
        if (Object.FindFirstObjectByType<GameManager>() == null)
        {
            EditorUtility.DisplayDialog("Forklift Heroes", "Open the game scene (Phase1_Treino) first.", "OK");
            return;
        }

        ModelCatalog.ResetCache();
        if (!ModelCatalog.Has("shelfrack"))
        {
            EditorUtility.DisplayDialog("Forklift Heroes",
                "The Storage Pack rack model was not found.\nMake sure Assets/Art is imported, then use 'Forklift Heroes > Register Imported Models' and try again.", "OK");
            return;
        }

        if (confirm && !EditorUtility.DisplayDialog("Forklift Heroes",
            "Build the supermarket INTO this scene?\n\n" +
            "- the market becomes visible in the editor (object 'Market')\n" +
            "- the original giant environment, the old gray shelves and the old cargo are removed\n\n" +
            "You can undo with Ctrl+Z, or close the scene without saving.", "Build market", "Not now"))
            return;

        // reconstruindo mercado já montado: mantém os dados
        LevelManager.BakeSeed seed = null;
        GameObject templateCopy = null;
        var old = Object.FindFirstObjectByType<MarketRoot>();
        if (old != null && old.cargoTemplate != null)
        {
            templateCopy = Object.Instantiate(old.cargoTemplate);
            templateCopy.name = "~TemplateCopy";
            templateCopy.SetActive(false);
            seed = new LevelManager.BakeSeed
            {
                stock = new Bounds(old.stockCenter, old.stockSize),
                cargoY = old.stockCenter.y,
                fruitsCenter = old.fruitsCenter,
                templateSource = templateCopy
            };
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        var env = GameObject.Find("Environment");
        if (env != null) Undo.DestroyObjectImmediate(env);

        var root = new GameObject("Market");
        Undo.RegisterCreatedObjectUndo(root, "Build Market");
        var data = root.AddComponent<MarketRoot>();

        var temp = new GameObject("~MarketBakeTemp");
        temp.AddComponent<MarketBuilder>();
        var lm = temp.AddComponent<LevelManager>();
        LevelManager.StaticResult res = null;
        try { res = lm.BuildStatic(root.transform, seed); }
        finally
        {
            Object.DestroyImmediate(temp);
            if (templateCopy != null) Object.DestroyImmediate(templateCopy);
        }

        if (res == null)
        {
            Object.DestroyImmediate(root);
            EditorUtility.DisplayDialog("Forklift Heroes", "Could not build the market (no cargo/shelves found in the scene).", "OK");
            return;
        }

        data.stockCenter = res.stock.center;
        data.stockSize = res.stock.size;
        data.regionCenter = res.region.center;
        data.regionSize = res.region.size;
        data.floorTop = res.floorTop;
        data.fruitsCenter = res.fruitsCenter;
        data.cargoTemplate = res.template;

        // fundo escuro
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.10f);
            EditorUtility.SetDirty(cam);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Mercado montado na cena (objeto Market). Salve a cena.");
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }
}
