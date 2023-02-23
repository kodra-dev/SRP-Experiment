using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace SRP.Runtime
{
	// TODO: SSAA is not actually a post FX pass. It affects how we create RTs.
	//       Move it out to higher level.
	public static class SSAAValet
	{
		private static readonly int SuperSampleScaleID = Shader.PropertyToID("_SuperSampleScale");
		
		private static readonly CommandBuffer Buffer = new()
		{
			name = "SSAAPass",
		};
		
		// A singleton. Not bother to release.
		private static Material _ssaaMaterial;
		public static Material SSAAMaterial
		{
			get
			{
				if (_ssaaMaterial == null)
				{
					Shader shader = Shader.Find("CustomSRP/PostFX/SSAA");
					Assert.IsNotNull(shader, "Shader not found: CustomSRP/PostFX/SSAA");
					_ssaaMaterial = new Material(shader);
				}
				return _ssaaMaterial;
			}
		}


		public static void SSAADownSample(ScriptableRenderContext context, Camera _,
			RenderTargetIdentifier src, RenderTargetIdentifier dst,
			int superSampleScale)
		{
			Buffer.SetGlobalInt(SuperSampleScaleID, superSampleScale);
			
			BlitValet.BlitParms parms = new(src, dst)
			{
				ClearColor = true,
				ClearDepth = true,
				Material = SSAAMaterial,
				Pass = 0,
				BackgroundColor = new Color(0, 0, 0, 0),
			};
			BlitValet.Blit(context, Buffer, parms);
		}

	}
}