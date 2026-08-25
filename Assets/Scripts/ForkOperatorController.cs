using UnityEngine;
using static CargoItem;

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
    private CargoItem currentCargo;
    private float previousTilt;

    private bool inputEnabled;
    private Vector2 forkInput; 

    private float currentHeight;
    private float currentTilt;

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

        currentCargo = cargo;
        cargo.AttachTo(carriedLoadSocket);

        float speedMultiplier = cargo.cargoType == CargoType.Heavy
            ? cargo.heavyMoveSpeedMultiplier
            : 1f;
        driverController?.SetCarriedCargoSpeedMultiplier(speedMultiplier);

        return true;
    }

    public bool TryDropCargo(DeliveryZone zone)
    {
        if (currentCargo == null) return false;

        bool success = zone != null && zone.AcceptDelivery(currentCargo);
        if (success)
        {
            currentCargo = null;
            driverController?.SetCarriedCargoSpeedMultiplier(1f); 
        }
        return success;
    }
}
