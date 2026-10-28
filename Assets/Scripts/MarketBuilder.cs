using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MarketBuilder : MonoBehaviour
{
    public class Plan
    {
        public Bounds floor, region, stock;
        public List<SectionZone> zones = new List<SectionZone>();
        public bool hasFreezerIsland;
        public Bounds freezerIsland;
    }

    private Transform root;
    public Transform Root => root;
    private float top;
    private Bounds region;
    private float stockZ;
    private readonly System.Random rng = new System.Random(2024);

    public static bool ShowWalls = true;

    private static readonly Color[] Palette =
    {
        new Color(0.9f,0.25f,0.25f), new Color(0.95f,0.8f,0.2f), new Color(0.25f,0.6f,0.95f),
        new Color(0.3f,0.8f,0.4f), new Color(0.95f,0.5f,0.15f), new Color(0.7f,0.4f,0.85f)
    };
    private static readonly Color WallColor = new Color(0.78f, 0.82f, 0.88f);
    private static readonly Color Steel = new Color(0.6f, 0.62f, 0.66f);
    private static readonly Color Dark = new Color(0.12f, 0.12f, 0.15f);
    private static readonly Color Glass = new Color(0.55f, 0.75f, 0.9f);

    public static bool FindFloor(out Bounds floor)
    {
        floor = new Bounds(Vector3.zero, Vector3.zero);
        float bestArea = 0f;
        bool found = false;
        foreach (var c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (c.isTrigger) continue;
            Bounds b = c.bounds;
            if (b.size.y > 3f) continue;
            float area = b.size.x * b.size.z;
            if (area < 1500f) continue;  
            if (area > bestArea) { bestArea = area; floor = b; found = true; }
        }
        return found;
    }

    public void Build(Plan p, Transform parent = null)
    {
        var rootGo = new GameObject("MarketDecor");
        root = rootGo.transform;
        if (parent != null) root.SetParent(parent, false);
        top = p.floor.max.y;
        region = p.region;
        stockZ = p.stock.center.z;

        StoreShell(p);
        foreach (var z in p.zones) { ZoneFloor(z); ZoneSign(z); ZoneProps(z); }
        if (p.hasFreezerIsland) FreezerIsland(p.freezerIsland);
        ReceivingArea(p.stock);
        ModelProps(p.stock);
        LoadingDock(p.stock);
        Checkouts();
        Pillars();
        BackWall();
        FridgeWall();
        Entrance();
        Bakery();
        InfoDesk();
        EndcapDisplays(p.zones);
        AisleBanners(p.zones);
        FloorArrows();
        StreetFurniture();
        Posters();
    }

    private GameObject Box(string n, Vector3 pos, Vector3 size, Color c, bool col, Transform parent = null)
    {
        return VisualKit.Prim(PrimitiveType.Cube, n, pos, size, c, col, parent != null ? parent : root);
    }

    private GameObject Cyl(string n, Vector3 pos, float diameter, float height, Color c, bool col, Transform parent = null)
    {
        return VisualKit.Prim(PrimitiveType.Cylinder, n, pos, new Vector3(diameter, height * 0.5f, diameter), c, col, parent != null ? parent : root);
    }

    private GameObject Sph(string n, Vector3 pos, float diameter, Color c, Transform parent = null)
    {
        return VisualKit.Prim(PrimitiveType.Sphere, n, pos, Vector3.one * diameter, c, false, parent != null ? parent : root);
    }

    private void Textured(GameObject go, string texName)
    {
        var m = ModelCatalog.MarketMat(texName);
        if (m != null) go.GetComponent<Renderer>().sharedMaterial = m;
    }

    private static string EndcapTex(string section, int k)
    {
        string[][] map =
        {
            new[] { "fruits", "greens" }, 
            new[] { "misc", "dumplings" },  
            new[] { "candy", "chips" },  
            new[] { "meat", "dumplings" },  
            new[] { "misc", "household" },  
        };
        int idx = 0;
        switch (section) { case "Dairy": idx = 1; break; case "Drinks": idx = 2; break; case "Butcher": idx = 3; break; case "Wine": idx = 4; break; }
        return map[idx][k % 2];
    }

    private bool Free(Vector3 center, Vector3 half)
    {
        return !Physics.CheckBox(center, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
    }

    private void Sync() { Physics.SyncTransforms(); }

    private void Sign(string text, Vector3 pos, Color c, float fontSize)
    {
        var go = new GameObject("Sign_" + text);
        go.transform.SetParent(root, true);
        go.transform.position = pos;
        var t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = TextAlignmentOptions.Center;
        t.color = c;
        t.outlineWidth = 0.25f;
        t.outlineColor = new Color32(0, 0, 0, 255);
        t.rectTransform.sizeDelta = new Vector2(fontSize * 3f, fontSize * 0.6f);
        go.AddComponent<Billboard>();
        Billboard.FaceNow(go.transform);
    }

    // texto fixo em parede/pôster
    private void FixedText(string text, Vector3 pos, Quaternion rot, Color c, float width)
    {
        var go = new GameObject("Text_" + text);
        go.transform.SetParent(root, true);
        go.transform.position = pos;
        go.transform.rotation = rot;
        var t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = Mathf.Min(8f, width * 10f / (0.55f * Mathf.Max(4, text.Length)));
        t.alignment = TextAlignmentOptions.Center;
        t.color = c;
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color32(0, 0, 0, 255);
        t.rectTransform.sizeDelta = new Vector2(width, width * 0.4f);
    }


    private void StoreShell(Plan p)
    {
        Bounds r = p.region;

        var ground = Box("PhysicalGround", new Vector3(r.center.x, top - 0.5f, r.center.z), new Vector3(r.size.x + 60f, 1f, r.size.z + 60f), Color.gray, true);
        ground.GetComponent<Renderer>().enabled = false;

        if (!TileFloor(r, p.stock))
            Box("StoreFloor", new Vector3(r.center.x, top + 0.012f, r.center.z), new Vector3(r.size.x, 0.02f, r.size.z), new Color(0.88f, 0.86f, 0.80f), false);

        const float H = 8f, T = 0.6f;
        Wall("WestWall", new Vector3(r.min.x, top + H / 2f, r.center.z), new Vector3(T, H, r.size.z));
        Wall("EastWall", new Vector3(r.max.x, top + H / 2f, r.center.z), new Vector3(T, H, r.size.z));
        Wall("SouthWall", new Vector3(r.center.x, top + H / 2f, r.min.z), new Vector3(r.size.x, H, T));
        Wall("NorthWall", new Vector3(r.center.x, top + H / 2f, r.max.z), new Vector3(r.size.x, H, T));

        Color stripe = new Color(0.85f, 0.2f, 0.2f);
        float inset = T * 0.5f + 0.04f;
        Box("StripeSouth", new Vector3(r.center.x, top + 3.2f, r.min.z + inset), new Vector3(r.size.x - T, 0.35f, 0.06f), stripe, false);
        Box("StripeNorth", new Vector3(r.center.x, top + 3.2f, r.max.z - inset), new Vector3(r.size.x - T, 0.35f, 0.06f), stripe, false);
        Box("StripeWest", new Vector3(r.min.x + inset, top + 3.2f, r.center.z), new Vector3(0.06f, 0.35f, r.size.z - T), stripe, false);
        Box("StripeEast", new Vector3(r.max.x - inset, top + 3.2f, r.center.z), new Vector3(0.06f, 0.35f, r.size.z - T), stripe, false);
        Box("BaseboardSouth", new Vector3(r.center.x, top + 0.25f, r.min.z + inset), new Vector3(r.size.x - T, 0.5f, 0.06f), Steel, false);
        Box("BaseboardNorth", new Vector3(r.center.x, top + 0.25f, r.max.z - inset), new Vector3(r.size.x - T, 0.5f, 0.06f), Steel, false);
        Box("BaseboardWest", new Vector3(r.min.x + inset, top + 0.25f, r.center.z), new Vector3(0.06f, 0.5f, r.size.z - T), Steel, false);
        Box("BaseboardEast", new Vector3(r.max.x - inset, top + 0.25f, r.center.z), new Vector3(0.06f, 0.5f, r.size.z - T), Steel, false);

        Sign("FORKLIFT SUPERMARKET", new Vector3(r.center.x, top + 10f, r.min.z + 1f), new Color(1f, 0.85f, 0.2f), 26f);
    }

    private void Wall(string n, Vector3 pos, Vector3 size)
    {
        var go = Box(n, pos, size, WallColor, true);
        if (!ShowWalls) go.GetComponent<Renderer>().enabled = false;
    }

    private bool TileFloor(Bounds r, Bounds stock)
    {
        if (!ModelCatalog.Has("floortile")) return false;
        int nx = Mathf.Max(1, Mathf.RoundToInt(r.size.x / 6f));
        int nz = Mathf.Max(1, Mathf.RoundToInt(r.size.z / 6f));
        float tx = r.size.x / nx, tz = r.size.z / nz;
        var hazard = stock; hazard.Expand(4f);
        bool stripes = ModelCatalog.Has("floorstripes");

        var floorRoot = new GameObject("FloorTiles").transform;
        floorRoot.SetParent(root, true);
        for (int ix = 0; ix < nx; ix++)
            for (int iz = 0; iz < nz; iz++)
            {
                Vector3 c = new Vector3(r.min.x + (ix + 0.5f) * tx, top + 0.012f, r.min.z + (iz + 0.5f) * tz);
                bool hz = stripes && hazard.Contains(new Vector3(c.x, hazard.center.y, c.z));
                var holder = new GameObject("Tile");
                holder.transform.SetParent(floorRoot, true);
                holder.transform.position = c;
                var inst = ModelCatalog.Spawn(hz ? "floorstripes" : "floortile", 0, holder.transform);
                if (inst == null) { VisualKit.Kill(holder); continue; }
                ModelCatalog.FitToBox(inst, holder.transform, new Vector3(tx, 0.15f, tz));
                inst.transform.localPosition += new Vector3(0f, -0.075f, 0f);
            }
        return true;
    }

    private void ZoneFloor(SectionZone z)
    {
        Color c = Color.Lerp(SectionInfo.ColorOf(z.section), new Color(0.92f, 0.92f, 0.92f), 0.55f);
        Vector3 size = new Vector3(z.bounds.size.x + 3f, 0.02f, z.bounds.size.z + 3f);
        Box("Zone_" + z.section, new Vector3(z.bounds.center.x, top + 0.02f, z.bounds.center.z), size, c, false);
    }

    private void ZoneSign(SectionZone z)
    {
        Sign(SectionInfo.Display(z.section).ToUpperInvariant(),
             new Vector3(z.bounds.center.x, top + 7.5f, z.bounds.center.z), SectionInfo.ColorOf(z.section), 22f);
    }

    private void ZoneProps(SectionZone z)
    {
        Vector3 b = new Vector3(z.bounds.center.x, top, z.bounds.min.z - 3f);  

        switch (z.section)
        {
            case "Fruits":
                for (int i = -1; i <= 1; i++)
                {
                    Vector3 c = b + new Vector3(i * 3.4f, 0f, 0f);
                    if (!Free(c + Vector3.up * 0.5f, new Vector3(1.2f, 0.6f, 0.7f))) continue;
                    Box("ProduceTable", c + Vector3.up * 0.4f, new Vector3(2.2f, 0.8f, 1.2f), new Color(0.55f, 0.38f, 0.2f), true);
                    for (int k = 0; k < 6; k++)
                    {
                        Color fc = Palette[rng.Next(4)];
                        Vector3 pp = c + new Vector3(-0.8f + k * 0.32f, 0.97f, (k % 2 == 0 ? -0.25f : 0.25f));
                        Sph("Fruit", pp, 0.34f, fc);
                    }
                    Sync();
                }
                break;

            case "Dairy":
                for (int i = -1; i <= 1; i++)
                {
                    Vector3 c = b + new Vector3(i * 2.6f, 0f, 0f);
                    if (!Free(c + Vector3.up * 1.1f, new Vector3(0.95f, 1.1f, 0.55f))) continue;
                    Box("Fridge", c + Vector3.up * 1.1f, new Vector3(1.8f, 2.2f, 1.0f), new Color(0.95f, 0.96f, 0.98f), true);
                    Box("BlueStripe", c + new Vector3(0f, 1.75f, 0f), new Vector3(1.82f, 0.25f, 1.02f), new Color(0.2f, 0.4f, 0.9f), false);
                    Box("FridgeGlass", c + new Vector3(0f, 0.95f, -0.52f), new Vector3(1.5f, 1.3f, 0.05f), new Color(0.35f, 0.55f, 0.75f), false);
                    Sync();
                }
                break;

            case "Drinks":
                for (int i = -1; i <= 1; i += 2)
                {
                    Vector3 c = b + new Vector3(i * 2.2f, 0f, 0f);
                    if (!Free(c + Vector3.up * 0.9f, new Vector3(0.85f, 0.9f, 0.75f))) continue;
                    Box("Pallet", c + Vector3.up * 0.08f, new Vector3(1.5f, 0.15f, 1.3f), new Color(0.55f, 0.4f, 0.22f), true);
                    Color[] layers = { new Color(0.1f, 0.7f, 0.75f), new Color(0.95f, 0.55f, 0.15f), new Color(0.85f, 0.2f, 0.2f) };
                    for (int l = 0; l < 3; l++)
                        Box("DrinkCrate", c + new Vector3(0f, 0.4f + l * 0.5f, 0f), new Vector3(1.4f, 0.48f, 1.2f), layers[l], false);
                    Sync();
                }
                {
                    Vector3 c = b + new Vector3(0f, 0f, -1.5f);
                    if (Free(c + Vector3.up * 0.7f, new Vector3(0.95f, 0.7f, 0.55f)))
                    {
                        Box("Cooler", c + Vector3.up * 0.7f, new Vector3(1.8f, 1.4f, 1.0f), new Color(0.6f, 0.82f, 0.95f), true);
                        Sync();
                    }
                }
                break;

            case "Butcher":
            {
                Vector3 c = b;
                if (Free(c + Vector3.up * 0.5f, new Vector3(2.6f, 0.55f, 0.7f)))
                {
                    Box("ButcherCounter", c + Vector3.up * 0.5f, new Vector3(5.0f, 1.0f, 1.2f), new Color(0.95f, 0.95f, 0.95f), true);
                    Box("CounterFront", c + new Vector3(0f, 0.48f, -0.62f), new Vector3(5.0f, 0.95f, 0.06f), new Color(0.8f, 0.15f, 0.2f), false);
                    Box("HookRail", c + new Vector3(0f, 3.2f, 0f), new Vector3(5.0f, 0.1f, 0.1f), Steel, false);
                    Box("PostL", c + new Vector3(-2.5f, 1.6f, 0f), new Vector3(0.12f, 3.2f, 0.12f), Steel, false);
                    Box("PostR", c + new Vector3(2.5f, 1.6f, 0f), new Vector3(0.12f, 3.2f, 0.12f), Steel, false);
                    for (int i = 0; i < 5; i++)
                        VisualKit.Prim(PrimitiveType.Capsule, "Ham", c + new Vector3(-2f + i * 1.0f, 2.55f, 0f), new Vector3(0.4f, 0.55f, 0.4f), new Color(0.85f, 0.3f, 0.35f), false, root);
                    for (int i = 0; i < 4; i++)
                        Textured(Box("Meat", c + new Vector3(-1.5f + i * 1.0f, 1.08f, 0f), new Vector3(0.7f, 0.12f, 0.6f), new Color(0.85f, 0.25f, 0.3f), false), "meat");
                    Sync();
                }
                break;
            }

            case "Wine":
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 c = b + new Vector3((i % 2) * 1.3f - 0.65f - 2.6f, 0f, (i / 2) * 1.3f);
                    if (!Free(c + Vector3.up * 0.7f, new Vector3(0.55f, 0.7f, 0.55f))) continue;
                    if (ModelCatalog.Has("barrel"))
                    {
                        PlaceModelProp("barrel", c, 1.3f, (float)rng.NextDouble() * 360f, i);
                        Sync();
                        continue;
                    }
                    Cyl("Barrel", c + Vector3.up * 0.7f, 1.0f, 1.4f, new Color(0.5f, 0.3f, 0.12f), true);
                    Cyl("BarrelHoop", c + Vector3.up * 0.3f, 1.03f, 0.06f, new Color(0.2f, 0.2f, 0.22f), false);
                    Cyl("BarrelHoop", c + Vector3.up * 1.1f, 1.03f, 0.06f, new Color(0.2f, 0.2f, 0.22f), false);
                    Sync();
                }
                Vector3 rc = b + new Vector3(1.8f, 0f, 0.3f);
                if (Free(rc + Vector3.up * 1f, new Vector3(1.5f, 1f, 0.4f)))
                {
                    Box("WineRack", rc + Vector3.up * 1f, new Vector3(3.0f, 2.0f, 0.7f), new Color(0.45f, 0.28f, 0.14f), true);
                    for (int row = 0; row < 3; row++)
                        for (int k = 0; k < 6; k++)
                            Cyl("RackBottle", rc + new Vector3(-1.2f + k * 0.48f, 0.5f + row * 0.55f, -0.36f), 0.22f, 0.08f, new Color(0.1f, 0.3f, 0.15f), false)
                                .transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    Sync();
                }
                break;
            }
        }
    }

    private void FreezerIsland(Bounds isl)
    {
        Sign("FROZEN", new Vector3(isl.center.x, top + 7.5f, isl.center.z), new Color(0.6f, 0.85f, 1f), 22f);
        for (int i = 0; i < 5; i++)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 c = new Vector3(isl.center.x + side * 1.3f, top + 0.5f, isl.min.z + 1.5f + i * (isl.size.z - 3f) / 4f);
                if (!Free(c, new Vector3(0.7f, 0.55f, 1.0f))) continue;
                Box("Freezer", c, new Vector3(1.3f, 1.0f, 2.2f), new Color(0.95f, 0.96f, 0.98f), true);
                Box("FreezerLid", c + Vector3.up * 0.52f, new Vector3(1.35f, 0.06f, 2.25f), new Color(0.35f, 0.6f, 0.85f), false);
                Sync();
            }
        }
    }

    private void ReceivingArea(Bounds stock)
    {
        Color yellow = new Color(1f, 0.82f, 0.1f);
        float y = top + 0.03f;
        float hx = stock.extents.x + 3f, hz = stock.extents.z + 3f;
        Vector3 c = new Vector3(stock.center.x, y, stock.center.z);
        Box("Mark", new Vector3(c.x, y, c.z + hz), new Vector3(hx * 2f, 0.02f, 0.25f), yellow, false);
        Box("Mark", new Vector3(c.x, y, c.z - hz), new Vector3(hx * 2f, 0.02f, 0.25f), yellow, false);
        Box("Mark", new Vector3(c.x + hx, y, c.z), new Vector3(0.25f, 0.02f, hz * 2f), yellow, false);
        Box("Mark", new Vector3(c.x - hx, y, c.z), new Vector3(0.25f, 0.02f, hz * 2f), yellow, false);
        Sign("RECEIVING", new Vector3(c.x, top + 4f, c.z), yellow, 14f);
    }

    private void ModelProps(Bounds stock)
    {
        if (!ModelCatalog.Has("pallet")) return;
        const float palletW = 2.4f;

        for (int i = 0; i < 4; i++)
        {
            Vector3 pn = ModelCatalog.MeasureSize("pallet", i);
            float f = palletW / Mathf.Max(0.0001f, Mathf.Max(pn.x, pn.z));  
            Vector3 pos = new Vector3(stock.min.x - 7f - (i % 2) * 3.2f, top, stock.center.z - 4f + (i / 2) * 6f);
            if (!Free(pos + Vector3.up * 0.8f, new Vector3(1.3f, 0.8f, 1.3f))) continue;

            Bounds pb = PlaceModelProp("pallet", pos, palletW, 0f, i);
            if (ModelCatalog.Has("carton"))
            {
                float y = pos.y + pb.size.y;
                float h = ModelCatalog.MeasureSize("carton", i).y * f;
                PlaceModelPropScaled("carton", i, new Vector3(pos.x - 0.55f, y, pos.z), f, 0f);
                PlaceModelPropScaled("carton", i + 1, new Vector3(pos.x + 0.55f, y, pos.z), f, 0f);
                PlaceModelPropScaled("carton", i + 2, new Vector3(pos.x, y + h, pos.z), f, 0f);
            }
            Sync();
        }
    }

    private Bounds PlaceModelProp(string key, Vector3 groundPos, float maxDim, float yaw, int seed)
    {
        var holder = new GameObject("Prop_" + key);
        holder.transform.SetParent(root, true);
        holder.transform.position = groundPos;
        holder.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        var inst = ModelCatalog.Spawn(key, seed, holder.transform);
        if (inst == null) { VisualKit.Kill(holder); return new Bounds(Vector3.zero, Vector3.zero); }
        Bounds b = ModelCatalog.FitInto(inst, holder.transform, maxDim);

        var col = holder.AddComponent<BoxCollider>();
        col.center = b.center;
        col.size = b.size;
        return b;
    }

    private Bounds PlaceModelPropScaled(string key, int seed, Vector3 groundPos, float factor, float yaw)
    {
        var holder = new GameObject("Prop_" + key);
        holder.transform.SetParent(root, true);
        holder.transform.position = groundPos;
        holder.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        var inst = ModelCatalog.Spawn(key, seed, holder.transform);
        if (inst == null) { VisualKit.Kill(holder); return new Bounds(Vector3.zero, Vector3.zero); }
        inst.transform.localScale = Vector3.one * factor;
        Bounds b = ModelCatalog.Ground(inst, holder.transform);

        var col = holder.AddComponent<BoxCollider>();
        col.center = b.center;
        col.size = b.size;
        return b;
    }

    private void LoadingDock(Bounds stock)
    {
        float x = region.min.x + 0.36f;
        Vector3 c = new Vector3(x, top + 2f, stock.center.z);
        var door = Box("DockDoor", c, new Vector3(0.08f, 4f, 5f), new Color(0.7f, 0.72f, 0.76f), false);
        for (int i = 0; i < 8; i++)
            Box("DockDoorSlat", new Vector3(x + 0.05f, top + 0.25f + i * 0.5f, stock.center.z), new Vector3(0.04f, 0.04f, 5f), new Color(0.45f, 0.47f, 0.5f), false);
        Box("DockFrame", new Vector3(x + 0.03f, top + 4.1f, stock.center.z), new Vector3(0.12f, 0.2f, 5.4f), new Color(0.95f, 0.8f, 0.1f), false);
        FixedText("LOADING DOCK", new Vector3(x + 0.1f, top + 4.9f, stock.center.z), Quaternion.LookRotation(Vector3.left), new Color(1f, 0.85f, 0.2f), 5f);
    }

    private void Checkouts()
    {
        Bounds r = region;
        for (int i = 0; i < 4; i++)
        {
            Vector3 pos = new Vector3(r.min.x + 5.5f + i * 5.5f, top + 0.5f, r.min.z + 5f);
            if (!Free(pos, new Vector3(1.3f, 0.6f, 0.7f))) continue;

            var lane = new GameObject("Checkout_" + (i + 1)).transform;
            lane.SetParent(root, true);

            Box("Counter", pos, new Vector3(2.4f, 1.0f, 1.1f), new Color(0.2f, 0.3f, 0.6f), true, lane);
            Box("CounterTop", pos + Vector3.up * 0.52f, new Vector3(2.5f, 0.05f, 1.2f), new Color(0.85f, 0.87f, 0.9f), false, lane);
            Box("ConveyorBelt", pos + new Vector3(-0.3f, 0.57f, 0f), new Vector3(1.5f, 0.06f, 0.7f), Dark, false, lane);
            if (ModelCatalog.Has("mktpos"))
                PlaceModelPropScaled("mktpos", 0, pos + new Vector3(0.85f, 0.55f, 0f), 1.4f, 180f); 
            else
            {
                Box("PosTerminal", pos + new Vector3(0.85f, 0.85f, 0f), new Vector3(0.4f, 0.4f, 0.35f), new Color(0.15f, 0.85f, 0.5f), false, lane);
                Box("CardReader", pos + new Vector3(0.85f, 0.62f, -0.4f), new Vector3(0.2f, 0.08f, 0.2f), Dark, false, lane);
            }
            Box("DividerBar", pos + new Vector3(0.2f, 0.65f, 0f), new Vector3(0.06f, 0.1f, 0.6f), new Color(0.95f, 0.8f, 0.1f), false, lane);
            Box("BagRack", pos + new Vector3(1.35f, 0.9f, 0.3f), new Vector3(0.08f, 0.6f, 0.4f), Steel, false, lane);
            for (int k = 0; k < 3; k++)
                Box("Bag", pos + new Vector3(1.28f, 0.95f - k * 0.15f, 0.3f), new Vector3(0.06f, 0.22f, 0.3f), Palette[(i + k) % Palette.Length], false, lane);
            Cyl("LightPole", pos + new Vector3(-1.2f, 1.6f, 0.4f), 0.08f, 2.2f, Steel, false, lane);
            Box("OpenLight", pos + new Vector3(-1.2f, 2.8f, 0.4f), new Vector3(0.7f, 0.3f, 0.1f), new Color(0.2f, 0.9f, 0.3f), false, lane);
            for (int s = -1; s <= 1; s += 2)
                Cyl("QueuePost", pos + new Vector3(s * 1.1f, 0.0f, -2.6f) + Vector3.up * 0.5f, 0.1f, 1.0f, Steel, false, lane);
            Box("QueueRope", pos + new Vector3(0f, 0.85f, -2.6f), new Vector3(2.2f, 0.05f, 0.05f), new Color(0.85f, 0.15f, 0.15f), false, lane);

            Sign("CHECKOUT " + (i + 1), pos + Vector3.up * 3.3f, Color.white, 6f);
            Sync();
        }
    }

    private void Pillars()
    {
        Bounds r = region;
        int idx = 0;
        for (float x = r.min.x + 6f; x < r.max.x - 3f; x += 14f)
            for (float z = r.min.z + 8f; z < r.max.z - 6f; z += 16f, idx++)
            {
                Vector3 c = new Vector3(x, top + 3.5f, z);
                if (!Free(c, new Vector3(0.7f, 3.5f, 0.7f))) continue;
                Box("Pillar", c, new Vector3(0.9f, 7f, 0.9f), new Color(0.7f, 0.72f, 0.76f), true);
                Box("PillarStripe", c + new Vector3(0f, -1.8f, 0f), new Vector3(0.94f, 0.3f, 0.94f), new Color(0.95f, 0.8f, 0.1f), false);
                if (idx % 2 == 0)
                {
                    Cyl("FireExtinguisher", new Vector3(x, top + 1.1f, z - 0.55f), 0.22f, 0.6f, new Color(0.85f, 0.1f, 0.1f), false);
                    Box("ExtinguisherSign", new Vector3(x, top + 1.9f, z - 0.48f), new Vector3(0.3f, 0.3f, 0.04f), new Color(0.85f, 0.1f, 0.1f), false);
                }
                else
                {
                    Box("ExitSign", new Vector3(x, top + 4.6f, z - 0.5f), new Vector3(0.95f, 0.35f, 0.08f), new Color(0.1f, 0.65f, 0.25f), false);
                    FixedText("EXIT", new Vector3(x, top + 4.6f, z - 0.56f), Quaternion.LookRotation(Vector3.forward), Color.white, 0.9f);
                }
                Sync();
            }
    }

    private void BackWallGondolas()
    {
        Bounds r = region;
        float z = r.max.z - 1.5f;
        float x = r.min.x + 4f;
        for (int i = 0; i < 12; i++)
        {
            Vector3 size = ModelCatalog.MeasureSize("mktgondola", i);
            if (size.x < 0.5f) break;
            if (x + size.x > r.max.x - 2f) break;
            Vector3 pos = new Vector3(x + size.x * 0.5f, top, z);
            if (Free(pos + Vector3.up * size.y * 0.5f, new Vector3(size.x * 0.48f, size.y * 0.48f, size.z * 0.48f)))
            {
                PlaceModelPropScaled("mktgondola", i, pos, 1f, 0f);
                Sync();
            }
            x += size.x + 0.6f;
        }
    }

    private void BackWall()
    {
        if (ModelCatalog.Has("mktgondola")) { BackWallGondolas(); return; }
        Bounds r = region;
        float z = r.max.z - 1.3f;
        int gi = 0;
        for (float x = r.min.x + 3f; x < r.max.x - 3f; x += 3.2f, gi++)
        {
            if (gi % 3 == 1 && ModelCatalog.Has("rack") && Free(new Vector3(x, top + 1.6f, z), new Vector3(1.3f, 1.6f, 0.7f)))
            {
                PlaceModelProp("rack", new Vector3(x, top, z), 3.4f, 0f, gi);
                Sync();
                continue;
            }
            Vector3 c = new Vector3(x, top + 0.9f, z);
            if (!Free(c, new Vector3(1.4f, 0.9f, 0.5f))) continue;
            Box("BackGondola", c, new Vector3(2.6f, 1.8f, 0.9f), new Color(0.82f, 0.84f, 0.88f), true);
            for (int row = 0; row < 2; row++)
                for (int k = 0; k < 5; k++)
                    Box("Product", c + new Vector3(-1.0f + k * 0.5f, -0.25f + row * 0.6f, -0.42f), new Vector3(0.36f, 0.36f, 0.12f), Palette[rng.Next(Palette.Length)], false);
            Sync();
        }
    }

    private void FridgeWallModels()
    {
        Bounds r = region;
        float z = Mathf.Max(r.min.z + 16f, stockZ + 5f); 
        int n = 0;
        for (int i = 0; i < 8; i++)
        {
            Vector3 size = ModelCatalog.MeasureSize("mktfridge", i);
            if (size.x < 0.5f) break;
            float len = size.x;  
            if (z + len > r.max.z - 4f) break;
            Vector3 pos = new Vector3(r.min.x + 0.5f + size.z * 0.5f, top, z + len * 0.5f);
            if (Free(pos + Vector3.up * size.y * 0.5f, new Vector3(size.z * 0.48f, size.y * 0.48f, len * 0.48f)))
            {
                PlaceModelPropScaled("mktfridge", i, pos, 1f, -90f);
                Sync();
                n++;
            }
            z += len + 0.3f;
        }
        if (n > 0) Sign("CHILLED", new Vector3(r.min.x + 2f, top + 4.2f, z - 6f), new Color(0.5f, 0.8f, 1f), 10f);
    }

    private void FridgeWall()
    {
        if (ModelCatalog.Has("mktfridge")) { FridgeWallModels(); return; }
        Bounds r = region;
        float x = r.min.x + 1.3f;
        int n = 0;
        for (float z = r.min.z + 16f; z < r.max.z - 4f; z += 2.5f)
        {
            Vector3 c = new Vector3(x, top + 1.3f, z);
            if (!Free(c, new Vector3(0.7f, 1.3f, 1.2f))) continue;

            var unit = new GameObject("ChillerUnit").transform;
            unit.SetParent(root, true);
            Box("ChillerBody", c, new Vector3(1.2f, 2.6f, 2.3f), new Color(0.93f, 0.94f, 0.96f), true, unit);
            Box("ChillerGlass", c + new Vector3(0.62f, 0.05f, 0f), new Vector3(0.05f, 2.2f, 2.0f), Glass, false, unit);
            Box("ChillerLight", c + new Vector3(0.62f, 1.25f, 0f), new Vector3(0.08f, 0.1f, 2.1f), Color.white, false, unit);
            for (int row = 0; row < 3; row++)
                for (int k = 0; k < 5; k++)
                    Box("ChillerProduct", c + new Vector3(0.66f, -0.7f + row * 0.7f, -0.8f + k * 0.4f), new Vector3(0.06f, 0.4f, 0.3f), Palette[(n + row + k) % Palette.Length], false, unit);
            n++;
            Sync();
        }
        if (n > 0) Sign("CHILLED", new Vector3(x + 1f, top + 5.5f, r.min.z + 16f + n * 1.25f), new Color(0.5f, 0.8f, 1f), 10f);
    }

    private void Entrance()
    {
        Bounds r = region;
        float zWall = r.min.z + 0.36f;
        float cx = r.center.x;

        Box("DoorFrameL", new Vector3(cx - 3.2f, top + 2f, zWall), new Vector3(0.3f, 4f, 0.2f), Steel, false);
        Box("DoorFrameR", new Vector3(cx + 3.2f, top + 2f, zWall), new Vector3(0.3f, 4f, 0.2f), Steel, false);
        Box("DoorHeader", new Vector3(cx, top + 4.2f, zWall), new Vector3(6.7f, 0.6f, 0.2f), new Color(0.2f, 0.3f, 0.6f), false);
        Box("DoorGlassL", new Vector3(cx - 1.55f, top + 1.9f, zWall), new Vector3(2.9f, 3.6f, 0.08f), Glass, false);
        Box("DoorGlassR", new Vector3(cx + 1.55f, top + 1.9f, zWall), new Vector3(2.9f, 3.6f, 0.08f), Glass, false);
        FixedText("ENTRANCE", new Vector3(cx, top + 4.2f, zWall + 0.12f), Quaternion.LookRotation(Vector3.back), Color.white, 5f);
        Box("WelcomeMat", new Vector3(cx, top + 0.035f, zWall + 1.4f), new Vector3(4.5f, 0.03f, 2f), new Color(0.2f, 0.22f, 0.26f), false);

        for (int row = 0; row < 2; row++)
        {
            float x = cx + 6.5f + row * 1.6f;
            if (!Free(new Vector3(x, top + 0.6f, zWall + 2.4f), new Vector3(0.6f, 0.6f, 2.2f))) continue;
            for (int i = 0; i < 6; i++)
                ShoppingCart(new Vector3(x, top, zWall + 1.0f + i * 0.34f), 0f);
            Sync();
        }

        for (int s = 0; s < 3; s++)
        {
            Vector3 b = new Vector3(cx - 6f - s * 1.0f, top, zWall + 1.2f);
            if (!Free(b + Vector3.up * 0.6f, new Vector3(0.35f, 0.6f, 0.4f))) continue;
            Color bc = s % 2 == 0 ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.95f, 0.55f, 0.1f);
            for (int k = 0; k < 6; k++)
                Box("Basket", b + new Vector3(0f, 0.12f + k * 0.16f, 0f), new Vector3(0.6f, 0.15f, 0.42f), bc, false);
            Sync();
        }

        float[] px = { -9f, -3.8f, 3.8f, 9f };
        foreach (float dx in px) Plant(new Vector3(cx + dx, top, zWall + 1.0f));
    }

    private void ShoppingCart(Vector3 pos, float yaw)
    {
        var cart = new GameObject("ShoppingCart").transform;
        cart.SetParent(root, true);
        cart.position = pos;
        cart.rotation = Quaternion.Euler(0f, yaw, 0f);

        Color metal = new Color(0.78f, 0.8f, 0.84f);
        Box("Basket", pos + new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 0.42f, 0.95f), metal, false, cart);
        Box("Tray", pos + new Vector3(0f, 0.35f, 0f), new Vector3(0.62f, 0.05f, 0.85f), metal, false, cart);
        Box("Handle", pos + new Vector3(0f, 1.18f, -0.52f), new Vector3(0.72f, 0.05f, 0.05f), new Color(0.9f, 0.2f, 0.2f), false, cart);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                var w = Cyl("Wheel", pos + new Vector3(sx * 0.27f, 0.07f, sz * 0.35f), 0.14f, 0.05f, Dark, false, cart);
                w.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }
    }

    private void Plant(Vector3 pos)
    {
        if (!Free(pos + Vector3.up * 0.6f, new Vector3(0.4f, 0.6f, 0.4f))) return;
        var plant = new GameObject("Plant").transform;
        plant.SetParent(root, true);
        Cyl("Pot", pos + Vector3.up * 0.3f, 0.7f, 0.6f, new Color(0.75f, 0.4f, 0.25f), true, plant);
        Sph("Foliage", pos + new Vector3(0f, 1.05f, 0f), 1.0f, new Color(0.2f, 0.6f, 0.25f), plant);
        Sph("Foliage", pos + new Vector3(0.25f, 1.4f, 0.1f), 0.7f, new Color(0.3f, 0.7f, 0.3f), plant);
        Sync();
    }

    private void Bakery()
    {
        Bounds r = region;
        float x = r.max.x - 3f;
        float z = r.center.z + 14f;
        Vector3 c = new Vector3(x, top + 0.55f, z);
        if (!Free(c, new Vector3(0.9f, 0.6f, 1.9f))) return;

        var bakery = new GameObject("Bakery").transform;
        bakery.SetParent(root, true);
        Box("DisplayCase", c, new Vector3(1.2f, 1.1f, 3.6f), new Color(0.95f, 0.93f, 0.88f), true, bakery);
        Box("CaseGlass", c + new Vector3(0f, 0.75f, 0f), new Vector3(1.1f, 0.55f, 3.5f), Glass, false, bakery);
        for (int i = 0; i < 8; i++)
        {
            var bread = VisualKit.Prim(PrimitiveType.Capsule, "Bread", c + new Vector3(0f, 0.55f, -1.5f + i * 0.43f), new Vector3(0.3f, 0.2f, 0.3f), new Color(0.85f, 0.6f, 0.3f), false, bakery);
            bread.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }
        Box("BakeryShelf", new Vector3(x + 1.8f, top + 1.2f, z), new Vector3(0.6f, 2.4f, 3.6f), new Color(0.6f, 0.42f, 0.25f), true, bakery);
        for (int row = 0; row < 3; row++)
            for (int k = 0; k < 6; k++)
                Box("Loaf", new Vector3(x + 1.45f, top + 0.5f + row * 0.7f, z - 1.5f + k * 0.6f), new Vector3(0.12f, 0.3f, 0.4f), new Color(0.88f, 0.65f, 0.35f), false, bakery);
        Box("Oven", new Vector3(x + 1.8f, top + 0.9f, z + 3.4f), new Vector3(1.2f, 1.8f, 1.6f), new Color(0.25f, 0.25f, 0.28f), true, bakery);
        Box("OvenGlow", new Vector3(x + 1.18f, top + 0.9f, z + 3.4f), new Vector3(0.06f, 0.7f, 1.0f), new Color(1f, 0.55f, 0.15f), false, bakery);
        Sign("BAKERY", new Vector3(x + 0.6f, top + 4.2f, z), new Color(1f, 0.8f, 0.4f), 10f);
        Sync();
    }

    private void InfoDesk()
    {
        Bounds r = region;
        Vector3 c = new Vector3(r.center.x - 12f, top + 0.55f, r.min.z + 4.5f);
        if (!Free(c, new Vector3(1.6f, 0.6f, 0.7f))) return;
        var desk = new GameObject("InfoDesk").transform;
        desk.SetParent(root, true);
        Box("DeskBody", c, new Vector3(3.0f, 1.1f, 1.0f), new Color(0.2f, 0.45f, 0.75f), true, desk);
        Box("DeskTop", c + Vector3.up * 0.58f, new Vector3(3.2f, 0.07f, 1.2f), new Color(0.9f, 0.9f, 0.92f), false, desk);
        Box("DeskMonitor", c + new Vector3(0.6f, 0.95f, 0f), new Vector3(0.5f, 0.35f, 0.06f), Dark, false, desk);
        Sign("INFO", c + Vector3.up * 3f, new Color(0.4f, 0.8f, 1f), 7f);
        Sync();
    }

    private void EndcapDisplays(List<SectionZone> zones)
    {
        foreach (var z in zones)
        {
            Vector3 c = new Vector3(z.bounds.center.x, top + 0.45f, z.bounds.max.z + 2.8f);
            if (c.z > region.max.z - 4f) continue;
            if (!Free(c, new Vector3(1.2f, 0.5f, 0.7f))) continue;
            var display = new GameObject("Endcap_" + z.section).transform;
            display.SetParent(root, true);
            Color sc = SectionInfo.ColorOf(z.section);
            Box("EndcapTable", c, new Vector3(2.2f, 0.9f, 1.0f), Color.Lerp(sc, Color.white, 0.5f), true, display);
            for (int k = 0; k < 4; k++)
                Textured(Box("EndcapBox", c + new Vector3(-0.8f + k * 0.55f, 0.65f, 0f), new Vector3(0.45f, 0.4f, 0.5f), Palette[rng.Next(Palette.Length)], false, display), EndcapTex(z.section, k));
            Box("PriceTag", c + new Vector3(0f, 0.9f, -0.55f), new Vector3(0.9f, 0.35f, 0.04f), new Color(1f, 0.9f, 0.2f), false, display);
            FixedText("SALE", c + new Vector3(0f, 0.9f, -0.58f), Quaternion.LookRotation(Vector3.forward), new Color(0.8f, 0.1f, 0.1f), 0.85f);
            Sync();
        }
    }

    private void AisleBanners(List<SectionZone> zones)
    {
        string[] texts = { "SALE", "FRESH", "2 FOR 1", "LOW PRICES" };
        Color[] cols = { new Color(0.85f, 0.15f, 0.15f), new Color(0.2f, 0.65f, 0.3f), new Color(0.95f, 0.65f, 0.1f), new Color(0.25f, 0.5f, 0.9f) };
        for (int i = 0; i + 1 < zones.Count && i < 4; i++)
        {
            Vector3 mid = (zones[i].bounds.center + zones[i + 1].bounds.center) * 0.5f;
            if (Mathf.Abs(zones[i].bounds.center.z - zones[i + 1].bounds.center.z) > 3f) continue;  
            Vector3 b = new Vector3(mid.x, top, zones[i].bounds.center.z);
            if (!Free(b + Vector3.up * 3f, new Vector3(2.5f, 3f, 0.4f))) continue;
            Color c = cols[i % cols.Length];
            Cyl("BannerPoleL", b + new Vector3(-2.4f, 3.2f, 0f), 0.14f, 6.4f, Steel, false);
            Cyl("BannerPoleR", b + new Vector3(2.4f, 3.2f, 0f), 0.14f, 6.4f, Steel, false);
            Box("Banner", b + new Vector3(0f, 6.0f, 0f), new Vector3(4.6f, 1.1f, 0.06f), c, false);
            Sign(texts[i % texts.Length], b + new Vector3(0f, 6.0f, -0.1f), Color.white, 6f);
            Sync();
        }
    }

    private void FloorArrows()
    {
        Bounds r = region;
        Color y = new Color(1f, 0.82f, 0.1f);
        for (int i = 0; i < 4; i++)
        {
            float z = r.min.z + 4.5f + i * 3.2f;
            Vector3 c = new Vector3(r.center.x, top + 0.04f, z);
            var a = Box("ArrowL", c + new Vector3(-0.4f, 0f, -0.2f), new Vector3(1.2f, 0.02f, 0.22f), y, false);
            a.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
            var b = Box("ArrowR", c + new Vector3(0.4f, 0f, -0.2f), new Vector3(1.2f, 0.02f, 0.22f), y, false);
            b.transform.rotation = Quaternion.Euler(0f, -40f, 0f);
        }
    }

    private void StreetFurniture()
    {
        Bounds r = region;
        AFrame(new Vector3(r.center.x + 2.8f, top, r.min.z + 3.4f), "WET FLOOR", new Color(0.98f, 0.85f, 0.1f), new Color(0.1f, 0.1f, 0.1f));
        AFrame(new Vector3(r.center.x - 3.2f, top, r.min.z + 9f), "SALE", new Color(0.85f, 0.15f, 0.15f), Color.white);
        AFrame(new Vector3(r.center.x + 13f, top, r.min.z + 7f), "FRESH!", new Color(0.2f, 0.6f, 0.9f), Color.white);

        Vector3[] bins = {
            new Vector3(r.min.x + 4f, top, r.min.z + 2f), new Vector3(r.max.x - 3f, top, r.min.z + 2.5f),
            new Vector3(r.min.x + 3f, top, r.center.z), new Vector3(r.max.x - 3f, top, r.max.z - 5f) };
        foreach (var b in bins)
        {
            if (!Free(b + Vector3.up * 0.5f, new Vector3(0.35f, 0.5f, 0.35f))) continue;
            Cyl("TrashBin", b + Vector3.up * 0.45f, 0.6f, 0.9f, new Color(0.35f, 0.4f, 0.38f), true);
            Cyl("BinLid", b + Vector3.up * 0.93f, 0.66f, 0.07f, Dark, false);
            Sync();
        }
    }

    private void AFrame(Vector3 pos, string text, Color bg, Color fg)
    {
        if (!Free(pos + Vector3.up * 0.5f, new Vector3(0.5f, 0.5f, 0.4f))) return;
        var sign = new GameObject("AFrame_" + text).transform;
        sign.SetParent(root, true);
        var a = Box("PanelFront", pos + new Vector3(0f, 0.5f, -0.15f), new Vector3(0.7f, 0.95f, 0.04f), bg, false, sign);
        a.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        var b = Box("PanelBack", pos + new Vector3(0f, 0.5f, 0.15f), new Vector3(0.7f, 0.95f, 0.04f), bg, false, sign);
        b.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
        Sign(text, pos + new Vector3(0f, 1.25f, -0.2f), fg, 2.2f);
        Sync();
    }

    private void Posters()
    {
        Bounds r = region;
        string[] north = { "FRESH PRODUCE", "LOW PRICES", "FINE WINES", "ORGANIC", "SALE" };
        Color[] cols = { new Color(0.2f, 0.6f, 0.3f), new Color(0.85f, 0.2f, 0.2f), new Color(0.55f, 0.15f, 0.4f), new Color(0.3f, 0.65f, 0.35f), new Color(0.95f, 0.6f, 0.1f) };
        float inset = 0.36f;

        for (int i = 0; i < north.Length; i++)
        {
            float x = r.min.x + 6f + i * ((r.size.x - 12f) / (north.Length - 1));
            Vector3 c = new Vector3(x, top + 5.6f, r.max.z - inset);
            if (i == north.Length - 1 && ModelCatalog.MarketMat("category_sign") != null)
            {
                Textured(Box("CategoryBoard", c, new Vector3(4.2f, 2.36f, 0.06f), Color.white, false), "category_sign");
                continue;
            }
            Box("Poster", c, new Vector3(4.2f, 1.9f, 0.06f), cols[i], false);
            FixedText(north[i], c + new Vector3(0f, 0f, -0.06f), Quaternion.LookRotation(Vector3.forward), Color.white, 3.9f);
        }

        string[] west = { "FRESH", "COLD DRINKS", "BAKERY", "WELCOME" };
        for (int i = 0; i < west.Length; i++)
        {
            float z = r.min.z + 8f + i * ((r.size.z - 16f) / (west.Length - 1));
            Vector3 c = new Vector3(r.min.x + inset, top + 5.6f, z);
            Box("Poster", c, new Vector3(0.06f, 1.9f, 4.2f), cols[(i + 2) % cols.Length], false);
            FixedText(west[i], c + new Vector3(0.06f, 0f, 0f), Quaternion.LookRotation(Vector3.left), Color.white, 3.9f);
        }
    }
}
