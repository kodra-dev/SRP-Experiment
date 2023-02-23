using UnityEngine;

namespace SRP.Runtime
{
	public struct ShadowedLightInfo
	{
		public LightType LightType;
		
		public int IndexInCullingResults;
		public Matrix4x4 ViewMatrix;
		public Matrix4x4 ProjectionMatrix;
		
		public int TileSizeX;
		public int TileSizeY;
		public int TileOffsetX;
		public int TileOffsetY;
		public float ShadowStrength;
		public float SlopeScaleBias;
		public float NormalBias;
		public Color ShadowColor;
	}
}