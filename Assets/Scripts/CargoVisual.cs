using TMPro;
using UnityEngine;

public enum ProductShape
{
    Auto, Generic,
    Apple, Orange, Banana, Pineapple, Watermelon, Mango,
    Milk, Cheese, Yogurt, Eggs,
    Can, Bottle, JuiceBox,
    Steak, Sausage, Ham,
    WineBottle, Champagne, Barrel
}

public class Billboard : MonoBehaviour
{
    public static void FaceNow(Transform t)
    {
        var cam = Camera.main;
        t.rotation = cam != null ? cam.transform.rotation : Quaternion.Euler(45f, 0f, 0f);
    }

    private void LateUpdate()
    {
        var cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;
    }
}

public class CargoVisual : MonoBehaviour
{
    public ProductShape shape = ProductShape.Auto;
    public bool useCrateColor;
    public Color crateColor = Color.white;
    public int cartonSeed;  

    private Transform emblemRoot;
    private Renderer[] crateRenderers;

    public static readonly Color Gold = new Color(0.95f, 0.75f, 0.1f);
    public static readonly Color HeavyGray = new Color(0.25f, 0.27f, 0.32f);

    public void Build()
    {
        var cargo = GetComponent<CargoItem>();
        if (crateRenderers == null) crateRenderers = GetComponentsInChildren<Renderer>();
        if (emblemRoot != null) Destroy(emblemRoot.gameObject);

        Color? tint = null;
        if (cargo != null)
        {
            if (cargo.isVip) tint = Gold;
            else if (cargo.cargoType == CargoType.Heavy) tint = HeavyGray;
            else if (useCrateColor) tint = crateColor;
        }

        bool cartonCrate = BuildCartonCrate(tint);
        if (!cartonCrate)
        {
            if (tint.HasValue)
                foreach (var r in crateRenderers) if (r != null && r is MeshRenderer) r.material.color = tint.Value;
            foreach (var r in crateRenderers) OutlineUtil.Apply(r);
        }

        var rootGo = new GameObject("Emblem");
        emblemRoot = rootGo.transform;
        emblemRoot.SetParent(transform, false);
        Vector3 s = transform.lossyScale;
        emblemRoot.localScale = new Vector3(1f / Mathf.Max(s.x, 0.01f), 1f / Mathf.Max(s.y, 0.01f), 1f / Mathf.Max(s.z, 0.01f));
        emblemRoot.localPosition = new Vector3(0f, 0.5f, 0f);

        float u = Mathf.Max(0.3f, Mathf.Sqrt(s.x * s.z));
        ProductShape resolved = ResolveShape();
        if (!TryBuildModel(resolved, u, cargo)) BuildShape(resolved, u);
        BuildTag(cargo, u);
    }

    private bool BuildCartonCrate(Color? tint)
    {
        if (!ModelCatalog.Has("carton")) return false;
        var inst = ModelCatalog.Spawn("carton", cartonSeed, transform);
        if (inst == null) return false;
        inst.name = "CartonModel";
        ModelCatalog.FitToBox(inst, transform, Vector3.one);

        foreach (var r in crateRenderers) if (r != null) r.enabled = false;

        if (tint.HasValue)
        {
            Vector3 s = transform.lossyScale;
            var label = VisualKit.Prim(PrimitiveType.Cube, "SectionLabel", transform.position, Vector3.one, tint.Value, false, transform);
            label.transform.localRotation = Quaternion.identity;
            label.transform.localPosition = new Vector3(0f, 0.5f + 0.012f / Mathf.Max(s.y, 0.01f), 0f);
            label.transform.localScale = new Vector3(0.7f, 0.025f / Mathf.Max(s.y, 0.01f), 0.7f);
        }
        return true;
    }

    private static string ModelKeyFor(ProductShape sh)
    {
        switch (sh)
        {
            case ProductShape.Apple: return "apple";
            case ProductShape.Banana: return "banana";
            case ProductShape.Orange: return "orange";
            case ProductShape.Mango: return "mango";
            case ProductShape.Eggs: return "eggs";
            case ProductShape.Watermelon: return "watermelon";
            case ProductShape.Milk: return "milk";
            case ProductShape.Yogurt: return "yogurt";
            case ProductShape.Can: return "can";
            case ProductShape.JuiceBox: return "juice";
            case ProductShape.Barrel: return "barrel";
            default: return null;
        }
    }

    private bool TryBuildModel(ProductShape sh, float u, CargoItem cargo)
    {
        string key = ModelKeyFor(sh);
        if (key == null) return false;

        string seedSource = cargo != null && !string.IsNullOrEmpty(cargo.targetSlotId) ? cargo.targetSlotId : name;
        var inst = ModelCatalog.Spawn(key, seedSource.GetHashCode(), emblemRoot);
        if (inst == null) return false;
        ModelCatalog.FitInto(inst, emblemRoot, 0.6f * u);
        return true;
    }

    private ProductShape ResolveShape()
    {
        if (shape != ProductShape.Auto) return shape;
        string n = name.ToLowerInvariant();
        ProductShape best = ProductShape.Generic;
        int bestLen = 0;
        foreach (ProductShape p in System.Enum.GetValues(typeof(ProductShape)))
        {
            if (p == ProductShape.Auto || p == ProductShape.Generic) continue;
            string pn = p.ToString().ToLowerInvariant();
            if (n.Contains(pn) && pn.Length > bestLen) { best = p; bestLen = pn.Length; }
        }
        return best;
    }

    private void BuildShape(ProductShape sh, float u)
    {
        Color red = new Color(0.85f, 0.12f, 0.12f), orange = new Color(1f, 0.55f, 0.05f),
              yellow = new Color(1f, 0.88f, 0.15f), green = new Color(0.15f, 0.6f, 0.2f),
              dkGreen = new Color(0.05f, 0.3f, 0.1f), brown = new Color(0.4f, 0.25f, 0.1f),
              pine = new Color(0.95f, 0.65f, 0.1f), white = new Color(0.95f, 0.95f, 0.95f),
              blue = new Color(0.2f, 0.4f, 0.9f), pink = new Color(0.9f, 0.45f, 0.6f),
              meat = new Color(0.85f, 0.25f, 0.3f), silver = new Color(0.75f, 0.78f, 0.82f);

        const PrimitiveType Cube = PrimitiveType.Cube, Sph = PrimitiveType.Sphere, Cyl = PrimitiveType.Cylinder;
        switch (sh)
        {
            case ProductShape.Apple:
                Part(Sph, new Vector3(0, 0.28f, 0), new Vector3(0.5f, 0.5f, 0.5f), Vector3.zero, red, u);
                Part(Cube, new Vector3(0, 0.56f, 0), new Vector3(0.05f, 0.12f, 0.05f), Vector3.zero, brown, u);
                Part(Sph, new Vector3(0.09f, 0.6f, 0), new Vector3(0.16f, 0.04f, 0.09f), new Vector3(0, 0, 20), green, u);
                break;
            case ProductShape.Orange:
                Part(Sph, new Vector3(0, 0.28f, 0), new Vector3(0.52f, 0.52f, 0.52f), Vector3.zero, orange, u);
                Part(Sph, new Vector3(0, 0.55f, 0), new Vector3(0.12f, 0.05f, 0.12f), Vector3.zero, green, u);
                break;
            case ProductShape.Banana:
                for (int b = -1; b <= 1; b += 2)
                {
                    float z = 0.1f * b;
                    Part(Cube, new Vector3(-0.22f, 0.2f, z), new Vector3(0.3f, 0.09f, 0.12f), new Vector3(0, 0, -35), yellow, u);
                    Part(Cube, new Vector3(0f, 0.12f, z), new Vector3(0.3f, 0.09f, 0.12f), Vector3.zero, yellow, u);
                    Part(Cube, new Vector3(0.22f, 0.2f, z), new Vector3(0.3f, 0.09f, 0.12f), new Vector3(0, 0, 35), yellow, u);
                    Part(Cube, new Vector3(0.36f, 0.3f, z), new Vector3(0.06f, 0.06f, 0.06f), Vector3.zero, brown, u);
                }
                break;
            case ProductShape.Pineapple:
                Part(Cyl, new Vector3(0, 0.3f, 0), new Vector3(0.32f, 0.3f, 0.32f), Vector3.zero, pine, u);
                for (int i = -1; i <= 1; i++)
                    Part(Cube, new Vector3(0.05f * i, 0.72f, 0), new Vector3(0.05f, 0.26f, 0.05f), new Vector3(0, 0, -22 * i), green, u);
                break;
            case ProductShape.Mango:
                Part(Sph, new Vector3(0, 0.22f, 0), new Vector3(0.5f, 0.42f, 0.4f), new Vector3(0, 0, 15), new Color(1f, 0.6f, 0.15f), u);
                Part(Sph, new Vector3(0.08f, 0.3f, 0), new Vector3(0.3f, 0.2f, 0.3f), Vector3.zero, new Color(0.9f, 0.3f, 0.15f), u);
                Part(Cube, new Vector3(0, 0.46f, 0), new Vector3(0.04f, 0.08f, 0.04f), Vector3.zero, brown, u);
                break;
            case ProductShape.Eggs:
                for (int i = -1; i <= 1; i++)
                    for (int j = 0; j < 2; j++)
                        Part(Sph, new Vector3(0.18f * i, 0.12f, -0.1f + 0.2f * j), new Vector3(0.16f, 0.2f, 0.16f), Vector3.zero, new Color(0.96f, 0.9f, 0.8f), u);
                Part(Cube, new Vector3(0, 0.02f, 0), new Vector3(0.6f, 0.04f, 0.45f), Vector3.zero, new Color(0.6f, 0.55f, 0.5f), u);
                break;
            case ProductShape.Watermelon:
                Part(Sph, new Vector3(0, 0.22f, 0), new Vector3(0.8f, 0.5f, 0.5f), Vector3.zero, green, u);
                float[] xs = { -0.2f, 0f, 0.2f };
                foreach (float x in xs)
                {
                    float y = 0.22f + 0.25f * Mathf.Sqrt(Mathf.Max(0f, 1f - (x / 0.4f) * (x / 0.4f)));
                    Part(Cube, new Vector3(x, y, 0), new Vector3(0.04f, 0.03f, 0.35f), Vector3.zero, dkGreen, u);
                }
                break;

            case ProductShape.Milk:
                Part(Cube, new Vector3(0, 0.23f, 0), new Vector3(0.3f, 0.46f, 0.3f), Vector3.zero, white, u);
                Part(Cube, new Vector3(0, 0.2f, 0), new Vector3(0.31f, 0.14f, 0.31f), Vector3.zero, blue, u);
                Part(Cube, new Vector3(0, 0.5f, 0), new Vector3(0.3f, 0.06f, 0.14f), Vector3.zero, blue, u);
                break;
            case ProductShape.Cheese:
                Part(Cyl, new Vector3(0, 0.13f, 0), new Vector3(0.56f, 0.13f, 0.56f), Vector3.zero, yellow, u);
                Part(Sph, new Vector3(0.1f, 0.265f, 0.05f), new Vector3(0.09f, 0.02f, 0.09f), Vector3.zero, pine, u);
                Part(Sph, new Vector3(-0.12f, 0.265f, -0.08f), new Vector3(0.07f, 0.02f, 0.07f), Vector3.zero, pine, u);
                Part(Sph, new Vector3(0.0f, 0.265f, -0.15f), new Vector3(0.06f, 0.02f, 0.06f), Vector3.zero, pine, u);
                break;
            case ProductShape.Yogurt:
                for (int i = -1; i <= 1; i++)
                {
                    Part(Cyl, new Vector3(0.2f * i, 0.13f, 0), new Vector3(0.18f, 0.13f, 0.18f), Vector3.zero, white, u);
                    Part(Cyl, new Vector3(0.2f * i, 0.17f, 0), new Vector3(0.19f, 0.04f, 0.19f), Vector3.zero, pink, u);
                    Part(Cyl, new Vector3(0.2f * i, 0.27f, 0), new Vector3(0.2f, 0.015f, 0.2f), Vector3.zero, silver, u);
                }
                break;

            case ProductShape.Can:
                for (int i = -1; i <= 1; i += 2)
                {
                    Part(Cyl, new Vector3(0.17f * i, 0.2f, 0), new Vector3(0.28f, 0.2f, 0.28f), Vector3.zero, red, u);
                    Part(Cyl, new Vector3(0.17f * i, 0.405f, 0), new Vector3(0.26f, 0.01f, 0.26f), Vector3.zero, silver, u);
                }
                break;
            case ProductShape.Bottle:
                for (int i = -1; i <= 1; i += 2)
                {
                    Part(Cyl, new Vector3(0.17f * i, 0.175f, 0), new Vector3(0.22f, 0.175f, 0.22f), Vector3.zero, new Color(0.5f, 0.8f, 1f), u);
                    Part(Cyl, new Vector3(0.17f * i, 0.41f, 0), new Vector3(0.1f, 0.06f, 0.1f), Vector3.zero, new Color(0.5f, 0.8f, 1f), u);
                    Part(Cyl, new Vector3(0.17f * i, 0.5f, 0), new Vector3(0.12f, 0.02f, 0.12f), Vector3.zero, white, u);
                }
                break;
            case ProductShape.JuiceBox:
                Part(Cube, new Vector3(0, 0.2f, 0), new Vector3(0.28f, 0.4f, 0.18f), Vector3.zero, orange, u);
                Part(Cube, new Vector3(0, 0.2f, 0), new Vector3(0.29f, 0.12f, 0.19f), Vector3.zero, white, u);
                Part(Cube, new Vector3(0.07f, 0.5f, 0), new Vector3(0.025f, 0.22f, 0.025f), new Vector3(0, 0, -15), red, u);
                break;

            case ProductShape.Steak:
                Part(Cube, new Vector3(0, 0.06f, 0), new Vector3(0.5f, 0.12f, 0.38f), Vector3.zero, meat, u);
                Part(Cube, new Vector3(0, 0.07f, 0.19f), new Vector3(0.52f, 0.1f, 0.05f), Vector3.zero, white, u);
                Part(Sph, new Vector3(-0.13f, 0.14f, 0f), new Vector3(0.12f, 0.1f, 0.12f), Vector3.zero, white, u);
                break;
            case ProductShape.Sausage:
                Part(Cyl, new Vector3(0, 0.07f, -0.14f), new Vector3(0.13f, 0.26f, 0.13f), new Vector3(0, 0, 90), new Color(0.75f, 0.3f, 0.2f), u);
                Part(Cyl, new Vector3(0, 0.07f, 0.14f), new Vector3(0.13f, 0.26f, 0.13f), new Vector3(0, 0, 90), new Color(0.75f, 0.3f, 0.2f), u);
                Part(Cyl, new Vector3(0, 0.19f, 0f), new Vector3(0.13f, 0.26f, 0.13f), new Vector3(0, 0, 90), new Color(0.8f, 0.35f, 0.25f), u);
                break;
            case ProductShape.Ham:
                Part(Sph, new Vector3(0, 0.2f, 0), new Vector3(0.5f, 0.4f, 0.4f), Vector3.zero, pink, u);
                Part(Cyl, new Vector3(0.3f, 0.22f, 0), new Vector3(0.07f, 0.1f, 0.07f), new Vector3(0, 0, 90), white, u);
                Part(Sph, new Vector3(0.41f, 0.22f, 0), new Vector3(0.11f, 0.11f, 0.11f), Vector3.zero, white, u);
                break;

            case ProductShape.WineBottle:
                for (int i = -1; i <= 1; i += 2)
                {
                    Part(Cyl, new Vector3(0.16f * i, 0.175f, 0), new Vector3(0.18f, 0.175f, 0.18f), Vector3.zero, dkGreen, u);
                    Part(Cyl, new Vector3(0.16f * i, 0.45f, 0), new Vector3(0.07f, 0.1f, 0.07f), Vector3.zero, dkGreen, u);
                    Part(Cyl, new Vector3(0.16f * i, 0.18f, 0), new Vector3(0.19f, 0.06f, 0.19f), Vector3.zero, new Color(0.95f, 0.9f, 0.7f), u);
                    Part(Cyl, new Vector3(0.16f * i, 0.56f, 0), new Vector3(0.05f, 0.02f, 0.05f), Vector3.zero, brown, u);
                }
                break;
            case ProductShape.Champagne:
                Part(Cyl, new Vector3(0, 0.19f, 0), new Vector3(0.22f, 0.19f, 0.22f), Vector3.zero, new Color(0.08f, 0.2f, 0.1f), u);
                Part(Cyl, new Vector3(0, 0.5f, 0), new Vector3(0.09f, 0.12f, 0.09f), Vector3.zero, Gold, u);
                Part(Cyl, new Vector3(0, 0.62f, 0), new Vector3(0.11f, 0.02f, 0.11f), Vector3.zero, silver, u);
                Part(Cyl, new Vector3(0, 0.2f, 0), new Vector3(0.23f, 0.06f, 0.23f), Vector3.zero, white, u);
                break;
            case ProductShape.Barrel:
                Part(Cyl, new Vector3(0, 0.21f, 0), new Vector3(0.44f, 0.21f, 0.44f), Vector3.zero, new Color(0.5f, 0.3f, 0.12f), u);
                Part(Cyl, new Vector3(0, 0.09f, 0), new Vector3(0.46f, 0.012f, 0.46f), Vector3.zero, new Color(0.2f, 0.2f, 0.22f), u);
                Part(Cyl, new Vector3(0, 0.33f, 0), new Vector3(0.46f, 0.012f, 0.46f), Vector3.zero, new Color(0.2f, 0.2f, 0.22f), u);
                break;

            default:
                Part(Cube, new Vector3(0, 0.15f, 0), new Vector3(0.4f, 0.3f, 0.4f), Vector3.zero, new Color(0.7f, 0.5f, 0.3f), u);
                break;
        }
    }

    private void BuildTag(CargoItem cargo, float u)
    {
        if (cargo == null) return;
        string txt = "";
        if (cargo.isVip) txt += "VIP\n";
        if (cargo.cargoType == CargoType.Fragile) txt += "FRAGILE\n";
        if (cargo.cargoType == CargoType.Heavy) txt += "HEAVY\n";
        if (txt.Length == 0) return;

        var go = new GameObject("Tag");
        go.transform.SetParent(emblemRoot, false);
        go.transform.localPosition = new Vector3(0f, 0.95f * u, 0f);
        var t = go.AddComponent<TextMeshPro>();
        t.text = txt.TrimEnd('\n');
        t.fontSize = 3.2f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = cargo.isVip ? Gold : cargo.cargoType == CargoType.Fragile ? new Color(1f, 0.5f, 0.5f) : Color.white;
        t.outlineWidth = 0.3f;
        t.outlineColor = new Color32(0, 0, 0, 255);
        t.rectTransform.sizeDelta = new Vector2(5f, 2f);
        go.AddComponent<Billboard>();
    }

    private void Part(PrimitiveType type, Vector3 pos, Vector3 scale, Vector3 euler, Color color, float u)
    {
        var go = GameObject.CreatePrimitive(type);
        DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(emblemRoot, false);
        go.transform.localPosition = pos * u;
        go.transform.localScale = scale * u;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.GetComponent<Renderer>().sharedMaterial = VisualKit.Mat(color);
    }
}
