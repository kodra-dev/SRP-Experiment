using UnityEngine;
using UnityEngine.Rendering;

namespace SRP.Runtime
{
	public interface IPostFXPass
	{
		bool IsEnabled { get; }
		void Draw(ScriptableRenderContext context, Camera camera,
			RenderTargetIdentifier src, RenderTargetIdentifier dst);

		void CleanUp(ScriptableRenderContext context);
	}
}