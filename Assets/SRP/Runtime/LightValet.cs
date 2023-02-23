using SRP.Shared;
using Unity.Burst.Intrinsics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SRP.Runtime
{
	public static class LightValet
	{
		public static readonly int LightCountID = Shader.PropertyToID("_LightCount");
		public static readonly int LightColorsID = Shader.PropertyToID("_LightColors");
		public static readonly int LightDirectionsID = Shader.PropertyToID("_LightDirections");
		public static readonly int LightSpotAnglesID = Shader.PropertyToID("_LightSpotAngles");
		public static readonly int LightPositionsID = Shader.PropertyToID("_LightPositions");
		public static readonly int LightTypesID = Shader.PropertyToID("_LightTypes");
		
		private static readonly Vector4[] LightColors = new Vector4[CameraRenderer.MaxLightCount];
		private static readonly Vector4[] LightDirections = new Vector4[CameraRenderer.MaxLightCount];
		// x = cos(inner / 2), y = cos(outer / 2)
		// See https://catlikecoding.com/unity/tutorials/custom-srp/point-and-spot-lights/#2
		private static readonly Vector4[] LightSpotAngles = new Vector4[CameraRenderer.MaxLightCount];
		private static readonly Vector4[] LightPositions = new Vector4[CameraRenderer.MaxLightCount];
		private static readonly float[] LightTypes = new float[CameraRenderer.MaxLightCount];
		
		
		private static readonly CommandBuffer Buffer = new CommandBuffer
		{
			name = "Setup Lights",
		};
			

		public static void SetupLightsForShaders(ScriptableRenderContext context, CullingResults cullingResults)
		{
			int lightCount = 0;
			var visibleLights = cullingResults.visibleLights;
			for (int i = 0; i < visibleLights.Length; i++)
			{
				var vl = visibleLights[i];
				switch (vl.lightType)
				{
					case LightType.Directional:
						SetDirectionalLightOf(i, vl);
						lightCount++;
						break;
					case LightType.Point:
						SetPointLightOf(i, vl);
						lightCount++;
						break;
					case LightType.Spot:
						SetSpotLightOf(i, vl);
						lightCount++;
						break;
				}

				if (lightCount >= CameraRenderer.MaxLightCount)
				{
					break;
				}
			}

			Buffer.SetGlobalInt(LightCountID, lightCount);	
			Buffer.SetGlobalFloatArray(LightTypesID, LightTypes);
			Buffer.SetGlobalVectorArray(LightColorsID, LightColors);
			Buffer.SetGlobalVectorArray(LightDirectionsID, LightDirections);
			Buffer.SetGlobalVectorArray(LightPositionsID, LightPositions);
			Buffer.SetGlobalVectorArray(LightSpotAnglesID, LightSpotAngles);
			context.ExecuteAndClearBuffer(Buffer);
			
			void SetDirectionalLightOf(int index, VisibleLight vl)
			{
				LightColors[index] = vl.finalColor;
				// Z of localToWorldMatrix is the direction from the light to the origin
				LightDirections[index] = -vl.localToWorldMatrix.GetColumn(2);
				LightTypes[index] = 1;
			}
			
			void SetPointLightOf(int index, VisibleLight vl)
			{
				LightColors[index] = vl.finalColor;
				LightPositions[index] = vl.localToWorldMatrix.GetColumn(3);
				LightTypes[index] = 2;
			}
			
			void SetSpotLightOf(int index, VisibleLight vl)
			{
				LightColors[index] = vl.finalColor;
				LightPositions[index] = vl.localToWorldMatrix.GetColumn(3);
				LightDirections[index] = -vl.localToWorldMatrix.GetColumn(2);
				LightSpotAngles[index] = new Vector4(
					Mathf.Cos(vl.light.innerSpotAngle * 0.5f * Mathf.Deg2Rad),
					Mathf.Cos(vl.spotAngle * 0.5f * Mathf.Deg2Rad)
				);
				LightTypes[index] = 0;
			}
		}

	}
}