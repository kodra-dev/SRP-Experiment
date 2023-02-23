using System.Collections.Generic;
using SRP.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace SRP.Runtime
{
	public class CameraRenderer
	{
		public const int MaxCameraCount = 16;
		public const int MaxLightCount = 16;
		public const int MaxShadowedLightCount = 16;
		
		// We might make these configurable and non-static in the future
		
		private static readonly ShaderTagId[] OpaqueShaderPassIds = {ShaderPass.CustomLit.ToTagID(), ShaderPass.CustomUnlit.ToTagID()};
		private static readonly ShaderTagId[] TransparentShaderPassIds = {ShaderPass.CustomLit.ToTagID(), ShaderPass.CustomUnlit.ToTagID()};
		private static readonly ShaderTagId[] LegacyShaderPassIds = ShaderPassDefinition.LegacyIds;
		
		private readonly ShadowSettings _directionalShadowSettings;

		// Unity URP shader property IDs
        private static readonly int ScaledScreenParamsID = Shader.PropertyToID("_ScaledScreenParams");
        private static readonly int WorldSpaceCameraPosID = Shader.PropertyToID("_WorldSpaceCameraPos");
        // private static readonly int ZBufferParamsID = Shader.PropertyToID("_ZBufferParams");
        private static readonly int OrthoParamsID = Shader.PropertyToID("unity_OrthoParams");
        private static readonly int ScreenParamsID = Shader.PropertyToID("_ScreenParams");
        
        
		private static readonly int CameraFrameBufferID = Shader.PropertyToID("_CameraFrameBuffer");
		private static readonly int BackBufferID = Shader.PropertyToID("_PostFXBackBuffer");
		private static readonly int CompositionLayerID = Shader.PropertyToID("_CompositionLayer");

		private static readonly int NormalBufferID = Shader.PropertyToID("_CameraNormalsTexture");
		private static readonly int DepthBufferID = Shader.PropertyToID("_CameraDepthTexture");
	
		private readonly List<ShadowedLightInfo> _shadowedLightInfos = new((int) MaxShadowedLightCount);

		private readonly PostFXSettings _defaultPostFXSettings;
		
		public CompositorSettings CompositorSettings { get; set; }

		// Only for debugging
		public ShaderPass ShaderPassOverride
		{
			get => _shaderPassOverride;
			set
			{
				_shaderPassOverride = value;
				_overrideShaderPassIds = new[] {value.ToTagID()};
			}
		}
		private ShaderPass _shaderPassOverride = ShaderPass.None;
		private ShaderTagId[] _overrideShaderPassIds = {};
		
		// Align indices with ShadowFilter enum
		private static readonly GlobalKeyword[] ShadowFilterKeywords = {
			GlobalKeyword.Create("_SHADOW_PCF2x2"),
			GlobalKeyword.Create("_SHADOW_PCF3x3"),
			GlobalKeyword.Create("_SHADOW_PCF5x5"),
			GlobalKeyword.Create("_SHADOW_PCF7x7"),
		};
		

	
		private readonly CommandBuffer _buffer = new()
		{
			name = "Setup and Clear",
		};

		private string[] _bufferNames = null;
		private string[] BufferNames
		{
			get
			{
				if (_bufferNames == null)
				{
					_bufferNames = new string[MaxCameraCount];
					for (int i = 0; i < MaxCameraCount; i++)
					{
						_bufferNames[i] = $"Camera {i}";
					}
				}
				return _bufferNames;
			}
		}
		
		private PostFXStack _defaultPostFXStack;
		private Compositor _compositor;


		public CameraRenderer(
			ShadowSettings directionalShadowSettings,
			PostFXSettings postFXSettings,
			CompositorSettings compositorSettings)
		{
			_directionalShadowSettings = directionalShadowSettings;
			_defaultPostFXSettings = postFXSettings;
			CompositorSettings = compositorSettings;
		}

		public void Render(ScriptableRenderContext context, Camera[] cameras)
		{
			if(cameras.Length > MaxCameraCount)
			{
				Debug.LogError($"Too many cameras in a frame! Max: {MaxCameraCount}, Actual: {cameras.Length}");
				return;
			}
			
			InitializePostFX();
			InitializeCompositor();
			
			for (int i = 0; i < cameras.Length; i++)
			{
				bool isFirstCamera = (i == 0);
				CustomCameraConfig cameraConfig = GetCustomCameraConfig(cameras[i]);
				cameraConfig.Initialize(_defaultPostFXStack);
				PostFXStack postFXStack = cameraConfig.PostFX;
				using (new SampleScope(BufferNames[i], context))
				{
					Render(context, cameras[i], postFXStack, !isFirstCamera, cameraConfig.superSampleScale);
				}
				context.Submit();
			}
		}
		
		private CustomCameraConfig GetCustomCameraConfig(Camera camera)
		{
#if UNITY_EDITOR
			// The preview panel on inspector don't have 'Camera.main' set
			if (camera != Camera.main && Camera.main != null)
			{
				// Use the main camera's settings for the preview camera in editor 
				// (its camera.cameraType is CameraType.Game for some reason)
				if(!Application.isPlaying && camera.name == "Preview Camera")
				{
					return GetCustomCameraConfig(Camera.main);
				}
				// Use the main camera's settings for viewport camera in editor scene view 
				if (camera.cameraType is CameraType.SceneView or CameraType.Preview)
				{
					return GetCustomCameraConfig(Camera.main);
				}
			}	
#endif
			
			return CustomCameraConfig.GetOrCreateCustomConfig(camera);
		}
		
		
		private void InitializePostFX()
		{
			_defaultPostFXStack ??= new PostFXStack(_defaultPostFXSettings);
		}
		

		// TODO: Do we really need this Reinitialize method?
		private void InitializeCompositor()
		{
			RenderTargetIdentifier finalTarget = BuiltinRenderTextureType.CameraTarget;
			if (_compositor == null)
			{
				_compositor = new Compositor(CompositorSettings, CompositionLayerID, finalTarget);
			}
			else
			{
				_compositor.Reinitialize(CompositorSettings, CompositionLayerID, finalTarget);
			}
		}

		private void Render(ScriptableRenderContext context, Camera camera,
							PostFXStack postFXStack, bool needComposition,
							int superSampleScale)
		{
			SetupUnityVariables();
			
			
#if UNITY_EDITOR
			EmitUIGeometry();
#endif
			
			#region Main_Rendering_Logic

			CullingResults cullingResults = Cull();

			using (new SampleScope("DepthNormal", context))
			{
				DrawDepthNormal();
			}
			
			DrawShadowMap();
			
			SetupShadowMapForShaders();
			SetupLightsForShaders();
			SetupKeywords();

			using (new SampleScope("Main Rendering", context))
			{
				SetupCameraRendering();
				DrawOpaqueGeometry();
				DrawSkybox();
				DrawTransparentGeometry();
			}


			RenderTargetIdentifier afterPostFXTarget;
			using (new SampleScope("PostFX", context))
			{
				afterPostFXTarget = DrawPostFX();
			}
			
			using (new SampleScope("SSAA", context))
			{
				SSAADownSample(afterPostFXTarget);
			}

			// First camera just render to cameraTarget so we don't need to composite
			if (needComposition)
			{
				CompositeToFinalTarget();
			}
			
			#endregion Main_Rendering_Logic
			
			DrawUnsupportedShaders();

			using (new SampleScope("Gizmos", context))
			{
				DrawGizmosPreFX();
				DrawGizmosPostFX();
			}

			ReleaseCameraBuffers();
			ReleaseShadowMap();
			ReleaseDepthNormal();
// 			



			// ===== Local Functions =====

			void SetupUnityVariables()
			{
				Vector4 orthoParams = new Vector4(
					camera.orthographicSize * camera.aspect, camera.orthographicSize, 0.0f, camera.orthographic ? 1.0f : 0.0f);

				// Camera and Screen variables as described in https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
				_buffer.SetGlobalVector(WorldSpaceCameraPosID,
					camera.worldToCameraMatrix.GetColumn(3));
				_buffer.SetGlobalVector(ScreenParamsID,
					new Vector4(camera.pixelWidth, camera.pixelHeight, 
						1.0f + 1.0f / camera.pixelWidth, 1.0f + 1.0f / camera.pixelHeight));
				_buffer.SetGlobalVector(ScaledScreenParamsID,
					new Vector4(camera.pixelWidth * superSampleScale, camera.pixelHeight * superSampleScale, 
						1.0f + 1.0f / (camera.pixelWidth * superSampleScale), 1.0f + 1.0f / (camera.pixelHeight * superSampleScale)));
				
				// NOTE: Not sure exactly how, but _ZBufferParams seems to be set by Unity anyway
				// _buffer.SetGlobalVector(ZBufferParamsID,
				// 	zBufferParams);
				
				_buffer.SetGlobalVector(OrthoParamsID,
					orthoParams);
				
				context.ExecuteAndClearBuffer(_buffer);
			}



			
			void EmitUIGeometry()
			{
#if UNITY_EDITOR
				if(camera.cameraType == CameraType.SceneView)
				{
					ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
				}
#endif
			}
			
			void DrawDepthNormal()
			{
				context.SetupCameraProperties(camera);

				SRPUtils.GetTemporaryRTForGraphicsFormat(NormalBufferID, _buffer, camera, superSampleScale,
					format: GraphicsFormat.R8G8B8A8_SNorm);
				SRPUtils.GetTemporaryRTFor(DepthBufferID, _buffer, camera, superSampleScale,
					format: RenderTextureFormat.Depth);

				_buffer.SetRenderTarget(
					NormalBufferID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
					DepthBufferID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
				_buffer.ClearRenderTarget(
					true, true, Color.clear);
				
				context.ExecuteAndClearBuffer(_buffer);
				
				var sortingSettings = new SortingSettings(camera)
				{
					criteria = SortingCriteria.CommonOpaque,
				};
				var drawingSettings = SRPUtils.CreateDrawingSettings(
					ShaderPass.DepthNormal.ToTagID(), ref sortingSettings);
				var filteringSettings = new FilteringSettings(RenderQueueRange.all);
				
				context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
				//
				//
				// _buffer.SetRenderTarget(
				// 	DepthBufferID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
				// _buffer.ClearRenderTarget(
				// 	true, true, Color.clear);
				//
				// context.ExecuteAndClearBuffer(_buffer);
				//
				// var sortingSettings2 = new SortingSettings(camera)
				// {
				// 	criteria = SortingCriteria.CommonOpaque,
				// };
				// var drawingSettings2 = SRPUtils.CreateDrawingSettings(
				// 	ShaderPass.Depth.ToTagID(), ref sortingSettings2);
				// var filteringSettings2 = new FilteringSettings(RenderQueueRange.all);
				//
				// context.DrawRenderers(cullingResults, ref drawingSettings2, ref filteringSettings2);
			}
			
			void ReleaseDepthNormal()
			{
				_buffer.ReleaseTemporaryRT(NormalBufferID);
				_buffer.ReleaseTemporaryRT(DepthBufferID);
				context.ExecuteAndClearBuffer(_buffer);
			}
			
			void DrawShadowMap()
			{
				_shadowedLightInfos.Clear();	
				ShadowValet.CollectionShadowedLightInfos(cullingResults, MaxShadowedLightCount, _shadowedLightInfos);
				ShadowValet.CalculateTiles(_shadowedLightInfos, _directionalShadowSettings);
				if (_shadowedLightInfos.Count >= 0)
				{
					ShadowValet.DrawDirectionalShadowMap(context, camera, cullingResults, _shadowedLightInfos, _directionalShadowSettings);
				}
				else
				{
					ShadowValet.CreateDummyShadowMap(context);
				}
			}
			
			void ReleaseShadowMap()
			{
				ShadowValet.ReleaseShadowMap(context);
			}
			
			void SetupShadowMapForShaders()
			{
				ShadowValet.SetupShadowMapForShaders(
					context,
					cullingResults,
					_shadowedLightInfos,
					_directionalShadowSettings);
			}


			void SetupLightsForShaders()
			{
				LightValet.SetupLightsForShaders(context, cullingResults);
			}

			void SetupCameraRendering()
			{
				context.SetupCameraProperties(camera);
			

				SRPUtils.GetTemporaryRTFor(CameraFrameBufferID, _buffer, camera, superSampleScale);
				SRPUtils.GetTemporaryRTFor(BackBufferID, _buffer, camera, superSampleScale);
				SRPUtils.GetTemporaryRTFor(CompositionLayerID, _buffer, camera, superSampleScale);

				_buffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
				_buffer.SetRenderTarget(CameraFrameBufferID,
					RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
				
				CameraClearFlags flags = camera.clearFlags;
				if (needComposition)
				{
					// The clear logic is handled by the compositor later, we always clear buffer here
					_buffer.ClearRenderTarget(
						true, true,
						new Color(0, 0, 0, 0));
				}
				else
				{
					_buffer.ClearRenderTarget(
						flags <= CameraClearFlags.Depth,
						flags <= CameraClearFlags.Color,
						flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.magenta);
				}
				
				context.ExecuteAndClearBuffer(_buffer);
			}
			
			void SetupKeywords()
			{
				// Shadow PCF happens when sampling the shadow map, not when writing to it.
				// So we setup the shadow PCF keyword here.
				for(int i = 0; i < ShadowFilterKeywords.Length; i++)
				{
					_buffer.DisableKeyword(ShadowFilterKeywords[i]);
				}
				_buffer.EnableKeyword(ShadowFilterKeywords[(int) _directionalShadowSettings.filter]);
				// Shader.EnableKeyword("_SHADOW_PCF7x7");
				context.ExecuteAndClearBuffer(_buffer);
			}

			void DrawSkybox()
			{
				context.DrawSkybox(camera);
			}
			
			void DrawOpaqueGeometry()
			{
				var sortingSettings = new SortingSettings(camera)
				{
					criteria = SortingCriteria.CommonOpaque,
				};
				var drawingSettings = SRPUtils.CreateDrawingSettings(
					ShaderPassOverride == ShaderPass.None ? OpaqueShaderPassIds : _overrideShaderPassIds,
					ref sortingSettings);
				var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
				
				context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
			}

			void DrawTransparentGeometry()
			{
				var sortingSettings = new SortingSettings(camera)
				{
					criteria = SortingCriteria.CommonTransparent,
				};
				var drawingSettings = SRPUtils.CreateDrawingSettings(
					ShaderPassOverride == ShaderPass.None ? TransparentShaderPassIds : _overrideShaderPassIds,
					ref sortingSettings);
				var filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
				
				context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
			}

			void DrawUnsupportedShaders()
			{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				var sortingSettings = new SortingSettings(camera);
				var drawingSettings = SRPUtils.CreateDrawingSettings(
					LegacyShaderPassIds, ref sortingSettings);
				for(int i = 1; i < ShaderPassDefinition.LegacyIds.Length; i++)
				{
					drawingSettings.SetShaderPassName(i, ShaderPassDefinition.LegacyIds[i]);
				}

				drawingSettings.overrideMaterial = GlobalResources.UnsupportedMaterial;
				var filteringSettings = FilteringSettings.defaultValue;
				
				context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
#endif
			}

			RenderTargetIdentifier DrawPostFX()
			{
				return postFXStack.DrawPostFX(context, camera,
					CameraFrameBufferID,
					BackBufferID);
			}

			void SSAADownSample(RenderTargetIdentifier source)
			{
				SSAAValet.SSAADownSample(context, camera,
				source,
					needComposition ? CompositionLayerID : BuiltinRenderTextureType.CameraTarget,
				superSampleScale);
			}

			void CompositeToFinalTarget()
			{
				_compositor.Composite(context, camera);
			}
			
			void DrawGizmosPreFX()
			{
#if UNITY_EDITOR
				if (Handles.ShouldRenderGizmos())
				{
					context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
				}
#endif
			}

			void DrawGizmosPostFX()
			{
#if UNITY_EDITOR
				if (Handles.ShouldRenderGizmos())
				{
					context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
				}
#endif
			}

			void ReleaseCameraBuffers()
			{
				_buffer.ReleaseTemporaryRT(CameraFrameBufferID);
				_buffer.ReleaseTemporaryRT(BackBufferID);
				_buffer.ReleaseTemporaryRT(CompositionLayerID);
				context.ExecuteAndClearBuffer(_buffer);
			}


			CullingResults Cull()
			{
				bool gotCullingParams = camera.TryGetCullingParameters(out var cullingParameters);
				cullingParameters.shadowDistance = Mathf.Min(_directionalShadowSettings.maxDistance, camera.farClipPlane);
				Assert.IsTrue(gotCullingParams, "Failed to get culling parameters");
				return context.Cull(ref cullingParameters);
			}
			
		}

	}
}