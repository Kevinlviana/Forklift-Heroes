using UnityEngine;

public class DriverController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private float brakeDeceleration = 15f;
    [SerializeField] private float operatorModeSpeedMultiplier = 0.15f;
    [SerializeField] private float tractionMultiplier = 1f;
    [SerializeField] private float rushSpeedMultiplier = 1.5f;
    [SerializeField] private float rushSkidRiskPerSecond = 0.15f;
    
    private bool engineStalled = false; 
    private bool rushHeld;

    private float carriedCargoSpeedMultiplier = 1f;

    public float OperatorModeSpeedMultiplier => operatorModeSpeedMultiplier;

    public void SetRushHeld(bool held) => rushHeld = held;
    public void SetCarriedCargoSpeedMultiplier(float multiplier) => carriedCargoSpeedMultiplier = Mathf.Clamp01(multiplier);

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isBraking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.3f, 0f); 
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    public void SetBraking(bool braking)
    {
        isBraking = braking;
    }

    public void SetTractionMultiplier(float multiplier)
    {
        tractionMultiplier = Mathf.Clamp01(multiplier);
    }

    public void SetEngineStalled(bool stalled)
    {
        engineStalled = stalled;
    }

    private void FixedUpdate()
    {
        if (engineStalled)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
            return;
        }

        if (isBraking)
        {
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, brakeDeceleration * Time.fixedDeltaTime);
            return;
        }

        float turn = moveInput.x * turnSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);

        float speedThisFrame = moveSpeed * tractionMultiplier * carriedCargoSpeedMultiplier;
        float effectiveTraction = tractionMultiplier;

        if (rushHeld)
        {
            speedThisFrame *= rushSpeedMultiplier;
            float skidPenalty = Mathf.Abs(moveInput.x) * rushSkidRiskPerSecond * Time.fixedDeltaTime;
            effectiveTraction = Mathf.Max(0.2f, tractionMultiplier - skidPenalty);
        }

        Vector3 forward = transform.forward * moveInput.y * speedThisFrame * (effectiveTraction / Mathf.Max(tractionMultiplier, 0.0001f));
        Vector3 targetVelocity = new Vector3(forward.x, rb.linearVelocity.y, forward.z);
        rb.linearVelocity = targetVelocity;
    }
}
