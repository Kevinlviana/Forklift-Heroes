using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class AudioFX : MonoBehaviour
{
    private const int SR = 44100;

    private readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
    private AudioSource sfxSource;
    private AudioSource engineSource;
    private DriverController driver;
    private Rigidbody driverRb;
    private float beepTimer;
    private System.Random rng = new System.Random(7);

    private void Awake()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        BuildClips();

        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.clip = Make("engine", 1f, t =>
        {
            float s = 0f;
            for (int k = 1; k <= 5; k++) s += Mathf.Sin(2f * Mathf.PI * 55f * k * t) / k;
            return s * 0.35f;
        });
        engineSource.loop = true;
        engineSource.spatialBlend = 0f;
        engineSource.volume = 0f;
        engineSource.Play();
    }

    private void OnEnable() { Feedback.OnSfx += Play; }
    private void OnDisable() { Feedback.OnSfx -= Play; }

    private void Start()
    {
        driver = FindFirstObjectByType<DriverController>();
        if (driver != null) driverRb = driver.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.mKey.wasPressedThisFrame)
            AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f;

        var gm = GameManager.Instance;
        bool live = gm != null && gm.Phase == GamePhase.Playing;

        float speed = 0f;
        bool reversing = false;
        if (driverRb != null)
        {
            Vector3 v = driverRb.linearVelocity;
            v.y = 0f;
            speed = v.magnitude;
            reversing = Vector3.Dot(v, driverRb.transform.forward) < -0.5f;
        }

        float t01 = Mathf.Clamp01(speed / 12f);
        engineSource.pitch = Mathf.Lerp(0.8f, 2.2f, t01);
        float targetVol = live ? Mathf.Lerp(0.05f, 0.16f, t01) : 0.02f;
        engineSource.volume = Mathf.MoveTowards(engineSource.volume, targetVol, Time.unscaledDeltaTime * 0.5f);

       
        if (live && reversing)
        {
            beepTimer -= Time.deltaTime;
            if (beepTimer <= 0f) { Play(Sfx.Beep); beepTimer = 0.5f; }
        }
        else beepTimer = 0f;
    }

    public void Play(Sfx id)
    {
        AudioClip c;
        if (clips.TryGetValue(id, out c)) sfxSource.PlayOneShot(c, id == Sfx.Beep ? 0.35f : 0.8f);
    }

  

    private float Noise() { return (float)(rng.NextDouble() * 2.0 - 1.0); }
    private static float Decay(float t, float k) { return Mathf.Exp(-t * k); }
    private static float Sine(float f, float t) { return Mathf.Sin(2f * Mathf.PI * f * t); }
    private static float Square(float f, float t) { return Sine(f, t) >= 0f ? 1f : -1f; }

    private void BuildClips()
    {
        clips[Sfx.Clack] = Make("clack", 0.12f, t => (Noise() * 0.5f + Sine(220f, t)) * Decay(t, 35f) * 0.8f);
        clips[Sfx.Place] = Make("place", 0.2f, t => Sine(Mathf.Lerp(140f, 60f, t / 0.2f), t) * Decay(t, 14f));
        clips[Sfx.Thump] = Make("thump", 0.25f, t => (Sine(70f, t) + Noise() * 0.3f) * Decay(t, 14f));
        clips[Sfx.Chime] = Make("chime", 0.5f, t =>
        {
            float a = Sine(880f, t) * Decay(t, 9f);
            float b = t > 0.09f ? Sine(1318.5f, t - 0.09f) * Decay(t - 0.09f, 7f) : 0f;
            return (a + b) * 0.5f;
        });
        clips[Sfx.Error] = Make("error", 0.35f, t => Square(Mathf.Lerp(160f, 100f, t / 0.35f), t) * Decay(t, 6f) * 0.5f);
        clips[Sfx.Crash] = Make("crash", 0.4f, t => (Noise() * 0.8f + Sine(60f, t)) * Decay(t, 9f));
        clips[Sfx.Hit] = Make("hit", 0.3f, t => (Noise() * 0.4f + Sine(110f, t) + Square(240f, t) * 0.2f) * Decay(t, 10f));
        clips[Sfx.Glass] = Make("glass", 0.5f, t =>
        {
            float sparkle = Noise() * (Mathf.Sin(t * 90f) > 0.2f ? 1f : 0.3f);
            return (sparkle * 0.6f + Sine(2800f + 700f * Mathf.Sin(t * 40f), t) * 0.3f) * Decay(t, 8f);
        });
        clips[Sfx.Beep] = Make("beep", 0.12f, t => Square(1000f, t) * 0.4f * (t < 0.1f ? 1f : 0f));
        clips[Sfx.Tick] = Make("tick", 0.06f, t => Sine(1500f, t) * Decay(t, 60f));
        clips[Sfx.Go] = Make("go", 0.5f, t => Sine(1046.5f, t) * Decay(t, 5f) * 0.7f);
        clips[Sfx.Win] = Make("win", 0.8f, t =>
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            int i = Mathf.Min(3, (int)(t / 0.14f));
            float lt = t - i * 0.14f;
            return Sine(notes[i], lt) * Decay(lt, i == 3 ? 3f : 12f) * 0.7f;
        });
        clips[Sfx.Lose] = Make("lose", 0.9f, t =>
        {
            float[] notes = { 392f, 349.2f, 293.7f, 196f };
            int i = Mathf.Min(3, (int)(t / 0.2f));
            float lt = t - i * 0.2f;
            return Square(notes[i], lt) * Decay(lt, i == 3 ? 3f : 10f) * 0.35f;
        });
    }

    private static AudioClip Make(string clipName, float duration, Func<float, float> sample)
    {
        int n = Mathf.CeilToInt(duration * SR);
        var data = new float[n];
        for (int i = 0; i < n; i++)
            data[i] = Mathf.Clamp(sample(i / (float)SR), -1f, 1f);
        var clip = AudioClip.Create(clipName, n, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }
}
