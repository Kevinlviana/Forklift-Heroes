using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    public static OrbitCamera Instance { get; private set; }

    [SerializeField] private float distance = 18f;
    [SerializeField] private float minDistance = 6f;
    [SerializeField] private float maxDistance = 55f;
    [SerializeField] private float pitch = 40f;
    [SerializeField] private float minPitch = 10f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private float pivotHeight = 1.5f;
    [SerializeField] private float mouseSensitivity = 0.2f;
    [SerializeField] private float keyOrbitSpeed = 100f;
    [SerializeField] private float followSmooth = 8f;

    private Camera cam;
    private Transform target;
    private float yaw;
    private float currentDist;
    private Vector3 pivot;
    private bool chase;
    private float shake;

    public string ModeLabel => chase ? "Follow" : "Free";

    private void Awake() { Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void OnEnable() { Feedback.OnShake += Kick; }
    private void OnDisable() { Feedback.OnShake -= Kick; }

    private void Kick(float force) { shake = Mathf.Max(shake, force * 0.35f); }

    private void Start()
    {
        cam = Camera.main;
        var drv = FindFirstObjectByType<DriverController>();
        if (cam == null || drv == null) { enabled = false; return; }
        target = drv.transform;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.07f, 0.10f);

        foreach (var b in cam.GetComponents<Behaviour>())
            if (b.GetType().Name == "CinemachineBrain") b.enabled = false;

        yaw = target.eulerAngles.y;
        currentDist = distance;
        pivot = target.position + Vector3.up * pivotHeight;
        Apply(0f);
    }

    private void LateUpdate()
    {
        if (cam == null || target == null) return;
        float dt = Time.unscaledDeltaTime;
        bool manual = false;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                Vector2 d = mouse.delta.ReadValue();
                yaw += d.x * mouseSensitivity;
                pitch -= d.y * mouseSensitivity;
                manual = true;
            }
            float sc = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(sc) > 0.01f) distance *= sc > 0f ? 0.9f : 1.1f;
        }

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.zKey.isPressed) { yaw -= keyOrbitSpeed * dt; manual = true; }
            if (kb.xKey.isPressed) { yaw += keyOrbitSpeed * dt; manual = true; }
            if (kb.vKey.wasPressedThisFrame) chase = !chase;
        }

        var gp = Gamepad.current;
        if (gp != null)
        {
            if (gp.leftShoulder.isPressed) { yaw -= keyOrbitSpeed * dt; manual = true; }
            if (gp.rightShoulder.isPressed) { yaw += keyOrbitSpeed * dt; manual = true; }
            if (gp.dpad.up.isPressed) distance -= 15f * dt;
            if (gp.dpad.down.isPressed) distance += 15f * dt;
            if (gp.rightStickButton.wasPressedThisFrame) chase = !chase;
        }

        var gm = GameManager.Instance;
        if (gm != null && gm.Phase == GamePhase.Menu) yaw += 10f * dt;  
        else if (chase && !manual) yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, 3f * dt);

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        Vector3 wantedPivot = target.position + Vector3.up * pivotHeight;
        pivot = Vector3.Lerp(pivot, wantedPivot, 1f - Mathf.Exp(-followSmooth * dt));

        Apply(dt);
    }

    private void Apply(float dt)
    {
        Vector3 dir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;

        float allowed = distance;
        var hits = Physics.RaycastAll(pivot, dir, distance, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.collider.bounds.size.y < 6f) continue;
            allowed = Mathf.Min(allowed, h.distance - 0.6f);
        }
        allowed = Mathf.Max(allowed, 3f);

        if (allowed < currentDist) currentDist = allowed;
        else currentDist = Mathf.Lerp(currentDist, allowed, 1f - Mathf.Exp(-3f * Mathf.Max(dt, 0.0001f)));

        Vector3 pos = pivot + dir * currentDist;
        cam.transform.rotation = Quaternion.LookRotation(pivot - pos);

        if (shake > 0.001f)
        {
            pos += Random.insideUnitSphere * shake;
            shake *= Mathf.Exp(-7f * dt);
        }
        else shake = 0f;

        cam.transform.position = pos;
    }
}
