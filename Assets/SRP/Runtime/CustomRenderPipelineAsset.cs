using System;
using System.Collections;
using System.Collections.Generic;
using SRP.Runtime;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Custom SRP/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    // NOTE: Modifying these values at runtime WILL NOT update the pipeline
    [SerializeField] private ShaderPass shaderPassOverride = ShaderPass.None;
    [SerializeField] private ShadowSettings shadowSettings = ShadowSettings.Default;
    [SerializeField] private PostFXSettings postFXSettings = new()
    {
    };

    [SerializeField] private CompositorSettings compositorSettings = new()
    {
        compositionOp = CompositionOp.AlphaBlendPremultiplied,
    };

    // Unity seems to call this on OnValidate() so we can send updated inspector values to the pipeline constructor
    protected override RenderPipeline CreatePipeline()
    {
        var pipeline = new CustomRenderPipeline(shadowSettings, postFXSettings, compositorSettings);
        pipeline.SetShaderPassOverride(shaderPassOverride);
        return pipeline;
    }
}
