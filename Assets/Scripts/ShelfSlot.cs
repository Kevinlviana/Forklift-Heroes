using System.Collections.Generic;
using UnityEngine;

public class ShelfSlot : MonoBehaviour
{
    public static readonly List<ShelfSlot> All = new List<ShelfSlot>();

    [SerializeField] private GameManager gameManager;
    [SerializeField] private Renderer slotRenderer;
    [SerializeField] private Color highlightColor = Color.green;
    [SerializeField] private Color wrongColor = Color.red;
    [SerializeField] private Transform placePoint;
    [SerializeField] private float wrongFlashDuration = 0.5f;

    public string sectionName;
    public string slotId;

    public bool IsOccupied { get; private set; }

    private Color originalColor;
    private bool isHighlighted;
    private float flashTimer;
    private SlotLabel label;

    private GameManager GM => gameManager != null ? gameManager : GameManager.Instance;

    private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    private void Awake()
    {
        if (slotRenderer != null)
            originalColor = slotRenderer.material.color;
        label = GetComponent<SlotLabel>();
    }

    private void Update()
    {
        if (flashTimer <= 0f) return;
        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f) ApplyColor(isHighlighted ? highlightColor : originalColor);
    }

    public void SetRenderer(Renderer r)
    {
        slotRenderer = r;
        if (r != null && r.sharedMaterial != null) originalColor = r.sharedMaterial.color;
    }

    public void SetHighlighted(bool on)
    {
        isHighlighted = on;
        if (label == null) label = GetComponent<SlotLabel>();
        if (label != null) label.SetHighlighted(on);
        if (flashTimer > 0f) return;
        ApplyColor(on ? highlightColor : originalColor);
    }

    private void ApplyColor(Color c)
    {
        if (slotRenderer != null) slotRenderer.material.color = c;
    }

    public void Vacate()
    {
        IsOccupied = false;
    }

    public bool AcceptDelivery(CargoItem cargo)
    {
        if (cargo == null || cargo.IsPlaced) return false;

        var gm = GM;
        bool isCorrectSlot = !IsOccupied && cargo.targetSlotId == slotId;

        if (isCorrectSlot)
        {
            IsOccupied = true;
            Vector3 pos = cargo.transform.position;
            var col = GetComponent<Collider>();
            float boardTop = col != null ? col.bounds.center.y : transform.position.y;
            var bc = cargo.GetComponent<BoxCollider>();
            float halfH = bc != null ? bc.size.y * Mathf.Abs(cargo.transform.lossyScale.y) * 0.5f : 0.25f;
            pos.y = boardTop + halfH;
            float yaw = Mathf.Round(cargo.transform.eulerAngles.y / 90f) * 90f;
            cargo.PlaceAt(pos, Quaternion.Euler(0f, yaw, 0f), this);
            PlacementLog.Add("OK: " + cargo.name + " em " + slotId);
            Feedback.PlaySfx(Sfx.Place);
            Feedback.PlaySfx(Sfx.Chime);
            Feedback.SpawnBurst(cargo.transform.position, FxKind.Sparkle);
            if (gm != null) gm.RegisterPlacement(cargo);
        }
        else
        {
            PlacementLog.Add("ERROR: " + cargo.name + " em " + slotId + " (target " + cargo.targetSlotId + (IsOccupied ? ", occupied" : "") + ")");
            flashTimer = wrongFlashDuration;
            ApplyColor(wrongColor);
            Feedback.PlaySfx(Sfx.Error);
            Feedback.Shake(0.3f);
            if (gm != null) gm.OnItemPlacedWrong();
        }

        return isCorrectSlot;
    }
}
