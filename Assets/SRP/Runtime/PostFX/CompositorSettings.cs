using System;
using UnityEngine.Serialization;

namespace SRP.Runtime
{
	[Serializable]
	public struct CompositorSettings
	{
		public CompositionOp compositionOp;

		// A bit of abstraction because we might want to add more composition operations
		public BlitPass BlitPass => compositionOp switch
		{
			// CompositionOp.AlphaBlend => BlitPass.AlphaBlend,
			CompositionOp.AlphaBlendPremultiplied => BlitPass.AlphaBlendPremultiplied,
			_ => throw new ArgumentOutOfRangeException(),
		};
	}

	public enum CompositionOp
	{
		AlphaBlendPremultiplied = 2,
	}
}