using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ForkliftSkin : MonoBehaviour
{
    private class Wheel { public Transform t; public Renderer r; }

    private Transform forklift;
    private Rigidbody rb;
    private Transform mount;
    private GameObject model;
    private Transform lift;
    private Vector3 liftBaseLocal;
    private float mountBaseY;
    private readonly List<Renderer> originals = new List<Renderer>();
    private readonly List<Wheel> wheels = new List<Wheel>();
    private Bounds origBounds;
    private bool skinOn = true;
    private bool flipped;

    private void Start()
    {
        var drv = FindFirstObjectByType<DriverController>();
        if (drv == null || !ModelCatalog.Has("forklift")) { enabled = false; return; }

        forklift = drv.transform;
        rb = drv.GetComponent<Rigidbody>();
        var fork = drv.GetComponent<ForkOperatorController>();
        mount = fork != null ? fork.ForkMount : null;

        foreach (var r in forklift.GetComponentsInChildren<MeshRenderer>())
            if (r.GetComponent<TMPro.TMP_Text>() == null) originals.Add(r);
        if (originals.Count == 0 || !ModelCatalog.LocalBounds(forklift, originals, out origBounds)) { enabled = false; return; }

        model = ModelCatalog.Spawn("forklift", 0, forklift);
        if (model == null) { enabled = false; return; }
        model.name = "ForkliftModel";
        Fit();
        Apply();
    }

    private static string Low(string s) { return s.ToLowerInvariant(); }

    private void Fit()
    {
        var rs = model.GetComponentsInChildren<Renderer>();
        model.transform.localScale = Vector3.one;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localPosition = Vector3.zero;

        lift = null;
        wheels.Clear();
        foreach (var t in model.GetComponentsInChildren<Transform>())
        {
            string n = Low(t.name);
            if (n == "lift") lift = t;
            else if (n.StartsWith("wheel"))
            {
                var wr = t.GetComponent<Renderer>();
                if (wr != null) wheels.Add(new Wheel { t = t, r = wr });
            }
        }

        Bounds b;
        if (!ModelCatalog.LocalBounds(forklift, rs, out b)) return;

        bool origLongZ = origBounds.size.z >= origBounds.size.x;
        bool modelLongZ = b.size.z >= b.size.x;
        float yaw = (origLongZ == modelLongZ ? 0f : 90f) + (flipped ? 180f : 0f);
        model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        ModelCatalog.LocalBounds(forklift, rs, out b);

        float modelLen = Mathf.Max(b.size.x, b.size.z);
        float origLen = Mathf.Max(origBounds.size.x, origBounds.size.z);
        if (modelLen > 0.0001f) model.transform.localScale = Vector3.one * (origLen / modelLen);

        ModelCatalog.LocalBounds(forklift, rs, out b);
        model.transform.localPosition += new Vector3(
            origBounds.center.x - b.center.x,
            origBounds.min.y - b.min.y,
            origBounds.center.z - b.center.z);

        if (lift != null) liftBaseLocal = lift.localPosition;
        mountBaseY = mount != null ? mount.position.y : 0f;
    }

    private void Apply()
    {
        if (model != null) model.SetActive(skinOn);
        foreach (var r in originals) if (r != null) r.enabled = !skinOn;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || model == null) return;
        if (kb.kKey.wasPressedThisFrame) { skinOn = !skinOn; Apply(); }
        if (kb.jKey.wasPressedThisFrame) { flipped = !flipped; Fit(); }
    }

    private void LateUpdate()
    {
        if (model == null || !skinOn) return;

        if (lift != null && mount != null)
        {
            mountBaseY = Mathf.Min(mountBaseY, mount.position.y);  
            float dy = mount.position.y - mountBaseY;
            float sy = Mathf.Max(0.0001f, model.transform.lossyScale.y);
            lift.localPosition = liftBaseLocal + new Vector3(0f, dy / sy, 0f);
        }

        if (rb != null && wheels.Count > 0)
        {
            float fwd = Vector3.Dot(rb.linearVelocity, forklift.forward);
            foreach (var w in wheels)
            {
                float radius = Mathf.Max(0.05f, w.r.bounds.size.y * 0.5f);
                float deg = fwd * Time.deltaTime / radius * Mathf.Rad2Deg;
                w.t.RotateAround(w.r.bounds.center, forklift.right, deg);
            }
        }
    }
}
