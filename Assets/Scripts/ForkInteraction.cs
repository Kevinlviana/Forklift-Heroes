using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ForkInteraction : MonoBehaviour
{
    public static ForkInteraction Instance { get; private set; }
    public string HintText { get; private set; } = "";

    [SerializeField] private float maxYawError = 25f;
    [SerializeField] private float maxHeightError = 0.7f;
    [SerializeField] private float maxFloorDropHeight = 1.2f;
    [SerializeField] private float placeBelow = 0.35f;
    [SerializeField] private float placeAbove = 0.7f;

    private ForkOperatorController fork;
    private CargoPickupDetector detector;
    private readonly List<CargoItem> buffer = new List<CargoItem>();

    private void Awake() { Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Start()
    {
        fork = FindFirstObjectByType<ForkOperatorController>();
        detector = FindFirstObjectByType<CargoPickupDetector>();
    }

    private void Update()
    {
        HintText = "";
        var gm = GameManager.Instance;
        if (gm == null || gm.Phase != GamePhase.Playing || fork == null || detector == null) return;

        bool pressed = InteractPressed();
        if (fork.CurrentCargo == null) HandlePickup(pressed);
        else HandleCarry(pressed);
    }

    private static bool InteractPressed()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame) return true;
        var gp = Gamepad.current;
        return gp != null && gp.buttonSouth.wasPressedThisFrame;
    }

    private void HandlePickup(bool pressed)
    {
        detector.Query(buffer);
        if (buffer.Count == 0) return;

        Transform socket = detector.transform;
        float forkYaw = fork.transform.eulerAngles.y;

        CargoItem best = null;
        float bestDist = float.MaxValue;
        bool yawOk = true, heightOk = true;

        foreach (var c in buffer)
        {
            float yawErr = YawError(forkYaw, c.transform.eulerAngles.y);
            float dy = Mathf.Abs(c.transform.position.y - socket.position.y);
            bool yOk = yawErr <= maxYawError;
            bool hOk = dy <= maxHeightError;
            if (yOk && hOk)
            {
                float d = (c.transform.position - socket.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = c; }
            }
            else { if (!yOk) yawOk = false; if (!hOk) heightOk = false; }
        }

        if (best != null)
        {
            HintText = best.IsPlaced ? "[E] Pick up cargo from shelf" : "[E] Pick up cargo";
            if (pressed && fork.TryPickUpCargo(best))
            {
                Feedback.PlaySfx(Sfx.Clack);
                if (ShelfManager.Instance != null) ShelfManager.Instance.HighlightTargetFor(best);
            }
        }
        else
        {
            HintText = !yawOk ? "Line the forklift up with the cargo" : !heightOk ? "Adjust the fork height" : "";
        }
    }

    private void HandleCarry(bool pressed)
    {
        var cargo = fork.CurrentCargo;
        bool overShelf;
        ShelfSlot slot = FindSlot(cargo, out overShelf);

        if (slot != null)
        {
            bool right = !slot.IsOccupied && slot.slotId == cargo.targetSlotId;
            HintText = right ? "[E] Place on shelf" : "[E] Place in " + slot.slotId + " (wrong! target: " + cargo.targetSlotId + ")";
            if (pressed)
            {
                bool ok = slot.AcceptDelivery(cargo);
                if (ok && ShelfManager.Instance != null) ShelfManager.Instance.ClearHighlight();
            }
            return;
        }

        if (overShelf)
        {
            HintText = "Adjust the fork height to fit the shelf board (target: " + cargo.targetSlotId + ")";
            return;
        }

        float h = HeightAboveGround(cargo);
        if (h <= maxFloorDropHeight)
        {
            HintText = "[E] Drop on the floor";
            if (pressed)
            {
                Vector3 p = cargo.transform.position;
                PlacementLog.Add("Dropped on floor (does not count): " + cargo.name);
                cargo.Release();
                if (ShelfManager.Instance != null) ShelfManager.Instance.ClearHighlight();
                Feedback.PlaySfx(Sfx.Thump);
                Feedback.SpawnBurst(new Vector3(p.x, p.y - 0.4f, p.z), FxKind.Dust);
            }
        }
        else HintText = "Lower the forks to drop on the floor";
    }

    private static float YawError(float a, float b)
    {
        float d = Mathf.Abs(Mathf.DeltaAngle(a, b)) % 90f;
        return Mathf.Min(d, 90f - d);
    }

    private ShelfSlot FindSlot(CargoItem cargo, out bool overShelf)
    {
        overShelf = false;
        Vector3 c = cargo.transform.position;
        ShelfSlot best = null, target = null;
        float bestAbs = float.MaxValue;
        foreach (var slot in ShelfSlot.All)
        {
            var col = slot.GetComponent<Collider>();
            if (col == null) continue;
            Bounds b = col.bounds;
            if (c.x < b.min.x || c.x > b.max.x || c.z < b.min.z || c.z > b.max.z) continue;
            overShelf = true;

            float v = c.y - b.center.y;
            if (v < -placeBelow || v > placeAbove) continue;
            if (!slot.IsOccupied && slot.slotId == cargo.targetSlotId) target = slot;
            if (Mathf.Abs(v) < bestAbs) { bestAbs = Mathf.Abs(v); best = slot; }
        }
        return target != null ? target : best;
    }

    private float HeightAboveGround(CargoItem cargo)
    {
        var bc = cargo.GetComponent<BoxCollider>();
        float halfY = bc != null ? bc.size.y * Mathf.Abs(cargo.transform.lossyScale.y) * 0.5f : 0.5f;

        var hits = Physics.RaycastAll(cargo.transform.position, Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        foreach (var h in hits)
        {
            if (h.collider.GetComponentInParent<CargoItem>() == cargo) continue;
            if (h.collider.GetComponentInParent<DriverController>() != null) continue;
            if (h.distance < nearest) nearest = h.distance;
        }
        return nearest == float.MaxValue ? 99f : Mathf.Max(0f, nearest - halfY);
    }
}
