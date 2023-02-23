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
		// public bool AfterOpaque = false;
		public DepthSource Source = DepthSource.DepthNormals;
		public NormalQuality NormalSamples = NormalQuality.High;
		[Min(0)]
		public float Intensity = 3.0f;
		[HideInInspector] public float DirectLightingStrength = 0.25f;
		[Min(0)] public float Radius = 0.035f;
		[Range(2, 40)]
		public int SampleCount = 10;

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