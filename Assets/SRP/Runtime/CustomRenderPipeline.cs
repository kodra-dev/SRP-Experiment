using System.Collections;
using System.Collections.Generic;
using SRP.Runtime;
using UnityEngine;
using UnityEngine.Rendering;


public class CustomRenderPipeline : RenderPipeline
{
    private readonly CameraRenderer _cameraRenderer;

    public CustomRenderPipeline(
        ShadowSettings directionalShadowSettings,
        PostFXSettings postFXSettings,
        CompositorSettings compositorSettings
        )
    {
        _cameraRenderer = new CameraRenderer(
            directionalShadowSettings, postFXSettings, compositorSettings);
    }
    
    public void SetShaderPassOverride(ShaderPass shaderPassOverride)
    {
        _cameraRenderer.ShaderPassOverride = shaderPassOverride;
    }
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //SRP Batcher
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
        _cameraRenderer.Render(context, cameras);
    }
}
