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
    private float switchLockTimer;

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
        if (switchLockTimer > 0f) return; 

        CurrentRole = CurrentRole == PlayerRole.Driver
            ? PlayerRole.ForkOperator
            : PlayerRole.Driver;

        switchLockTimer = roleSwitchDelay;
        UpdateActiveController();
        gameManager?.OnRoleChanged(CurrentRole);
    }

    private void Update()
    {
        if (switchLockTimer > 0f)
        {
            switchLockTimer -= Time.deltaTime;
        }
    }

   
    private bool InputAllowed => switchLockTimer <= 0f;

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if (!InputAllowed) { driverController.SetMoveInput(Vector2.zero); return; }

        if (CurrentRole == PlayerRole.Driver)
        {
            driverController.SetMoveInput(moveInput);
        }
        else
        {
            driverController.SetMoveInput(moveInput * driverController.OperatorModeSpeedMultiplier);
        }
    }

    public void OnForkControl(InputAction.CallbackContext context)
    {
        forkInput = context.ReadValue<Vector2>();
        if (!InputAllowed) return;

        if (CurrentRole == PlayerRole.ForkOperator)
        {
            forkOperatorController.SetForkInput(forkInput);
        }
    }

    public void OnBrake(InputAction.CallbackContext context)
    {
        if (CurrentRole != PlayerRole.Driver) return;
        driverController.SetBraking(context.performed);
    }

    public void OnRush(InputAction.CallbackContext context)
    {
        bool held = context.performed;
        driverController.SetRushHeld(held && CurrentRole == PlayerRole.Driver);
    }

    private void UpdateActiveController()
    {
 
        forkOperatorController.SetInputEnabled(CurrentRole == PlayerRole.ForkOperator);
    }
}
