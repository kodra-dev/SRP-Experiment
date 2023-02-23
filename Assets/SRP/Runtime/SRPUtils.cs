using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SRP.Runtime
{
	public static class SRPUtils
	{
		public static void ExecuteAndClearBuffer(this ScriptableRenderContext context, CommandBuffer buffer)
		{
			context.ExecuteCommandBuffer(buffer);
			buffer.Clear();
		}
		
		public static DrawingSettings CreateDrawingSettings(
			IList<ShaderTagId> shaderTagIds,
			ref SortingSettings sortingSettings,
			bool enableDynamicBatching = true,
			bool enableInstancing = true)
		{
			var drawingSettings = new DrawingSettings(shaderTagIds[0], sortingSettings)
			{
				enableDynamicBatching = enableDynamicBatching,
				enableInstancing = enableInstancing,
			};
			for (int i = 0; i < shaderTagIds.Count; i++)
			{
				drawingSettings.SetShaderPassName(i, shaderTagIds[i]);
			}
			return drawingSettings;
		}
		
		public static DrawingSettings CreateDrawingSettings(
			ShaderTagId shaderTagId,
			ref SortingSettings sortingSettings,
			bool enableDynamicBatching = true,
			bool enableInstancing = true)
		{
			var drawingSettings = new DrawingSettings(shaderTagId, sortingSettings)
			{
				enableDynamicBatching = enableDynamicBatching,
				enableInstancing = enableInstancing,
			};
			return drawingSettings;
		}

		public static void GetTemporaryRTFor(
			int nameID, CommandBuffer buffer, Camera camera, int superSampleScale)
		{	
			buffer.GetTemporaryRT(nameID,
				camera.pixelWidth * superSampleScale, camera.pixelHeight * superSampleScale,
				32, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
		}
	}
}