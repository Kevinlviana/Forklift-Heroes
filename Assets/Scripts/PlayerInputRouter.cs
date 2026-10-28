using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerRole
{
    Driver,
    ForkOperator
}

public class PlayerInputRouter : MonoBehaviour
{
    [SerializeField] private DriverController driverController;
    [SerializeField] private ForkOperatorController forkOperatorController;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerRole startingRole = PlayerRole.Driver;
    [SerializeField] private float roleSwitchDelay = 0.25f;

    public PlayerRole CurrentRole { get; private set; }

    private Vector2 moveInput;
    private Vector2 forkInput;
    private bool brakeHeld;
    private bool rushHeld;

    private float switchLockTimer;

    private GameManager GM => gameManager != null ? gameManager : GameManager.Instance;
    private bool RoundOpen => GM == null || GM.RoundActive;
    private bool InputAllowed => switchLockTimer <= 0f && RoundOpen;

    private void Awake()
    {
        CurrentRole = startingRole;
    }

    private void Start()
    {
        UpdateActiveController();
    }

    public void OnSwitchRole(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (switchLockTimer > 0f || !RoundOpen) return;

        CurrentRole = CurrentRole == PlayerRole.Driver
            ? PlayerRole.ForkOperator
            : PlayerRole.Driver;

        switchLockTimer = roleSwitchDelay;
        UpdateActiveController();
        if (gameManager != null) gameManager.OnRoleChanged(CurrentRole);
        else if (GameManager.Instance != null) GameManager.Instance.OnRoleChanged(CurrentRole);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnForkControl(InputAction.CallbackContext context)
    {
        forkInput = context.ReadValue<Vector2>();
    }

    public void OnBrake(InputAction.CallbackContext context)
    {
        if (context.performed) brakeHeld = true;
        else if (context.canceled) brakeHeld = false;
    }

    public void OnRush(InputAction.CallbackContext context)
    {
        if (context.performed) rushHeld = true;
        else if (context.canceled) rushHeld = false;
    }

    private void Update()
    {
        if (switchLockTimer > 0f)
        {
            switchLockTimer -= Time.deltaTime;
        }

        bool isDriver = CurrentRole == PlayerRole.Driver;

        if (driverController != null)
        {
            if (!InputAllowed)
                driverController.SetMoveInput(Vector2.zero);
            else
                driverController.SetMoveInput(isDriver
                    ? moveInput
                    : moveInput * driverController.OperatorModeSpeedMultiplier);

            driverController.SetBraking(!RoundOpen || (brakeHeld && isDriver));
            driverController.SetRushHeld(rushHeld && isDriver && RoundOpen);
        }

        if (forkOperatorController != null)
        {
            forkOperatorController.SetForkInput(
                InputAllowed && !isDriver ? forkInput : Vector2.zero);
        }
    }

    private void UpdateActiveController()
    {
        if (forkOperatorController != null)
            forkOperatorController.SetInputEnabled(CurrentRole == PlayerRole.ForkOperator);
    }
}
