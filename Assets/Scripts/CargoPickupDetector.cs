using System.Collections.Generic;
using UnityEngine;

public class CargoPickupDetector : MonoBehaviour
{
    [SerializeField] private ForkOperatorController forkOperator;
    [SerializeField] private bool autoPickup = false;

    private BoxCollider box;

    public ForkOperatorController Fork => forkOperator;

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
    }

    public void Query(List<CargoItem> results)
    {
        results.Clear();
        if (box == null) return;

        Vector3 center = transform.TransformPoint(box.center);
        Vector3 s = transform.lossyScale;
        Vector3 half = new Vector3(box.size.x * Mathf.Abs(s.x), box.size.y * Mathf.Abs(s.y), box.size.z * Mathf.Abs(s.z)) * 0.5f;

        var hits = Physics.OverlapBox(center, half, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            var c = h.GetComponentInParent<CargoItem>();
            if (c != null && !c.IsCarried && !c.Locked && !results.Contains(c)) results.Add(c);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!autoPickup || forkOperator == null) return;
        var cargo = other.GetComponent<CargoItem>();
        if (cargo == null || cargo.IsPlaced) return;
        if (forkOperator.TryPickUpCargo(cargo) && ShelfManager.Instance != null)
            ShelfManager.Instance.HighlightTargetFor(cargo);
    }
}
