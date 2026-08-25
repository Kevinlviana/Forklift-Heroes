using UnityEngine;

public class DeliveryZone : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    public bool AcceptDelivery(CargoItem cargo)
    {
        if (cargo == null) return false;

        cargo.Release();
        gameManager.AddScore(cargo.GetScoreValue());
        Destroy(cargo.gameObject, 0.1f);
        return true;
    }
}
