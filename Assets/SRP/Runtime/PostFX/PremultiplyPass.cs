using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SRP.Runtime
{
	// TODO: SSAA is not actually a post FX pass. It affects how we create RTs.
	//       Move it out to higher level.
	public class PremultiplyPass : IPostFXPass
	{
		private readonly CommandBuffer _buffer = new()
		{
			name = "Post FX PremultiplyPass",
		};

		public PremultiplyPassSettings Settings { get; }
		public bool IsEnabled => Settings.IsEnabled;

		public PremultiplyPass(PremultiplyPassSettings settings)
		{
			Settings = settings;
		}

		public void Draw(ScriptableRenderContext context, Camera _,
			RenderTargetIdentifier src, RenderTargetIdentifier dst)
		{
			// Not using camera's setting because this should be just copying
			BlitValet.BlitParms parms = new(src, dst)
			{
				Pass = (int) BlitPass.PremultiplyAlpha,
				ClearColor = true,
				ClearDepth = true,
				BackgroundColor = new Color(0, 0, 0, 0),
			};
			BlitValet.Blit(context, _buffer, parms);
		}

		public void CleanUp(ScriptableRenderContext context)
		{
			// Nothing to clean up
		}
	}
}