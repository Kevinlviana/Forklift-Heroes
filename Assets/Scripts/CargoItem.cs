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
    public CargoType cargoType = CargoType.Standard;
    public bool IsBroken { get; private set; }
    public bool isVip;

    public int baseScore = 10;
    public int vipScoreMultiplier = 1;

    public float fragileTiltSpeedLimit = 20f;
    public float heavyMoveSpeedMultiplier = 0.6f;

    public string sectionName;
    public string targetSlotId;

    private Rigidbody rb;

   

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void EvaluateTiltSpeed(float currentTiltSpeed)
    {
        if (cargoType != CargoType.Fragile || IsBroken) return;

        if (Mathf.Abs(currentTiltSpeed) > fragileTiltSpeedLimit)
        {
            Break();
        }
    }

    private void Break()
    {
        IsBroken = true;
    }

    public void AttachTo(Transform socket)
    {
        rb.isKinematic = true;
        transform.SetParent(socket);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Release()
    {
        transform.SetParent(null);
        rb.isKinematic = false;
    }

    public int GetScoreValue()
    {
        if (IsBroken) return 0;
        return isVip ? baseScore * vipScoreMultiplier : baseScore;
    }
}
