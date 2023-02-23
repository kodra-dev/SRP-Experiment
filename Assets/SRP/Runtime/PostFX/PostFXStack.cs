using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SRP.Runtime
{
	
	// Hold the references to all the post processing passes so that we don't create them every frame
	public class PostFXStack
	{
		private const int MaxPostFXPasses = 16;
		
		// +1 for premultiply pass
		private readonly List<IPostFXPass> _passes = new(MaxPostFXPasses + 1);
		

		// cameraFrameBufferID is where the camera rendered to
		// cameraFrameBuffer2ID is empty and will be used as a temporary buffer
		// finalTargetID is where the final result will be rendered to
		public PostFXStack(PostFXSettings settings)
		{
			var premultiplyPass = new PremultiplyPass(new PremultiplyPassSettings());
			_passes.Add(premultiplyPass);
			
			foreach (IPostFXPassSettings passSettings in settings.PassSettings)
			{
				if (passSettings != null)
				{
					IPostFXPass pass = passSettings.CreatePass();	
					_passes.Add(pass);
				}
			}
		}
		
		public RenderTargetIdentifier DrawPostFX(
			ScriptableRenderContext context, Camera camera,
			RenderTargetIdentifier src, RenderTargetIdentifier backBuffer
			)
		{
			RenderTargetIdentifier currentSrc = src;
			RenderTargetIdentifier currentDst = backBuffer;
		
			foreach(IPostFXPass pass in _passes) {
				if (pass.IsEnabled)
				{
					pass.Draw(context, camera, currentSrc, currentDst);
					(currentSrc, currentDst) = (currentDst, currentSrc);
				}
			}

			foreach (IPostFXPass pass in _passes)
			{
				pass.CleanUp(context);
			}

			return currentSrc;
		}

	}
}