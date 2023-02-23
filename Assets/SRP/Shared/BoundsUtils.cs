using UnityEngine;

namespace SRP.Shared
{
	public static class BoundsUtils
	{
		public static float Volume(this Bounds bounds)
		{
			return bounds.size.x * bounds.size.y * bounds.size.z;
		}
		
	}
}