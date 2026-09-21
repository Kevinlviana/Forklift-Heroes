using UnityEngine;

public class ShelfSlot : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Renderer slotRenderer;
    [SerializeField] private Color highlightColor = Color.green;

    public string sectionName;
    public string slotId;

    private Color originalColor;

    private void Awake()
    {
        if (slotRenderer != null)
            originalColor = slotRenderer.material.color;
    }

    public void SetHighlighted(bool on)
    {
        if (slotRenderer == null) return;
        slotRenderer.material.color = on ? highlightColor : originalColor;
    }

    public bool AcceptDelivery(CargoItem cargo)
    {
        if (cargo == null) return false;

        bool isCorrectSlot = cargo.targetSlotId == slotId;

        if (isCorrectSlot)
        {
            cargo.Release();
            gameManager.AddScore(cargo.GetScoreValue());
            gameManager.OnItemPlacedCorrectly();
            Destroy(cargo.gameObject, 0.1f);
        }
        else
        {
            gameManager.OnItemPlacedWrong();
        }

        return isCorrectSlot;
    }
}
