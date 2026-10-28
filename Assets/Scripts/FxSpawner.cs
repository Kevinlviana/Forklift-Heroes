using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FxSpawner : MonoBehaviour
{
    private readonly Dictionary<FxKind, ParticleSystem> systems = new Dictionary<FxKind, ParticleSystem>();

    private void Awake()
    {
        foreach (FxKind k in System.Enum.GetValues(typeof(FxKind)))
            systems[k] = Create(k);
    }

    private void OnEnable() { Feedback.OnBurst += Burst; }
    private void OnDisable() { Feedback.OnBurst -= Burst; }

    private void Burst(Vector3 pos, FxKind kind)
    {
        ParticleSystem ps;
        if (!systems.TryGetValue(kind, out ps)) return;
        ps.transform.position = pos;
        ps.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); 
        int count = kind == FxKind.Confetti ? 80 : kind == FxKind.Sparkle ? 25 : kind == FxKind.Sparks ? 18 : 12;
        ps.Emit(count);
    }

    private ParticleSystem Create(FxKind kind)
    {
        var go = new GameObject("FX_" + kind);
        go.transform.SetParent(transform);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 600;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.15f;

        switch (kind)
        {
            case FxKind.Sparks:
                shape.angle = 55f;
                main.startLifetime = 0.5f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.3f), new Color(1f, 0.45f, 0.1f));
                main.gravityModifier = 1.5f;
                break;
            case FxKind.Dust:
                shape.angle = 70f;
                main.startLifetime = 0.8f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
                main.startColor = new Color(0.85f, 0.82f, 0.75f, 0.55f);
                main.gravityModifier = -0.05f;
                break;
            case FxKind.Sparkle:
                shape.angle = 80f;
                main.startLifetime = 0.7f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 1f, 0.5f), Color.white);
                main.gravityModifier = 0.2f;
                break;
            case FxKind.Confetti:
                shape.angle = 35f;
                main.startLifetime = 2.5f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.24f);
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
                var g = new Gradient();
                g.SetKeys(
                    new[] {
                        new GradientColorKey(new Color(1f, 0.3f, 0.3f), 0f),
                        new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.33f),
                        new GradientColorKey(new Color(0.3f, 0.8f, 1f), 0.66f),
                        new GradientColorKey(new Color(0.5f, 1f, 0.4f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                var grad = new ParticleSystem.MinMaxGradient(g);
                grad.mode = ParticleSystemGradientMode.RandomColor;
                main.startColor = grad;
                main.gravityModifier = 1f;
                break;
        }

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = GetParticleMaterial();
        return ps;
    }

    private static Material GetParticleMaterial()
    {
        var rp = GraphicsSettings.currentRenderPipeline;
        Material m = rp != null ? rp.defaultParticleMaterial : null;
        if (m == null)
        {
            Shader s = Shader.Find("Sprites/Default");
            if (s != null) m = new Material(s);
        }
        return m;
    }
}
