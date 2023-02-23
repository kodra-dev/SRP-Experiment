using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal;

namespace SRP.Runtime
{
	public static class GlobalResources
	{
		private static Mesh _mesh01Quad;

		public static Mesh FullscreenMesh => RenderingUtils.fullscreenMesh;

		private static Material _unsupportedMaterial;
		public static Material UnsupportedMaterial
		{
			get
			{
				if (_unsupportedMaterial == null)
				{
					Shader shader = Shader.Find("Hidden/InternalErrorShader");
					Assert.IsNotNull(shader, "Shader not found: Hidden/InternalErrorShader");
					_unsupportedMaterial = new Material(shader);
				}

				return _unsupportedMaterial;
			}
		}

		private static Material _blitMaterial;

		public static Material BlitMaterial
		{
			get
			{
				if (_blitMaterial == null)
				{
					Shader shader = Shader.Find("CustomSRP/PostFX/Blit");
					Assert.IsNotNull(shader, "Shader not found: CustomSRP/PostFX/Blit");
					_blitMaterial = new Material(shader);
				}

				return _blitMaterial;
			}
		}
		
		private static Material _bloomMaterial;
		public static Material BloomMaterial
		{
			get
			{
				if (_bloomMaterial == null)
				{
					Shader shader = Shader.Find("CustomSRP/PostFX/Bloom");
					Assert.IsNotNull(shader, "Shader not found: CustomSRP/PostFX/Bloom");
					_bloomMaterial = new Material(shader);
				}
				return _bloomMaterial;
			}
		}
		
	}
}