using System.Collections.Generic;
using System.Linq;
using SRP.Shared;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using SystemInfo = UnityEngine.Device.SystemInfo;

namespace SRP.Runtime
{
	public static class ShadowValet
	{
		// private static ShaderTagId[] _shadowCasterShaderPassIds = new[] {ShaderPass.ShadowCaster.ToTagID()};
		private const string BufferName = "Shadow Map";
		
		public static readonly int ShadowAtlasId = Shader.PropertyToID("_ShadowAtlas");
		public static readonly int ShadowLightCountId = Shader.PropertyToID("_ShadowedLightCount");
		public static readonly int WorldToLightClipMatricesId = Shader.PropertyToID("_WorldToLightClipMatrices");
		public static readonly int ShadowTileSizesAndOffsetsId = Shader.PropertyToID("_ShadowTileSizesAndOffsets");
		public static readonly int ShadowStrengthsId = Shader.PropertyToID("_ShadowStrengths");
		public static readonly int ShadowColorsId = Shader.PropertyToID("_ShadowColors");
		public static readonly int NormalBiasesId = Shader.PropertyToID("_NormalBiases");
		public static readonly int ShadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
		
		public static readonly int VisibleLightToShadowedLightIndicesId = Shader.PropertyToID("_VisibleLightToShadowedLightIndices");
		
		// Make the inspector's range more user friendly
		private const float SlopeBiasMultiplier = 10f;
		private const float NormalBiasMultiplier = 0.01f;
		
		
		private static readonly CommandBuffer Buffer = new CommandBuffer
		{
			name = BufferName,
		};

		private static readonly Vector2Int[] PossibleTileLayouts = new[]
		{
			new Vector2Int(1, 1),
			new Vector2Int(2, 2),
			new Vector2Int(4, 4),
		};


		public static int CollectionShadowedLightInfos(
			CullingResults cullingResults,
			int maxShadowedLightCount, List<ShadowedLightInfo> outInfos)
		{
			int index = 0;
			NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
			for (int i = 0; i < visibleLights.Length; i++)
			{
				VisibleLightToShadowedLightIndices[i] = -1;
				
				var vl = visibleLights[i];
				Light light = vl.light;
				CustomLightConfig customLightConfig = SceneCache.ObtainComponent<CustomLightConfig>(light);
				
				Color shadowColor = customLightConfig != null ? customLightConfig.ShadowColor : Color.black;
				bool isCastingShadow = cullingResults.GetShadowCasterBounds(i, out var _);
				if (isCastingShadow)
				{
					 ShadowedLightInfo info = new ShadowedLightInfo()
					 {
						  LightType = light.type,
						  IndexInCullingResults = i,
						  ShadowStrength = light.shadowStrength,
						  ShadowColor = shadowColor,
						  SlopeScaleBias = light.shadowBias * SlopeBiasMultiplier,
						  NormalBias = light.shadowNormalBias * NormalBiasMultiplier,
					 };
					 outInfos.Add(info);
					 VisibleLightToShadowedLightIndices[i] = index;
					 index++;
				}

				if (index >= maxShadowedLightCount)
				{
					Debug.LogWarning(
						"Too many shadowed lights. Max ShadowedLightInfo count is " + maxShadowedLightCount + ".");
					break;
				}
			}

			return index;
		}
		

		public static void CalculateTiles(List<ShadowedLightInfo> outInfos, ShadowSettings shadowSettings)
		{	
			int xCount = 0;
			int yCount = 0;
			foreach (Vector2Int t in PossibleTileLayouts)
			{
				int xC = t.x;
				int yC = t.y;
				int tileCapacity = xC * yC;
				if (tileCapacity >= outInfos.Count)
				{
					 xCount = xC;
					 yCount = yC;
					 break;
				}
			}
			for (int i = 0; i < outInfos.Count; i++)
			{
				ShadowedLightInfo shadowedLightInfo = outInfos[i];
				shadowedLightInfo.TileSizeX = (int)shadowSettings.atlasSize / xCount;
				shadowedLightInfo.TileSizeY = (int)shadowSettings.atlasSize / yCount;
				int row = i / xCount;
				int col = i % xCount;
				shadowedLightInfo.TileOffsetX = col * shadowedLightInfo.TileSizeX;
				shadowedLightInfo.TileOffsetY = row * shadowedLightInfo.TileSizeY;
				outInfos[i] = shadowedLightInfo;
			}
		}

		
		// NOTE: This creates a render target that needs to be released with ReleaseShadowMap()
		public static void DrawDirectionalShadowMap(
			ScriptableRenderContext context,
			Camera _, // Shadow map is independent of camera
			CullingResults cullingResults,
			List<ShadowedLightInfo> lightInfos,
			ShadowSettings shadowSettings)
		{
			
			Buffer.BeginSample(BufferName);
			context.ExecuteAndClearBuffer(Buffer);
			
			int atlasSize = (int)shadowSettings.atlasSize;
			
			Buffer.GetTemporaryRT(ShadowAtlasId,
				atlasSize, atlasSize,
				24,
				FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
			
			Buffer.SetRenderTarget(ShadowAtlasId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			Buffer.ClearRenderTarget(true, false, Color.clear);

			for (var i = 0; i < lightInfos.Count; i++)
			{
				ShadowedLightInfo lightInfo = lightInfos[i];
				Matrix4x4 viewMatrix = Matrix4x4.identity;
				Matrix4x4 projectionMatrix = Matrix4x4.identity;
				ShadowSplitData shadowSplitData = new ShadowSplitData();

				switch (lightInfo.LightType)
				{
					case LightType.Directional:
						cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
							 lightInfo.IndexInCullingResults,
							 0, 1, Vector3.zero,
							 atlasSize,
							 0,
							 out viewMatrix, out projectionMatrix, out shadowSplitData);
						break;
					 case LightType.Spot:
						cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(
							lightInfo.IndexInCullingResults,
							out viewMatrix, out projectionMatrix, out shadowSplitData);	
						break;
					default:
						// Other light types are not supported
						break;
				}

				// Update matrices to calculate WorldToLightClipMatrices later
				lightInfo.ViewMatrix = viewMatrix;
				lightInfo.ProjectionMatrix = projectionMatrix;
				
				lightInfos[i] = lightInfo;

				Buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
				Buffer.SetViewport(new Rect(
					lightInfo.TileOffsetX, lightInfo.TileOffsetY,
					lightInfo.TileSizeX, lightInfo.TileSizeY));

				Buffer.SetGlobalDepthBias(0.0f, lightInfo.SlopeScaleBias);
				context.ExecuteAndClearBuffer(Buffer);

				ShadowDrawingSettings shadowDrawingSettings =
					new ShadowDrawingSettings(cullingResults, lightInfo.IndexInCullingResults)
					{
						splitData = shadowSplitData,
					};
				context.DrawShadows(ref shadowDrawingSettings);
				Buffer.SetGlobalDepthBias(0.0f, 0.0f);
				context.ExecuteAndClearBuffer(Buffer);
			}


			Buffer.EndSample(BufferName);
			context.ExecuteAndClearBuffer(Buffer);
			
			// camera.worldToCameraMatrix = oldViewMatrix;
			// camera.projectionMatrix = oldProjectionMatrix;
		}
		
		// Missing textures causes error on some platforms
		// NOTE: This creates a render target that needs to be released with ReleaseShadowMap()
		public static void CreateDummyShadowMap(ScriptableRenderContext context)
		{
			Buffer.GetTemporaryRT(ShadowAtlasId,
				1, 1,
				32,
				FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
			
			Buffer.SetRenderTarget(ShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
			Buffer.ClearRenderTarget(true, false, Color.clear);
			context.ExecuteAndClearBuffer(Buffer);
		}
		
		public static void ReleaseShadowMap(ScriptableRenderContext context)
		{
			Buffer.ReleaseTemporaryRT(ShadowAtlasId);
			context.ExecuteAndClearBuffer(Buffer);
		}
		
		// The only purpose of these is to send data to the shaders
		// So we don't need to worry about cleaning them up
		private static readonly Matrix4x4[] WorldToLightClipMatrices = new Matrix4x4[CameraRenderer.MaxShadowedLightCount];
		private static readonly Vector4[] ShadowTileSizesAndOffsets = new Vector4[CameraRenderer.MaxShadowedLightCount];
		private static readonly float[] ShadowStrengths = new float[CameraRenderer.MaxShadowedLightCount];
		private static readonly float[] NormalBiases = new float[CameraRenderer.MaxShadowedLightCount];
		private static readonly Vector4[] ShadowColors = new Vector4[CameraRenderer.MaxShadowedLightCount];
		
		// Set on CollectShadowedLightInfos()
		private static readonly float[] VisibleLightToShadowedLightIndices = new float[CameraRenderer.MaxLightCount];

		public static void SetupShadowMapForShaders(
			ScriptableRenderContext context,
			CullingResults cullingResults,
			List<ShadowedLightInfo> lightInfos,
			ShadowSettings shadowSettings
			)
		{
			Buffer.SetGlobalInt(ShadowLightCountId, lightInfos.Count);
			for(int i = 0; i < lightInfos.Count; i++)
			{
				ShadowedLightInfo lightInfo = lightInfos[i];
				Matrix4x4 worldToShadowMatrix = CalculateWorldToShadowMatrix(lightInfo);
				WorldToLightClipMatrices[i] = worldToShadowMatrix;
				// WorldToLightClipMatrices[i] = Matrix4x4.identity;
				ShadowTileSizesAndOffsets[i] = new Vector4(
					(float) lightInfo.TileSizeX / (int) shadowSettings.atlasSize,
					(float) lightInfo.TileSizeY / (int) shadowSettings.atlasSize,
					(float) lightInfo.TileOffsetX / (int) shadowSettings.atlasSize,
					(float) lightInfo.TileOffsetY / (int) shadowSettings.atlasSize);
				ShadowStrengths[i] = lightInfo.ShadowStrength;
				NormalBiases[i] = lightInfo.NormalBias;
				ShadowColors[i] = lightInfo.ShadowColor;
			}
			Buffer.SetGlobalFloatArray(VisibleLightToShadowedLightIndicesId, VisibleLightToShadowedLightIndices);
			Buffer.SetGlobalMatrixArray(WorldToLightClipMatricesId, WorldToLightClipMatrices);
			Buffer.SetGlobalVectorArray(ShadowTileSizesAndOffsetsId, ShadowTileSizesAndOffsets);
			Buffer.SetGlobalFloatArray(ShadowStrengthsId, ShadowStrengths);
			Buffer.SetGlobalFloatArray(NormalBiasesId, NormalBiases);
			Buffer.SetGlobalVectorArray(ShadowColorsId, ShadowColors);
			
			Buffer.SetGlobalVector(ShadowAtlasSizeId, new Vector4(
				(int) shadowSettings.atlasSize,
				(int) shadowSettings.atlasSize,
				1.0f / (int) shadowSettings.atlasSize,
				1.0f / (int) shadowSettings.atlasSize
			));
			
			context.ExecuteAndClearBuffer(Buffer);
		}
		
		private static Matrix4x4 CalculateWorldToShadowMatrix(ShadowedLightInfo lightInfo)
		{
			Matrix4x4 m = lightInfo.ProjectionMatrix * lightInfo.ViewMatrix;
			return m;
		}
	}
}