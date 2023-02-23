using System;
using UnityEngine;

namespace SRP.Runtime
{
	[Serializable]
	public class BloomSettings : IPostFXPassSettings
	{
		public bool enabled = true;
		public bool IsEnabled => enabled;

		[Range(0, 2)]
		public float intensity = 0.5f;
		[Range(0, BloomPass.MaxIteration)]
		public int iteration = 4;
		[Range(0f, 1f)]
		public float threshold = 0.5f;
		[Range(0f, 1f)]
		public float thresholdKnee = 0.88f;

		public IPostFXPass CreatePass()
		{
			return new BloomPass(this);
		}
	}
}