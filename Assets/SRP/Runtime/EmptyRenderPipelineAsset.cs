using UnityEngine;
using UnityEngine.Rendering;

namespace SRP.Runtime
{
	// For debugging and check how much GC is caused by Unity itself
	
	[CreateAssetMenu(menuName = "Custom SRP/Empty Custom Render Pipeline")]
	public class EmptyRenderPipelineAsset  : RenderPipelineAsset
	{
		protected override RenderPipeline CreatePipeline()
		{
			return new EmptyRenderPipeline();
		}
	}

	public class EmptyRenderPipeline : RenderPipeline
	{
		protected override void Render(ScriptableRenderContext context, Camera[] cameras)
		{
			return;
		}
	}
}