using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public struct Load
    {
        public string section; public int count;
        public Load(string s, int c) { section = s; count = c; }
    }

    public struct LevelConfig
    {
        public string name;
        public Load[] loads;
        public int fragile, heavy, vip, obstacles;
        public float time;
        public int Total { get { int t = 0; foreach (var l in loads) t += l.count; return t; } }
    }

    private static LevelConfig Cfg(string n, Load[] loads, int f, int h, int v, int o, float t)
    {
        return new LevelConfig { name = n, loads = loads, fragile = f, heavy = h, vip = v, obstacles = o, time = t };
    }


    public static readonly LevelConfig[] Levels =
    {
        Cfg("Produce",     new[] { new Load("Fruits", 5) },  1, 0, 0, 0, 180f),
        Cfg("Dairy",       new[] { new Load("Dairy", 5) },   1, 1, 0, 1, 240f),
        Cfg("Drinks",      new[] { new Load("Drinks", 5) },  2, 1, 1, 2, 270f),
        Cfg("Butcher",     new[] { new Load("Butcher", 5) }, 2, 2, 1, 3, 300f),
        Cfg("Wine Cellar", new[] { new Load("Wine", 6) },    2, 2, 2, 4, 360f),
    };

    public static LevelConfig Current => Levels[Mathf.Clamp(RunState.Level, 1, Levels.Length) - 1];

    private struct Cell { public string section; public int col, row; public Cell(string s, int c, int r) { section = s; col = c; row = r; } }
    private static readonly Cell[] Layout =
    {
        new Cell("Dairy", 1, 0), new Cell("Drinks", 2, 0), new Cell("Butcher", 1, 1), new Cell("Wine", 2, 1),
    };
    private const float ColSpacing = 9f;
    private const float RowSpacing = 22f;

    public List<CargoItem> Cargos { get; } = new List<CargoItem>(); 
    public List<string> ActiveSections { get; } = new List<string>();

    private bool prepared;
    private System.Random rng;
    private GameObject template;
    private Bounds stock, region;
    private float cargoY;
    private Transform obstaclesParent;
    private DriverController drv;

    private void Awake() { Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public bool IsSectionComplete(string section)
    {
        bool any = false;
        foreach (var c in Cargos)
        {
            if (c == null || c.sectionName != section) continue;
            any = true;
            if (!c.IsPlaced) return false;
        }
        return any;
    }

    public int SectionTotal(string section)
    {
        int n = 0; foreach (var c in Cargos) if (c != null && c.sectionName == section) n++; return n;
    }

    public int SectionDone(string section)
    {
        int n = 0; foreach (var c in Cargos) if (c != null && c.sectionName == section && c.IsPlaced) n++; return n;
    }

    public class StaticResult
    {
        public Bounds stock, region;
        public float floorTop, cargoY;
        public Vector3 fruitsCenter;
        public GameObject template;
    }

    public class BakeSeed
    {
        public Bounds stock;
        public float cargoY;
        public Vector3 fruitsCenter;
        public GameObject templateSource;
    }

    public void Prepare()
    {
        if (prepared) return;
        drv = FindFirstObjectByType<DriverController>();
        var fork = FindFirstObjectByType<ForkOperatorController>();
        Transform marketRoot;

        var baked = FindFirstObjectByType<MarketRoot>();
        if (baked != null && baked.cargoTemplate != null)
        {
            VisualKit.Init(baked.cargoTemplate.GetComponentInChildren<Renderer>(true).sharedMaterial.shader);
            template = baked.cargoTemplate;
            stock = new Bounds(baked.stockCenter, baked.stockSize);
            region = new Bounds(baked.regionCenter, baked.regionSize);
            cargoY = baked.stockCenter.y;
            marketRoot = baked.transform;
        }
        else
        {
            var res = BuildStatic(null, null);
            if (res == null) return;
            template = res.template; stock = res.stock; region = res.region; cargoY = res.cargoY;
            var mb = GetComponent<MarketBuilder>();
            marketRoot = mb != null ? mb.Root : null;
        }
        prepared = true;

        HideOriginalEnvironment(marketRoot, GetShelfRoots(), drv != null ? drv.transform : null);


        if (drv != null)
            foreach (var r in drv.GetComponentsInChildren<MeshRenderer>())
                if (r.GetComponent<TMPro.TMP_Text>() == null) OutlineUtil.Apply(r);

  
        if (fork != null) GhostForkAgainstShelves(fork);

        BeginStage(false);
    }


    public StaticResult BuildStatic(Transform parent, BakeSeed seed)
    {
        var res = new StaticResult();
        var fork = FindFirstObjectByType<ForkOperatorController>();
        var driver = FindFirstObjectByType<DriverController>();

        if (seed != null)
        {
            VisualKit.Init(seed.templateSource.GetComponentInChildren<Renderer>(true).sharedMaterial.shader);
            res.stock = seed.stock;
            res.cargoY = seed.cargoY;
            res.fruitsCenter = seed.fruitsCenter;
            res.template = Instantiate(seed.templateSource, parent != null ? parent : transform);
            res.template.name = "CargoTemplate";
            res.template.SetActive(false);
        }
        else
        {
            var sceneCargos = new List<CargoItem>(FindObjectsByType<CargoItem>(FindObjectsSortMode.None));
            if (sceneCargos.Count == 0) return null;
            VisualKit.Init(sceneCargos[0].GetComponentInChildren<Renderer>().sharedMaterial.shader);

            res.stock = new Bounds(sceneCargos[0].transform.position, Vector3.zero);
            foreach (var c in sceneCargos) res.stock.Encapsulate(c.transform.position);
            res.cargoY = sceneCargos[0].transform.position.y;

            res.template = Instantiate(sceneCargos[0].gameObject, parent != null ? parent : transform);
            res.template.name = "CargoTemplate";
            res.template.SetActive(false);
            foreach (var o in sceneCargos) { o.gameObject.SetActive(false); VisualKit.Kill(o.gameObject); }
        }

        Bounds floor;
        if (!MarketBuilder.FindFloor(out floor)) floor = new Bounds(new Vector3(res.stock.center.x, -0.5f, res.stock.center.z), new Vector3(400f, 1f, 400f));
        res.floorTop = floor.max.y;

        var originalRoots = GetShelfRoots();
        var zones = new List<SectionZone>();

        if (ModelCatalog.Has("shelfrack"))
        {
            Vector3 fruitsCenter = seed != null ? seed.fruitsCenter : BoundsOf(originalRoots).center;
            res.fruitsCenter = fruitsCenter;
            foreach (var r in originalRoots) { r.gameObject.SetActive(false); VisualKit.Kill(r.gameObject); }
            float reachTop = fork != null ? fork.MaxCargoHeightWorld : floor.max.y + 6f;

            zones.Add(BuildRackBank("Fruits", fruitsCenter, floor.max.y, reachTop, parent));
            foreach (var cell in Layout)
                zones.Add(BuildRackBank(cell.section, fruitsCenter + new Vector3(cell.col * ColSpacing, 0f, cell.row * RowSpacing), floor.max.y, reachTop, parent));
        }
        else
        {
            float topAllowed = fork != null ? fork.MaxCargoHeightWorld - 0.2f : float.MaxValue;
            foreach (var r in originalRoots) FitShelfHeight(r, topAllowed);
            zones.Add(new SectionZone { section = "Fruits", bounds = BoundsOf(originalRoots) });
            foreach (var cell in Layout) zones.Add(CloneSection(cell, originalRoots));
        }
        Physics.SyncTransforms();

        var plan = new MarketBuilder.Plan { floor = floor, stock = res.stock, zones = zones };
        Bounds all = zones[0].bounds;
        foreach (var z in zones) { all.Encapsulate(z.bounds.min); all.Encapsulate(z.bounds.max); }
        all.Encapsulate(res.stock.min); all.Encapsulate(res.stock.max);
        if (driver != null) all.Encapsulate(driver.transform.position);

        Vector3 mn = all.min - new Vector3(11f, 0f, 9f);
        Vector3 mx = all.max + new Vector3(10f, 0f, 4f);
        mn.x = Mathf.Max(mn.x, floor.min.x + 1.5f); mn.z = Mathf.Max(mn.z, floor.min.z + 1.5f);
        mx.x = Mathf.Min(mx.x, floor.max.x - 1.5f); mx.z = Mathf.Min(mx.z, floor.max.z - 1.5f);
        res.region = new Bounds((mn + mx) / 2f, mx - mn);
        plan.region = res.region;

        Bounds isl = zones[0].bounds;
        isl.center += new Vector3(0f, 0f, RowSpacing);
        plan.hasFreezerIsland = true;
        plan.freezerIsland = isl;

        var market = GetComponent<MarketBuilder>();
        if (market != null) market.Build(plan, parent);
        Physics.SyncTransforms();

        foreach (var slot in FindObjectsByType<ShelfSlot>(FindObjectsSortMode.None))
        {
            var label = slot.GetComponent<SlotLabel>();
            if (label == null) { label = slot.gameObject.AddComponent<SlotLabel>(); label.Init(slot); }
        }

        return res;
    }

    private void HideOriginalEnvironment(Transform marketRoot, List<Transform> shelfRoots, Transform forklift)
    {
        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (r.GetComponent<TMPro.TMP_Text>() != null) continue;
            Transform t = r.transform;
            if (t.IsChildOf(transform)) continue;  
            if (marketRoot != null && t.IsChildOf(marketRoot)) continue; 
            if (forklift != null && t.IsChildOf(forklift)) continue;  
            if (t.GetComponentInParent<CargoItem>() != null) continue; 
            if (t.GetComponentInParent<PatrolObstacle>() != null) continue;
            bool keep = false;
            foreach (var s in shelfRoots) if (t.IsChildOf(s)) { keep = true; break; }
            if (keep) continue;
            r.enabled = false;
        }
    }


    public void BeginStage(bool clearPrevious)
    {
        if (!prepared || template == null) return;

        if (ShelfManager.Instance != null) ShelfManager.Instance.ClearHighlight();
        foreach (var c in Cargos)
        {
            if (c == null) continue;
            if (clearPrevious)
            {
                if (c.IsCarried) c.Release();
                if (c.PlacedSlot != null) c.PlacedSlot.Vacate();
                Destroy(c.gameObject);
            }
            else c.Locked = true;
        }
        Cargos.Clear();
        ActiveSections.Clear();
        if (obstaclesParent != null) { Destroy(obstaclesParent.gameObject); obstaclesParent = null; }

        var cfg = Current;
        rng = new System.Random(1000 + RunState.Level * 17);

        var spawned = SpawnStageCargos(cfg);
        Cargos.AddRange(spawned);
        foreach (var l in cfg.loads) ActiveSections.Add(l.section);

        foreach (var c in spawned)
        {
            var v = c.GetComponent<CargoVisual>();
            if (v == null) v = c.gameObject.AddComponent<CargoVisual>();
            v.shape = ShapeFor(c);
            v.useCrateColor = true;
            v.crateColor = SectionInfo.ColorOf(c.sectionName);
            v.Build();
        }

        SpawnObstacles(cfg);
    }


    private List<Transform> GetShelfRoots()
    {
        var roots = new List<Transform>();
        foreach (var slot in FindObjectsByType<ShelfSlot>(FindObjectsSortMode.None))
        {
            Transform t = slot.transform.parent != null ? slot.transform.parent.parent : null;
            if (t != null && !roots.Contains(t)) roots.Add(t);
        }
        roots.Sort((a, b) => a.position.z.CompareTo(b.position.z));
        return roots;
    }

    private void FitShelfHeight(Transform root, float topAllowed)
    {
        var slots = root.GetComponentsInChildren<ShelfSlot>();
        if (slots.Length < 2) return;
        float lo = float.MaxValue, hi = float.MinValue;
        foreach (var s in slots) { lo = Mathf.Min(lo, s.transform.position.y); hi = Mathf.Max(hi, s.transform.position.y); }
        if (hi <= topAllowed || hi - lo < 0.01f) return;

        float ratio = Mathf.Clamp((topAllowed - lo) / (hi - lo), 0.3f, 1f);
        Vector3 sc = root.localScale;
        root.localScale = new Vector3(sc.x, sc.y * ratio, sc.z);

        float newLo = float.MaxValue;
        foreach (var s in slots) newLo = Mathf.Min(newLo, s.transform.position.y);
        root.position += Vector3.up * (lo - newLo);
    }

    private static readonly float[] RackTierTops = { 0.670f, 1.288f, 1.910f };
    private const int RacksPerSection = 4;
    private const float RackScaleH = 2.0f;

    private SectionZone BuildRackBank(string section, Vector3 center, float floorTop, float reachTop, Transform parent)
    {
        var info = SectionInfo.Get(section);
        var bank = new GameObject("RackBank_" + section).transform;
        bank.position = new Vector3(center.x, floorTop, center.z);
        if (parent != null) bank.SetParent(parent, true);

        float sH = RackScaleH;
        float sV = Mathf.Min(sH, Mathf.Max(0.8f, (reachTop - floorTop - 0.3f) / RackTierTops[RackTierTops.Length - 1]));
        Vector3 nat = ModelCatalog.MeasureSize("shelfrack", 0);
        float width = Mathf.Max(0.5f, nat.x) * sH;
        Color sectionColor = SectionInfo.ColorOf(section);
        Color padColor = Color.Lerp(sectionColor, Color.white, 0.55f);

        int n = 0;
        for (int i = 0; i < RacksPerSection; i++)
        {
            var holder = new GameObject("Rack_" + (i + 1)).transform;
            holder.SetParent(bank, false);
            holder.localPosition = new Vector3(0f, 0f, (i - (RacksPerSection - 1) * 0.5f) * (width + 0.1f));
            holder.localRotation = Quaternion.Euler(0f, 90f, 0f);  

            var inst = ModelCatalog.Spawn("shelfrack", 0, holder);
            if (inst == null) break;
            inst.name = "RackModel";
            inst.transform.localScale = new Vector3(sH, sV, sH);
            Bounds b = ModelCatalog.Ground(inst, holder);

            for (int k = 0; k < RackTierTops.Length; k++)
            {
                var board = holder.gameObject.AddComponent<BoxCollider>();
                board.center = new Vector3(b.center.x, RackTierTops[k] * sV - 0.03f, b.center.z);
                board.size = new Vector3(b.size.x * 0.98f, 0.06f, b.size.z * 0.95f);
            }
            float px = b.size.x * 0.5f - 0.07f * sH, pz = b.size.z * 0.5f - 0.07f * sH;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    var post = holder.gameObject.AddComponent<BoxCollider>();
                    post.center = new Vector3(b.center.x + sx * px, b.size.y * 0.5f, b.center.z + sz * pz);
                    post.size = new Vector3(0.1f * sH, b.size.y, 0.1f * sH);
                }

            for (int k = 0; k < RackTierTops.Length; k++)
            {
                string id = info.prefix + "-" + (++n).ToString("00");
                var go = new GameObject("Slot_" + id);
                go.transform.SetParent(holder, false);
                go.transform.localPosition = new Vector3(b.center.x, RackTierTops[k] * sV, b.center.z);

                var trig = go.AddComponent<BoxCollider>();
                trig.isTrigger = true;
                trig.size = new Vector3(b.size.x * 0.92f, 0.1f, b.size.z * 0.85f);

                var slot = go.AddComponent<ShelfSlot>();
                slot.sectionName = section;
                slot.slotId = id;

                var pad = VisualKit.Prim(PrimitiveType.Cube, "Pad", go.transform.position, Vector3.one, padColor, false, go.transform);
                pad.transform.localRotation = Quaternion.identity;
                pad.transform.localPosition = new Vector3(0f, 0.006f, 0f);
                pad.transform.localScale = new Vector3(trig.size.x * 0.96f, 0.01f, trig.size.z * 0.96f);
                slot.SetRenderer(pad.GetComponent<Renderer>());
            }
        }

        Bounds zb = new Bounds(bank.position, Vector3.zero);
        bool first = true;
        foreach (var r in bank.GetComponentsInChildren<Renderer>())
        {
            if (first) { zb = r.bounds; first = false; } else zb.Encapsulate(r.bounds);
        }
        return new SectionZone { section = section, bounds = zb };
    }

    private SectionZone CloneSection(Cell cell, List<Transform> originals)
    {
        var info = SectionInfo.Get(cell.section);
        Bounds zb = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        int unit = 0;
        foreach (var orig in originals)
        {
            Vector3 off = new Vector3(cell.col * ColSpacing, 0f, cell.row * RowSpacing);
            var clone = Instantiate(orig.gameObject, orig.position + off, orig.rotation);
            clone.name = "Shelf_" + cell.section + "_" + (++unit);
            foreach (var s in clone.GetComponentsInChildren<ShelfSlot>())
            {
                string id = s.slotId ?? "";
                int dash = id.LastIndexOf('-');
                string suffix = dash >= 0 ? id.Substring(dash + 1) : id;
                s.sectionName = cell.section;
                s.slotId = info.prefix + "-" + suffix;
            }
            foreach (var r in clone.GetComponentsInChildren<Renderer>())
            {
                if (first) { zb = r.bounds; first = false; } else zb.Encapsulate(r.bounds);
            }
        }
        return new SectionZone { section = cell.section, bounds = zb };
    }

    private Bounds BoundsOf(List<Transform> roots)
    {
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        foreach (var root in roots)
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
        return b;
    }

    private void GhostForkAgainstShelves(ForkOperatorController fork)
    {
        var shelfColliders = new List<Collider>();
        foreach (var root in GetShelfRoots())
            foreach (var c in root.GetComponentsInChildren<Collider>())
                if (!c.isTrigger) shelfColliders.Add(c);

        Transform mount = fork.ForkMount;
        foreach (var arm in fork.GetComponentsInChildren<Collider>())
        {
            if (arm.isTrigger) continue;
            if (mount != null && !arm.transform.IsChildOf(mount)) continue;
            foreach (var sc in shelfColliders) Physics.IgnoreCollision(arm, sc, true);
        }
    }

    private ProductShape ShapeFor(CargoItem c)
    {
        var info = SectionInfo.Get(c.sectionName);
        if (info == null || info.shapes.Length == 0) return ProductShape.Generic;
        string n = c.name;
        foreach (var s in info.shapes) if (n == "Cargo_" + s) return s;
        return info.shapes[rng.Next(info.shapes.Length)];
    }

    private List<CargoItem> SpawnStageCargos(LevelConfig cfg)
    {
        int total = cfg.Total;
        var idx = new List<int>();
        for (int i = 0; i < total; i++) idx.Add(i);
        Shuffle(idx);
        int p = 0;
        var fragile = new HashSet<int>(); var heavy = new HashSet<int>(); var vip = new HashSet<int>();
        for (int i = 0; i < cfg.fragile && p < total; i++) fragile.Add(idx[p++]);
        for (int i = 0; i < cfg.heavy && p < total; i++) heavy.Add(idx[p++]);
        for (int i = 0; i < cfg.vip && p < total; i++) vip.Add(idx[p++]);

        var result = new List<CargoItem>();
        var used = new List<Vector3>();
        int n = 0;
        foreach (var load in cfg.loads)
        {
            var pool = new List<ShelfSlot>();
            foreach (var s in ShelfSlot.All) if (s.sectionName == load.section && !s.IsOccupied) pool.Add(s);
            Shuffle(pool);

            var info = SectionInfo.Get(load.section);
            for (int k = 0; k < load.count && k < pool.Count; k++, n++)
            {
                Vector3 pos = FindFreeSpot(used);
                used.Add(pos);
                var go = Instantiate(template, pos, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
                var shape = info != null && info.shapes.Length > 0 ? info.shapes[k % info.shapes.Length] : ProductShape.Generic;
                go.name = "Cargo_" + shape;
                int cseed = 7 * n + RunState.Level;
                Vector3 nat = ModelCatalog.MeasureSize("carton", cseed);
                Vector3 dims = Vector3.one * 0.5f;
                if (nat.x > 0.0001f)
                {
                    float f = 0.8f / Mathf.Max(nat.x, Mathf.Max(nat.y, nat.z));
                    dims = new Vector3(Mathf.Max(0.3f, nat.x * f), Mathf.Max(0.3f, nat.y * f), Mathf.Max(0.3f, nat.z * f));
                }
                go.transform.localScale = dims;
                go.SetActive(true);
                var cv = go.GetComponent<CargoVisual>();
                if (cv == null) cv = go.AddComponent<CargoVisual>();
                cv.cartonSeed = cseed;

                var cargo = go.GetComponent<CargoItem>();
                cargo.targetSlotId = pool[k].slotId;
                cargo.sectionName = load.section;
                cargo.cargoType = CargoType.Standard;
                cargo.isVip = false;
                cargo.baseScore = 1;
                cargo.vipScoreMultiplier = 1;
                if (fragile.Contains(n)) { cargo.cargoType = CargoType.Fragile; cargo.baseScore = 2; }
                if (heavy.Contains(n)) { cargo.cargoType = CargoType.Heavy; cargo.baseScore = 2; }
                if (vip.Contains(n)) { cargo.isVip = true; cargo.vipScoreMultiplier = 3; }
                result.Add(cargo);
            }
        }
        return result;
    }

    private Vector3 FindFreeSpot(List<Vector3> used)
    {
        float grow = 2f;
        for (int attempt = 0; attempt < 120; attempt++)
        {
            if (attempt == 40) grow = 4.5f;
            float x = Mathf.Lerp(stock.min.x - grow, stock.max.x + grow, (float)rng.NextDouble());
            float z = Mathf.Lerp(stock.min.z - grow, stock.max.z + grow, (float)rng.NextDouble());
            var pos = new Vector3(x, cargoY, z);

            bool ok = true;
            foreach (var u in used) if ((u - pos).sqrMagnitude < 2.0f * 2.0f) { ok = false; break; }
            if (!ok) continue;
            if (Physics.CheckBox(pos, new Vector3(0.6f, 0.5f, 0.6f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
            return pos;
        }
        return new Vector3(stock.center.x + used.Count * 0.5f, cargoY + 1.5f * used.Count, stock.center.z);
    }

    private void SpawnObstacles(LevelConfig cfg)
    {
        if (cfg.obstacles <= 0) return;
        obstaclesParent = new GameObject("Obstacles").transform;
        Vector3 start = drv != null ? drv.transform.position : stock.center;
        float y = stock.center.y;
        var keepOut = stock; keepOut.Expand(3f);
        int spawned = 0;

        for (int attempt = 0; attempt < 300 && spawned < cfg.obstacles; attempt++)
        {
            float x = Mathf.Lerp(region.min.x + 3f, region.max.x - 3f, (float)rng.NextDouble());
            float z = Mathf.Lerp(region.min.z + 3f, region.max.z - 3f, (float)rng.NextDouble());
            bool alongX = rng.NextDouble() < 0.5;
            Vector3 dir = alongX ? Vector3.right : Vector3.forward;
            Vector3 a = new Vector3(x, y, z);
            Vector3 b = a + dir * 8f;

            if ((a - start).magnitude < 6f || (b - start).magnitude < 6f) continue;

            bool ok = true;
            for (int s = 0; s <= 8 && ok; s++)
            {
                Vector3 pt = a + dir * s;
                if (Physics.CheckSphere(pt + Vector3.up * 0.9f, 0.9f, ~0, QueryTriggerInteraction.Ignore)) ok = false;
                if (keepOut.Contains(new Vector3(pt.x, keepOut.center.y, pt.z))) ok = false;
            }
            if (!ok) continue;

            PatrolObstacle.Spawn(spawned % 2 == 1, a, b, 1.6f + (float)rng.NextDouble(), obstaclesParent);
            spawned++;
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
    }
}
