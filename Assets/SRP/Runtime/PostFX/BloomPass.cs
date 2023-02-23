using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace SRP.Runtime
{
	public class BloomPass : IPostFXPass
	{
		public const int MaxIteration = 16;
		
		public BloomSettings Settings { get; }
		
		private static readonly int BloomPrefilteredID = Shader.PropertyToID("_BloomPrefiltered");
		private static readonly int BloomKneePrecomputedID = Shader.PropertyToID("_BloomKneePrecomputed");
		private static readonly int BloomIntensityID = Shader.PropertyToID("_BloomIntensity");

		private static int[] _pyramidBufferIDs = null; // *2 for horizontal and vertical
		private static int[] PyramidBufferIDs
		{
			get
			{
				if (_pyramidBufferIDs == null)
				{
					_pyramidBufferIDs = new int[MaxIteration * 2]; // *2 for horizontal and vertical
					for(int i = 0; i < MaxIteration; i++)
					{
						PyramidBufferIDs[i*2] = Shader.PropertyToID($"_BloomPyramid{i}_H");
						PyramidBufferIDs[i*2+1] = Shader.PropertyToID($"_BloomPyramid{i}_V");
					}	
				}
				return _pyramidBufferIDs;
			}
		}

		private readonly CommandBuffer _buffer = new()
		{
			name = "Post FX BloomPass",
		};
		
		private int _lastIteration = -1;

		private enum Pass
		{
			BloomPrefilter = 0,
			HorizontalBlur = 1,
			VerticalBlur = 2,
			CombineBloom = 3,
			CombineBloomFinal = 4,
		}

		// A singleton. Not bother to release.
		private static Material _bloomMaterial;
		public static Material BloomMaterial
		{
			get
			{
				if (_bloomMaterial == null)
				{
					Shader shader = Shader.Find("CustomSRP/PostFX/Bloom");
					Assert.IsNotNull(shader, "Shader not found: CustomSRP/PostFX/Bloom");
					_bloomMaterial = new Material(shader);
				}
				return _bloomMaterial;
			}
		}
		
		public BloomPass(BloomSettings settings)
		{
			Settings = settings;
		}
		
		
		// The pyramid looks like:
		// -1 (original resolution): src, _pyramidBufferIDs[0] (h0)
		// 0  (  /2     resolution): _pyramidBufferIDs[1], _pyramidBufferIDs[2] (v0, h1)
		// 1  (  /4     resolution): _pyramidBufferIDs[3], _pyramidBufferIDs[4] (v1, h2)
		// etc

		public bool IsEnabled => Settings.enabled;

		public void Draw(
			ScriptableRenderContext context, Camera camera,
			RenderTargetIdentifier src, RenderTargetIdentifier dst)
		{
			// We don't use super sampling scale for the boom pyramid
			// So the amount of blur is not affected by the super sampling scale
			// int scale = CustomCameraConfig.GetSuperSampleScale(camera);

			int originalWidth = camera.pixelWidth;
			int originalHeight = camera.pixelHeight;
			int width = originalWidth;
			int height = originalHeight;

			SetThresholdKneePrecomputed();
			
			for (int i = 0; i < Settings.iteration; i++)
			{
				if(width <= 2 || height <= 2)
					break;

				int hi = i * 2;
				int vi = hi + 1;
				
				_lastIteration = i;
				_buffer.GetTemporaryRT(PyramidBufferIDs[hi],
					width, height,
					0,
					FilterMode.Bilinear, RenderTextureFormat.ARGB32);
				
				// Horizontal one is at the same resolution, and vertical one is half the resolution
				width /= 2;
				height /= 2;
				_buffer.GetTemporaryRT(PyramidBufferIDs[vi],
					width, height,
					0,
					FilterMode.Bilinear, RenderTextureFormat.ARGB32);
			}
			
			
			// Prefilter the original image
			_buffer.GetTemporaryRT(BloomPrefilteredID,
				originalWidth, originalHeight,
				0,
				FilterMode.Bilinear, RenderTextureFormat.ARGB32);
			BlitValet.BlitParms blitParms = new(src, BloomPrefilteredID)
			{
				Material = BloomMaterial,
				Pass = (int) Pass.BloomPrefilter,
				BackgroundColor = Color.black,
			};
			BlitValet.Blit(context, _buffer, blitParms);
			
			_buffer.SetGlobalFloat(BloomIntensityID, Settings.intensity);
			
			
			RenderTargetIdentifier lastBuffer = BloomPrefilteredID;
			for (int i = 0; i <= _lastIteration; i++)
			{
				int hi = i * 2;
				int vi = hi + 1;
				BlitValet.BlitParms blitParmsH = new(lastBuffer, PyramidBufferIDs[hi])
				{
					Material = BloomMaterial,
					Pass = (int) Pass.HorizontalBlur,
				};
				BlitValet.Blit(context, _buffer, blitParmsH);
				BlitValet.BlitParms blitParmsV = new(PyramidBufferIDs[hi], PyramidBufferIDs[vi])
				{
					Material = BloomMaterial,
					Pass = (int) Pass.VerticalBlur,
				};
				BlitValet.Blit(context, _buffer, blitParmsV);
				lastBuffer = PyramidBufferIDs[vi];
			}

			for (int i = _lastIteration-1; i >= 0; i--)
			{
				int hi = i * 2;
				int vi = hi + 1;
				
				// hi is "unused" here, so we use it to store the result of lastBuffer + _pyramidBufferIDs[vi]
				BlitValet.BlitParms blitParmsCombine = new(lastBuffer, PyramidBufferIDs[hi])
				{
					Material = BloomMaterial,
					Pass = (int) Pass.CombineBloom,
				};
				BlitValet.BlitWithExtraTextures(
					context, _buffer, blitParmsCombine,
					extraSrc1: PyramidBufferIDs[vi]
				);
				lastBuffer = PyramidBufferIDs[hi];
			}

			BlitValet.BlitParms blitParmsFinal = new(lastBuffer, dst)
			{
				Material = BloomMaterial,
				Pass = (int) Pass.CombineBloomFinal,
			};
			BlitValet.BlitWithExtraTextures(
				context, _buffer, blitParmsFinal,
				extraSrc1: src
			);

			
			// ==== Local Functions ====
			
			void SetThresholdKneePrecomputed()
			{
				float t = Mathf.GammaToLinearSpace(Settings.threshold);
				float k = Settings.thresholdKnee;
				Vector4 kneePrecomputed = new Vector4(
					t,
					-t + t * k,
					2 * t * k,
					1f / (4 * t * k + 0.000001f)
				);
				_buffer.SetGlobalVector(BloomKneePrecomputedID, kneePrecomputed);
			}
		}
		
		public void CleanUp(ScriptableRenderContext context)
		{
			for (int i = 0; i < _lastIteration; i++)
			{
				_buffer.ReleaseTemporaryRT(PyramidBufferIDs[i*2]);
				_buffer.ReleaseTemporaryRT(PyramidBufferIDs[i*2+1]);
			}
			_buffer.ReleaseTemporaryRT(BloomPrefilteredID);

			context.ExecuteAndClearBuffer(_buffer);
		}
	}
}