using UnityEngine;

public class CargoPickupDetector : MonoBehaviour
{
    [SerializeField] private ForkOperatorController forkOperator;

    private void OnTriggerEnter(Collider other)
    {
        CargoItem cargo = other.GetComponent<CargoItem>();
        if (cargo == null) return;

        bool success = forkOperator.TryPickUpCargo(cargo);

        if (success)
        {
            Debug.Log($"Carga '{cargo.name}' pega com sucesso.");

            if (ShelfManager.Instance != null)
            {
                ShelfManager.Instance.HighlightTargetFor(cargo);
            }
        }
    }
}
