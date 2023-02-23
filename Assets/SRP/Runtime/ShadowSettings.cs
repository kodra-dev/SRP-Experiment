using System;
using UnityEngine.Serialization;

namespace SRP.Runtime
{
	[Serializable]
	public struct ShadowSettings
	{
		public TextureSize atlasSize;
		public ShadowFilter filter;
		public float maxDistance;
		
		public static ShadowSettings Default => new()
		{
			atlasSize = TextureSize._1024,
			maxDistance = 100f,
			filter = ShadowFilter.PCF7x7,
		};
	}
}