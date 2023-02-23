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
		public bool downsample = false;
		// public bool AfterOpaque = false;
		public DepthSource source = DepthSource.DepthNormals;
		public NormalQuality normalSamples = NormalQuality.High;
		[Min(0)]
		public float intensity = 3.0f;
		[HideInInspector] public float directLightingStrength = 0.25f;
		[Min(0)] public float radius = 0.035f;
		[Range(2, 40)]
		public int sampleCount = 10;

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