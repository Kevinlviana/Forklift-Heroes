using UnityEngine;

public enum CargoType
{
    Standard,
    Fragile,
    Heavy,
    Stacked
}


public class CargoItem : MonoBehaviour
{
    [SerializeField] private Color brokenColor = new Color(0.35f, 0.05f, 0.05f);

    public CargoType cargoType = CargoType.Standard;
    public bool IsBroken { get; private set; }
    public bool IsCarried { get; private set; }
    public bool IsPlaced { get; private set; }
    public bool Locked { get; set; }
    public ShelfSlot PlacedSlot { get; set; }
  
    
    public int AwardedPoints { get; set; }
    public bool isVip;

    public int baseScore = 10;
    public int vipScoreMultiplier = 1;

    public float fragileTiltSpeedLimit = 20f;
    public float heavyMoveSpeedMultiplier = 0.6f;
    public float fragileImpactLimit = 4.5f;
    

    public string sectionName;
    public string targetSlotId;

    private Rigidbody rb;
    private Collider[] colliders;
    private Renderer[] renderers;
    private ForkOperatorController carrier;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void EvaluateTiltSpeed(float currentTiltSpeed)
    {
        if (cargoType != CargoType.Fragile || IsBroken) return;

        if (Mathf.Abs(currentTiltSpeed) > fragileTiltSpeedLimit)
        {
            Break();
        }
    }

    public void EvaluateImpact(float impactSpeed)
    {
        if (cargoType != CargoType.Fragile || IsBroken || !IsCarried) return;

        if (impactSpeed > fragileImpactLimit)
        {
            Break();
        }
    }

    private void Break()
    {
        if (IsBroken) return;
        IsBroken = true;

        foreach (var r in renderers)
        {
            if (r != null) r.material.color = brokenColor;
        }

        Feedback.PlaySfx(Sfx.Glass);
        Feedback.SpawnBurst(transform.position, FxKind.Sparks);
        Feedback.Shake(0.5f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCargoBroken(this);
        }
    }

    private float lastThump = -1f;
    private void OnCollisionEnter(Collision c)
    {
        if (IsCarried || IsPlaced) return;
        if (Time.timeSinceLevelLoad < 1.5f) return;  

        float speed = c.relativeVelocity.magnitude;
        if (speed > 2.5f && Time.time - lastThump > 0.3f)
        {
            lastThump = Time.time;
            Feedback.PlaySfx(Sfx.Thump);
        }
        if (cargoType == CargoType.Fragile && !IsBroken && speed > fragileImpactLimit) Break();
    }

    public void AttachTo(Transform socket, ForkOperatorController owner = null)
    {
        if (IsPlaced)
        {
            IsPlaced = false;
            PlacementLog.Add("Removed from shelf: " + name);
            if (PlacedSlot != null) { PlacedSlot.Vacate(); PlacedSlot = null; }
            if (GameManager.Instance != null) GameManager.Instance.UnregisterPlacement(this);
        }

        carrier = owner;
        IsCarried = true;
        SetIgnoreCarrier(true);

        rb.isKinematic = true;
        transform.SetParent(socket);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void PlaceAt(Transform point, ShelfSlot slot = null)
    {
        SetIgnoreCarrier(false);
        if (carrier != null) carrier.NotifyCargoReleased(this);
        carrier = null;

        IsCarried = false;
        IsPlaced = true;
        PlacedSlot = slot;

        transform.SetParent(null, true);
        transform.SetPositionAndRotation(point.position, point.rotation);
        rb.isKinematic = true;
    }

    public void PlaceAt(Vector3 position, Quaternion rotation, ShelfSlot slot)
    {
        SetIgnoreCarrier(false);
        if (carrier != null) carrier.NotifyCargoReleased(this);
        carrier = null;

        IsCarried = false;
        IsPlaced = true;
        PlacedSlot = slot;

        transform.SetParent(null, true);
        transform.SetPositionAndRotation(position, rotation);
        rb.isKinematic = true;
    }

    public void Release()
    {
        SetIgnoreCarrier(false);
        if (carrier != null) carrier.NotifyCargoReleased(this);
        carrier = null;

        IsCarried = false;
        transform.SetParent(null);
        rb.isKinematic = false;
    }

    public int GetScoreValue()
    {
        if (IsBroken) return 0;
        return isVip ? baseScore * vipScoreMultiplier : baseScore;
    }

    private void SetIgnoreCarrier(bool ignore)
    {
        if (carrier == null) return;
        var theirs = carrier.GetComponentsInChildren<Collider>();
        foreach (var mine in colliders)
        {
            foreach (var other in theirs)
            {
                if (mine != null && other != null && mine != other)
                    Physics.IgnoreCollision(mine, other, ignore);
            }
        }
    }
}
