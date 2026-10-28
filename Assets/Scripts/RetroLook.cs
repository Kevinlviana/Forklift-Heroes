using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RetroLook : MonoBehaviour
{
    [Range(0.25f, 1f)] public float renderScale = 0.55f;
    [Range(0f, 1f)] public float grain = 0.35f;
    [Range(0f, 0.6f)] public float vignette = 0.3f;

    private UniversalRenderPipelineAsset urp;
    private float oldScale;
    private UpscalingFilterSelection oldFilter;
    private bool applied;

    private void Start()
    {
        urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            oldScale = urp.renderScale;
            oldFilter = urp.upscalingFilter;
            urp.renderScale = renderScale;
            urp.upscalingFilter = UpscalingFilterSelection.Point;
            applied = true;
        }

        var cam = Camera.main;
        if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        var volume = FindFirstObjectByType<Volume>();
        if (volume != null && volume.profile != null)
        {
            FilmGrain fg;
            if (!volume.profile.TryGet(out fg)) fg = volume.profile.Add<FilmGrain>(true);
            fg.type.Override(FilmGrainLookup.Medium3);
            fg.intensity.Override(grain);

            Vignette vg;
            if (!volume.profile.TryGet(out vg)) vg = volume.profile.Add<Vignette>(true);
            vg.intensity.Override(vignette);
        }
    }

    private void OnDestroy()
    {
        if (applied && urp != null)
        {
            urp.renderScale = oldScale;
            urp.upscalingFilter = oldFilter;
        }
    }
}
