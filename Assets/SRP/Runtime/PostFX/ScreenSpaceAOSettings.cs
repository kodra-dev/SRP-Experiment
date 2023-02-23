using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SRP.Runtime
{
	[Serializable]
	public class ScreenSpaceAOSettings : IPostFXPassSettings
	{
		public bool enabled;
		public bool IsEnabled => enabled;
		public IPostFXPass CreatePass()
		{
			return new ScreenSpaceAOPass(this);
		}
		
		// Parameters
		public bool Downsample = false;
		public bool AfterOpaque = false;
		public DepthSource Source = DepthSource.DepthNormals;
		public NormalQuality NormalSamples = NormalQuality.Medium;
		public float Intensity = 3.0f;
		public float DirectLightingStrength = 0.25f;
		public float Radius = 0.035f;
		public int SampleCount = 4;

		// Enums
		public enum DepthSource
		{
			Depth = 0,
			DepthNormals = 1
		}

		public enum NormalQuality
		{
			Low,
			Medium,
			High
		}
	}
}