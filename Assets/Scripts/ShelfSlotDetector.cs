using UnityEngine;

public class ShelfSlotDetector : MonoBehaviour
{
    private ShelfSlot shelfSlot;

    private void Awake()
    {
        shelfSlot = GetComponent<ShelfSlot>();
    }

    private void OnTriggerEnter(Collider other)
    {
        CargoItem cargo = other.GetComponent<CargoItem>();
        if (cargo == null) return;

        bool success = shelfSlot.AcceptDelivery(cargo);

        Debug.Log(success
            ? $"Carga '{cargo.name}' entregue no slot certo."
            : $"Carga '{cargo.name}' entregue no slot ERRADO.");

        if (ShelfManager.Instance != null)
        {
            ShelfManager.Instance.ClearHighlight();
        }
    }
}
