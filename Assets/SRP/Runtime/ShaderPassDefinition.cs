using UnityEngine.Rendering;

namespace SRP.Runtime
{
	public static class ShaderPassDefinition
	{
		private static readonly ShaderTagId CustomLit = new ShaderTagId("CustomLit");
		private static readonly ShaderTagId CustomUnlit = new ShaderTagId("CustomUnlit");
		private static readonly ShaderTagId ShadowCaster = new ShaderTagId("ShadowCaster");
		private static readonly ShaderTagId DepthNormal = new ShaderTagId("DepthNormal");
		public static readonly ShaderTagId[] LegacyIds = {
			new ShaderTagId("Always"),
			new ShaderTagId("ForwardBase"),
			new ShaderTagId("PrepassBase"),
			new ShaderTagId("Vertex"),
			new ShaderTagId("VertexLMRGBM"),
			new ShaderTagId("VertexLM")
		};
		
		public static ShaderTagId ToTagID(this ShaderPass pass)
		{
			return pass switch
			{
				ShaderPass.CustomUnlit => CustomUnlit,
				ShaderPass.CustomLit => CustomLit,
				ShaderPass.ShadowCaster => ShadowCaster,
				ShaderPass.DepthNormal => DepthNormal,
				_ => CustomUnlit,
			};
		}
	}
	
	public enum ShaderPass
	{
		None, // Use the pipeline's default shader passes without overriding
		CustomUnlit,
		CustomLit,
		ShadowCaster,
		DepthNormal,
	}
}