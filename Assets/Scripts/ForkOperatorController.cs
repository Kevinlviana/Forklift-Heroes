using UnityEngine;

public class ForkOperatorController : MonoBehaviour
{
    [SerializeField] private Transform forkLiftMount;
    [SerializeField] private Transform forkTiltPivot;
    [SerializeField] private float liftSpeed = 1.5f;
    [SerializeField] private float minHeight = 0f;
    [SerializeField] private float maxHeight = 2.5f;
    [SerializeField] private float tiltSpeed = 25f;
    [SerializeField] private float minTiltAngle = -10f;
    [SerializeField] private float maxTiltAngle = 15f;
    [SerializeField] private Transform carriedLoadSocket;
    [SerializeField] private DriverController driverController;

    private Vector2 forkInput;

    private CargoItem currentCargo;

    private float previousTilt;
    private float currentHeight;
    private float currentTilt;

    private bool inputEnabled;

    public CargoItem CurrentCargo => currentCargo;
    public Transform ForkMount => forkLiftMount;


    public float MaxCargoHeightWorld
    {
        get
        {
            float scaleY = forkLiftMount != null && forkLiftMount.parent != null ? forkLiftMount.parent.lossyScale.y : 1f;
            float baseY = carriedLoadSocket != null ? carriedLoadSocket.position.y : transform.position.y;
            return baseY + (maxHeight - currentHeight) * scaleY;
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    public void SetForkInput(Vector2 input)
    {
        forkInput = input;
    }

    private void Update()
    {
        if (!inputEnabled) return;

        currentHeight = Mathf.Clamp(currentHeight + forkInput.y * liftSpeed * Time.deltaTime, minHeight, maxHeight);
        currentTilt = Mathf.Clamp(currentTilt + forkInput.x * tiltSpeed * Time.deltaTime, minTiltAngle, maxTiltAngle);

        ApplyForkTransform();

        if (currentCargo != null)
        {
            float tiltSpeedThisFrame = Mathf.Abs(currentTilt - previousTilt) / Mathf.Max(Time.deltaTime, 0.0001f);
            currentCargo.EvaluateTiltSpeed(tiltSpeedThisFrame);
        }
        previousTilt = currentTilt;
    }

    private void ApplyForkTransform()
    {
        Vector3 mountPos = forkLiftMount.localPosition;
        mountPos.y = currentHeight;
        forkLiftMount.localPosition = mountPos;

        forkTiltPivot.localRotation = Quaternion.Euler(currentTilt, 0f, 0f);
    }

    public bool TryPickUpCargo(CargoItem cargo)
    {
        if (currentCargo != null || cargo == null) return false;
        if (cargo.IsCarried || cargo.Locked) return false;

        currentCargo = cargo;
        cargo.AttachTo(carriedLoadSocket, this);

        float speedMultiplier = cargo.cargoType == CargoType.Heavy
            ? cargo.heavyMoveSpeedMultiplier
            : 1f;
        if (driverController != null) driverController.SetCarriedCargoSpeedMultiplier(speedMultiplier);

        return true;
    }


    public void NotifyCargoReleased(CargoItem cargo)
    {
        if (cargo != currentCargo) return;
        currentCargo = null;
        if (driverController != null) driverController.SetCarriedCargoSpeedMultiplier(1f);
    }

    public void ApplyImpactToCargo(float impactSpeed)
    {
        if (currentCargo != null) currentCargo.EvaluateImpact(impactSpeed);
    }
}
