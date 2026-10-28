using UnityEngine;

public class DriverController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private float brakeDeceleration = 15f;
    [SerializeField] private float operatorModeSpeedMultiplier = 0.15f;
    [SerializeField] private float tractionMultiplier = 1f;
    [SerializeField] private float rushSpeedMultiplier = 1.5f;
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float coastDeceleration = 12f;
    [SerializeField] private float rushGripMultiplier = 0.35f;
    [SerializeField] private float impactMinSpeed = 2.5f;

    private bool rushHeld;

    private float carriedCargoSpeedMultiplier = 1f;

    public float OperatorModeSpeedMultiplier => operatorModeSpeedMultiplier;

    public void SetRushHeld(bool held) => rushHeld = held;
    public void SetCarriedCargoSpeedMultiplier(float multiplier) => carriedCargoSpeedMultiplier = Mathf.Clamp01(multiplier);

    private Rigidbody rb;
    private ForkOperatorController forkOperator;
    private Vector2 moveInput;
    private bool isBraking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        forkOperator = GetComponent<ForkOperatorController>();
        rb.centerOfMass = new Vector3(0f, -0.3f, 0f);
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    public void SetBraking(bool braking)
    {
        isBraking = braking;
    }

    private float restY = float.NaN;

    private void FixedUpdate()
    {
        if (float.IsNaN(restY) && Time.timeSinceLevelLoad > 1f)
        {
            restY = rb.position.y;
            rb.constraints |= RigidbodyConstraints.FreezePositionY;  
        }
        if (!float.IsNaN(restY) && Mathf.Abs(rb.position.y - restY) > 0.02f)
        {
            Vector3 p = rb.position;
            rb.position = new Vector3(p.x, restY, p.z);
            Vector3 vv = rb.linearVelocity;
            rb.linearVelocity = new Vector3(vv.x, 0f, vv.z);
        }

        Vector3 v = rb.linearVelocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);

        if (isBraking)
        {
            horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, brakeDeceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
            return;
        }

        float steerDir = moveInput.y < -0.1f ? -1f : 1f;
        float turn = moveInput.x * steerDir * turnSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));

        float speed = moveSpeed * tractionMultiplier * carriedCargoSpeedMultiplier;
        if (rushHeld) speed *= rushSpeedMultiplier;

        Vector3 target = transform.forward * (moveInput.y * speed);

        bool hasInput = moveInput.sqrMagnitude > 0.0001f;
        float grip = hasInput
            ? acceleration * Mathf.Max(tractionMultiplier, 0.2f) * (rushHeld ? rushGripMultiplier : 1f)
            : coastDeceleration;

        horizontal = Vector3.MoveTowards(horizontal, target, grip * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
    }

    private float lastCrashFx = -1f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.GetComponentInParent<CargoItem>() != null) return;
        if (collision.collider.GetComponentInParent<PatrolObstacle>() != null) return;  
        if (collision.contactCount > 0 && Mathf.Abs(collision.GetContact(0).normal.y) > 0.7f) return; 

        float impact = collision.relativeVelocity.magnitude;

        if (impact >= 6f && Time.time - lastCrashFx > 0.4f)
        {
            lastCrashFx = Time.time;
            Feedback.PlaySfx(Sfx.Crash);
            Feedback.Shake(Mathf.Clamp(impact / 10f, 0.2f, 1.2f));
            if (collision.contactCount > 0) Feedback.SpawnBurst(collision.GetContact(0).point, FxKind.Sparks);
        }

        if (forkOperator != null && impact >= impactMinSpeed) forkOperator.ApplyImpactToCargo(impact);
    }
}
