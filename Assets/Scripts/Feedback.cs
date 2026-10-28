using System;
using UnityEngine;

public enum Sfx { Clack, Place, Chime, Error, Crash, Glass, Beep, Tick, Go, Win, Lose, Thump, Hit }
public enum FxKind { Sparks, Dust, Sparkle, Confetti }


public static class Feedback
{
    public static event Action<Sfx> OnSfx;
    public static event Action<float> OnShake;
    public static event Action<Vector3, FxKind> OnBurst;

    public static void PlaySfx(Sfx s) => OnSfx?.Invoke(s);
    public static void Shake(float force) => OnShake?.Invoke(force);
    public static void SpawnBurst(Vector3 pos, FxKind kind) => OnBurst?.Invoke(pos, kind);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { OnSfx = null; OnShake = null; OnBurst = null; }
}
