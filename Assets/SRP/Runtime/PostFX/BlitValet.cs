using UnityEngine;
using UnityEngine.Rendering;

namespace SRP.Runtime
{
	public static class BlitValet
	{
		public static int BlitSourceID => Shader.PropertyToID("_BlitSource");
		public static int ExtraSource1ID => Shader.PropertyToID("_ExtraSource1");
		public static int ExtraSource2ID => Shader.PropertyToID("_ExtraSource2");
		public static int ExtraSource3ID => Shader.PropertyToID("_ExtraSource3");
		public static int ExtraSource4ID => Shader.PropertyToID("_ExtraSource4");

		public static void Blit(
			ScriptableRenderContext context,
			CommandBuffer buffer,
			BlitParms blitParms
			)
		{
			RenderTargetIdentifier src = blitParms.Src;
			RenderTargetIdentifier dst = blitParms.Dst;
			Material material = blitParms.Material;
			int pass = blitParms.Pass;
			Rect viewport = blitParms.Viewport;
			bool setViewport = blitParms.SetViewport;
			bool clearDepth = blitParms.ClearDepth;
			bool clearColor = blitParms.ClearColor;
			Color backgroundColor = blitParms.BackgroundColor;
			bool loadTargetBuffer = blitParms.LoadTargetBuffer;
				
			buffer.SetRenderTarget(dst,
				loadTargetBuffer ? RenderBufferLoadAction.Load : RenderBufferLoadAction.DontCare,
				RenderBufferStoreAction.Store);
			
			if (setViewport)
			{
				buffer.SetViewport(viewport);
			}
			
			buffer.SetGlobalTexture(BlitSourceID, src);
			buffer.ClearRenderTarget(clearDepth, clearColor, backgroundColor);
			// _buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
			
			// Potential optimization: use a procedural triangle
			buffer.DrawMesh(GlobalResources.FullscreenMesh,
							Matrix4x4.identity,
							material, 0, pass);
			context.ExecuteAndClearBuffer(buffer);
		}
		
		public static void BlitWithExtraTextures(
			ScriptableRenderContext context, CommandBuffer buffer,
			BlitParms blitParms,
			RenderTargetIdentifier extraSrc1)
		{
			buffer.SetGlobalTexture(ExtraSource1ID, extraSrc1);
			Blit(context, buffer, blitParms);
		}
		
		public struct BlitParms
		{
			public RenderTargetIdentifier Src;
			public RenderTargetIdentifier Dst;
			public Material Material;
			public int Pass;
			public Rect Viewport;
			public bool SetViewport;
			public bool ClearDepth;
			public bool ClearColor;
			public Color BackgroundColor;
			public bool LoadTargetBuffer;
			
			public BlitParms(RenderTargetIdentifier src, RenderTargetIdentifier dst)
			{
				Src = src;
				Dst = dst;
				Material = GlobalResources.BlitMaterial;
				Pass = 0;
				Viewport = new Rect();
				SetViewport = false;
				ClearDepth = true;
				ClearColor = true;
				BackgroundColor = new Color(0, 0, 0, 0);
				LoadTargetBuffer = false;
			}

			public BlitParms(RenderTargetIdentifier src, RenderTargetIdentifier dst, Camera camera) : this(src, dst)
			{
				Viewport = camera.pixelRect;
				SetViewport = true;
				CameraClearFlags clearFlags = camera.clearFlags;
				ClearDepth = clearFlags <= CameraClearFlags.Depth;
				ClearColor = clearFlags <= CameraClearFlags.Color;
				BackgroundColor = ClearColor ? camera.backgroundColor : new Color(0, 0, 0, 0);
			}
		}
	}
}