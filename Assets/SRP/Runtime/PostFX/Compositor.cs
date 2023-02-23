using UnityEngine;
using UnityEngine.Rendering;

namespace SRP.Runtime
{
	// Cameras render to cameraFrameBuffer (with post processing)
	// Then compositor blits the cameraFrameBuffer to the BuiltinRenderTextureType.CameraTarget
	public class Compositor
	{
		private RenderTargetIdentifier _srcRT;
		private RenderTargetIdentifier _dstRT;
		
		private readonly CommandBuffer _buffer = new()
		{
			name = "Compositor",
		};
		private CompositorSettings _settings;

		public Compositor(CompositorSettings settings,
			RenderTargetIdentifier srcRT,
			RenderTargetIdentifier dstRT)
		{
			_settings = settings;
			_srcRT = srcRT;
			_dstRT = dstRT;
		}
		
		public void Reinitialize(CompositorSettings settings,
			RenderTargetIdentifier srcRT,
			RenderTargetIdentifier dstRT)
		{
			_settings = settings;
			_srcRT = srcRT;
			_dstRT = dstRT;
		}

		public void Composite(ScriptableRenderContext context, Camera camera)
		{
			BlitValet.BlitParms parms = new(_srcRT, _dstRT, camera)
			{
				Pass = (int) _settings.BlitPass,
				LoadTargetBuffer = true,
			};
			BlitValet.Blit(context, _buffer, parms);
		}

	}
}