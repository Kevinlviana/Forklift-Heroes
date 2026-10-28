using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ShelfManager : MonoBehaviour
{
    [SerializeField] private List<ShelfSlot> allSlots;

    public UnityEvent<string, string> OnTargetHighlighted;
    public UnityEvent OnTargetCleared;

    public static ShelfManager Instance { get; private set; }

    private ShelfSlot currentHighlighted;

    private void Awake()
    {
        Instance = this;
    }

    public void HighlightTargetFor(CargoItem cargo)
    {
        ClearHighlight();

        foreach (var slot in ShelfSlot.All)
        {
            if (slot.slotId == cargo.targetSlotId)
            {
                slot.SetHighlighted(true);
                currentHighlighted = slot;
                OnTargetHighlighted?.Invoke(slot.sectionName, slot.slotId);
                return;
            }
        }
    }

    public void ClearHighlight()
    {
        if (currentHighlighted != null)
        {
            currentHighlighted.SetHighlighted(false);
            currentHighlighted = null;
            OnTargetCleared?.Invoke();
        }
    }
}
