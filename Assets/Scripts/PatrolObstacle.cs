using UnityEngine;

public class PatrolObstacle : MonoBehaviour
{
    private Vector3 a, b;
    private float speed = 1.5f;
    private float pauseTimer;
    private bool toB = true;
    private Rigidbody rb;
    private float lastHit = -10f;

    public static PatrolObstacle Spawn(bool isCart, Vector3 from, Vector3 to, float speed, Transform parent)
    {
        var root = new GameObject(isCart ? "Obstacle_Cart" : "Obstacle_Customer");
        root.transform.SetParent(parent, false);
        root.transform.position = from;

        var trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;

        if (isCart)
        {
            Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0.55f, 0), new Vector3(0.9f, 0.5f, 0.6f), new Color(0.75f, 0.78f, 0.82f));
            Prim(root.transform, PrimitiveType.Cube, new Vector3(-0.4f, 1.0f, 0), new Vector3(0.08f, 0.5f, 0.6f), new Color(0.2f, 0.2f, 0.25f));
            Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0.45f, 0), new Vector3(0.8f, 0.2f, 0.5f), new Color(1f, 0.45f, 0.1f));
            trigger.center = new Vector3(0f, 0.7f, 0f);
            trigger.size = new Vector3(1.6f, 1.4f, 1.2f);
        }
        else
        {
            Color shirt = Color.HSVToRGB(Random.value, 0.65f, 0.9f);
            Prim(root.transform, PrimitiveType.Capsule, new Vector3(0, 0.9f, 0), new Vector3(0.6f, 0.9f, 0.6f), shirt);
            Prim(root.transform, PrimitiveType.Sphere, new Vector3(0, 1.95f, 0), new Vector3(0.4f, 0.4f, 0.4f), new Color(0.95f, 0.75f, 0.6f));
            trigger.center = new Vector3(0f, 1.0f, 0f);
            trigger.size = new Vector3(1.3f, 2.2f, 1.3f);
        }

        var body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        var po = root.AddComponent<PatrolObstacle>();
        po.a = from; po.b = to; po.speed = speed;
        return po;
    }

    private static void Prim(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Color c)
    {
        var go = GameObject.CreatePrimitive(t);
        DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = VisualKit.Mat(c);
        OutlineUtil.Apply(r);
    }

    private void Awake() { rb = GetComponent<Rigidbody>(); }

    private void FixedUpdate()
    {
        if (pauseTimer > 0f) { pauseTimer -= Time.fixedDeltaTime; return; }

        var gm = GameManager.Instance;
        if (gm != null && gm.Phase != GamePhase.Playing) return; 

        Vector3 target = toB ? this.b : this.a;
        Vector3 pos = rb.position;
        Vector3 next = Vector3.MoveTowards(pos, target, speed * Time.fixedDeltaTime);
        Vector3 dir = target - pos; dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) rb.MoveRotation(Quaternion.LookRotation(dir));
        rb.MovePosition(next);

        if ((next - target).sqrMagnitude < 0.0004f) { toB = !toB; pauseTimer = 0.6f; }
    }

    private void OnTriggerEnter(Collider other) { TryHit(other); }
    private void OnTriggerStay(Collider other) { TryHit(other); }

    private void TryHit(Collider other)
    {
        if (Time.time - lastHit < 1.5f) return;
        var body = other.attachedRigidbody;
        if (body == null || body.GetComponent<DriverController>() == null) return;
        lastHit = Time.time;
        pauseTimer = 1.0f;  

        var gm = GameManager.Instance;
        if (gm != null) gm.OnObstacleHit();
        Feedback.PlaySfx(Sfx.Hit);
        Feedback.Shake(0.8f);
        Feedback.SpawnBurst(other.ClosestPoint(transform.position), FxKind.Sparks);

        Vector3 away = body.position - transform.position;
        away.y = 0f;
        if (away.sqrMagnitude > 0.0001f) body.AddForce(away.normalized * 5f, ForceMode.VelocityChange);
    }
}
