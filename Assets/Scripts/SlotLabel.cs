using TMPro;
using UnityEngine;

public class SlotLabel : MonoBehaviour
{
    [SerializeField] private TextMeshPro tmp;
    private bool lit;

    public void Init(ShelfSlot slot)
    {
        var go = new GameObject("SlotLabel");
        go.transform.SetParent(transform, false);

        go.transform.position = transform.position + Vector3.up * 0.3f;

        tmp = go.AddComponent<TextMeshPro>();
        tmp.text = slot.slotId;
        tmp.fontSize = 3.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 1f, 0.85f);
        tmp.outlineWidth = 0.3f;
        tmp.outlineColor = new Color32(0, 0, 0, 255);
        tmp.rectTransform.sizeDelta = new Vector2(4f, 1.5f);
        go.AddComponent<Billboard>();
        Billboard.FaceNow(go.transform);
    }

    public void SetHighlighted(bool on)
    {
        lit = on;
        if (tmp != null) tmp.color = on ? new Color(1f, 0.9f, 0.1f) : new Color(1f, 1f, 1f, 0.85f);
    }

    private void Update()
    {
        if (tmp == null) return;
        float s = lit ? 1.25f + 0.2f * Mathf.Sin(Time.time * 8f) : 1f;
        tmp.transform.localScale = Vector3.one * s;
    }
}
