using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SRP.Runtime
{
	public class ScreenSpaceAOPass : IPostFXPass
	{
		public ScreenSpaceAOSettings Settings { get; }
		public bool IsEnabled => Settings.IsEnabled;

		
		// Constants
		private const string ShaderName = "CustomSRP/PostFX/ScreenSpaceAO";
		
		private static Material _ssaoMaterial;
		public static Material SSAOMaterial
		{
			get
			{
				if (_ssaoMaterial == null)
				{
					Shader shader = Shader.Find(ShaderName);
					Assert.IsNotNull(shader, $"Shader not found: " + ShaderName);
					_ssaoMaterial = new Material(shader);
				}
				return _ssaoMaterial;
			}
		}
		
		private readonly CommandBuffer _buffer = new()
		{
			name = "Post FX ScreenSpaceAOPass",
		};

		private const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";
		private const string k_NormalReconstructionLowKeyword = "_RECONSTRUCT_NORMAL_LOW";
		private const string k_NormalReconstructionMediumKeyword = "_RECONSTRUCT_NORMAL_MEDIUM";
		private const string k_NormalReconstructionHighKeyword = "_RECONSTRUCT_NORMAL_HIGH";
		private const string k_SourceDepthKeyword = "_SOURCE_DEPTH";
		private const string k_SourceDepthNormalsKeyword = "_SOURCE_DEPTH_NORMALS";


		private bool isRendererDeferred => false;

		// Private Variables
		private bool m_SupportsR8RenderTextureFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8);
		private Vector4[] m_CameraTopLeftCorner = new Vector4[2];
		private Vector4[] m_CameraXExtent = new Vector4[2];
		private Vector4[] m_CameraYExtent = new Vector4[2];
		private Vector4[] m_CameraZExtent = new Vector4[2];
		private Matrix4x4[] m_CameraViewProjections = new Matrix4x4[2];
		private RenderTargetIdentifier m_SSAOTexture1Target = new RenderTargetIdentifier(s_SSAOTexture1ID, 0, CubemapFace.Unknown, -1);
		private RenderTargetIdentifier m_SSAOTexture2Target = new RenderTargetIdentifier(s_SSAOTexture2ID, 0, CubemapFace.Unknown, -1);
		private RenderTargetIdentifier m_SSAOTexture3Target = new RenderTargetIdentifier(s_SSAOTexture3ID, 0, CubemapFace.Unknown, -1);
		private RenderTargetIdentifier m_SSAOTextureFinalTarget = new RenderTargetIdentifier(s_SSAOTextureFinalID, 0, CubemapFace.Unknown, -1);
		private RenderTextureDescriptor m_AOPassDescriptor;
		private RenderTextureDescriptor m_BlurPassesDescriptor;
		private RenderTextureDescriptor m_FinalDescriptor;

		// Constants
		private const string k_SSAOTextureName = "_ScreenSpaceOcclusionTexture";
		private const string k_SSAOAmbientOcclusionParamName = "_AmbientOcclusionParam";

		// Statics
		private static readonly int s_BaseMapID = Shader.PropertyToID("_BaseMap");
		private static readonly int s_SSAOParamsID = Shader.PropertyToID("_SSAOParams");
		private static readonly int s_SSAOTexture1ID = Shader.PropertyToID("_SSAO_OcclusionTexture1");
		private static readonly int s_SSAOTexture2ID = Shader.PropertyToID("_SSAO_OcclusionTexture2");
		private static readonly int s_SSAOTexture3ID = Shader.PropertyToID("_SSAO_OcclusionTexture3");
		private static readonly int s_SSAOTextureFinalID = Shader.PropertyToID("_SSAO_OcclusionTexture");
		private static readonly int s_CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
		private static readonly int s_CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");
		private static readonly int s_CameraViewZExtentID = Shader.PropertyToID("_CameraViewZExtent");
		private static readonly int s_ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
		private static readonly int s_CameraViewProjectionsID = Shader.PropertyToID("_CameraViewProjections");
		private static readonly int s_CameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner");
		
		public static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");
		
		public ScreenSpaceAOPass(ScreenSpaceAOSettings settings)
		{
			Settings = settings;
		}

		private void Setup(ScriptableRenderContext context, Camera camera)
		{
			int downsampleDivider = Settings.downsample ? 2 : 1;

			// Update SSAO parameters in the material
			Vector4 ssaoParams = new Vector4(
				Settings.intensity,   // Intensity
				Settings.radius,      // Radius
				1.0f / downsampleDivider,      // Downsampling
				Settings.sampleCount  // Sample count
			);
			_buffer.SetGlobalVector(s_SSAOParamsID, ssaoParams);

#if ENABLE_VR && ENABLE_XR_MODULE
                int eyeCount = renderingData.cameraData.xr.enabled && renderingData.cameraData.xr.singlePassEnabled ? 2 : 1;
#else
			int eyeCount = 1;
#endif
			for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
			{
				Matrix4x4 view = camera.worldToCameraMatrix;
				Matrix4x4 proj = camera.projectionMatrix;
				m_CameraViewProjections[eyeIndex] = proj * view;

				// camera view space without translation, used by SSAO.hlsl ReconstructViewPos() to calculate view vector.
				Matrix4x4 cview = view;
				cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
				Matrix4x4 cviewProj = proj * cview;
				Matrix4x4 cviewProjInv = cviewProj.inverse;

				Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, 1, -1, 1));
				Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1, 1, -1, 1));
				Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, -1, -1, 1));
				Vector4 farCentre = cviewProjInv.MultiplyPoint(new Vector4(0, 0, 1, 1));
				m_CameraTopLeftCorner[eyeIndex] = topLeftCorner;
				m_CameraXExtent[eyeIndex] = topRightCorner - topLeftCorner;
				m_CameraYExtent[eyeIndex] = bottomLeftCorner - topLeftCorner;
				m_CameraZExtent[eyeIndex] = farCentre;
			}

			_buffer.SetGlobalVector(s_ProjectionParams2ID, new Vector4(1.0f / camera.nearClipPlane, 0.0f, 0.0f, 0.0f));
			_buffer.SetGlobalMatrixArray(s_CameraViewProjectionsID, m_CameraViewProjections);
			_buffer.SetGlobalVectorArray(s_CameraViewTopLeftCornerID, m_CameraTopLeftCorner);
			_buffer.SetGlobalVectorArray(s_CameraViewXExtentID, m_CameraXExtent);
			_buffer.SetGlobalVectorArray(s_CameraViewYExtentID, m_CameraYExtent);
			_buffer.SetGlobalVectorArray(s_CameraViewZExtentID, m_CameraZExtent);

			// Update keywords
			CoreUtils.SetKeyword(SSAOMaterial, k_OrthographicCameraKeyword, camera.orthographic);

			ScreenSpaceAOSettings.DepthSource source = this.isRendererDeferred
				? ScreenSpaceAOSettings.DepthSource.DepthNormals
				: Settings.source;

			if (source == ScreenSpaceAOSettings.DepthSource.Depth)
			{
				switch (Settings.normalSamples)
				{
					case ScreenSpaceAOSettings.NormalQuality.Low:
						CoreUtils.SetKeyword(SSAOMaterial, k_NormalReconstructionLowKeyword, true);
						CoreUtils.SetKeyword(SSAOMaterial, k_NormalReconstructionMediumKeyword, false);
						CoreUtils.SetKeyword(SSAOMaterial, k_NormalReconstructionHighKeyword, false);
						break;
					case ScreenSpaceAOSettings.NormalQuality.Medium:
						CoreUtils.SetKeyword(SSAOMaterial, k_NormalReconstructionLowKeyword, false);
						CoreUtils.SetKeyword(SSAOMaterial, k_NormalReconstructionMediumKeyword, true);
						CoreUtils.SetKeyword(SSAOMaterial, k_NormalReconstructionHighKeyword, false);
						break;
					case ScreenSpaceAOSettings.NormalQuality.High:
						CoreUtils.SetKeyword(SSAOMaterial, k_NormalReconstructionLowKeyword, false);
						CoreUtils.SetKeyword(SSAOMaterial, k_NormalReconstructionMediumKeyword, false);
						CoreUtils.SetKeyword(SSAOMaterial, k_NormalReconstructionHighKeyword, true);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			switch (source)
			{
				case ScreenSpaceAOSettings.DepthSource.DepthNormals:
					CoreUtils.SetKeyword(SSAOMaterial, k_SourceDepthKeyword, false);
					CoreUtils.SetKeyword(SSAOMaterial, k_SourceDepthNormalsKeyword, true);
					break;
				default:
					CoreUtils.SetKeyword(SSAOMaterial, k_SourceDepthKeyword, true);
					CoreUtils.SetKeyword(SSAOMaterial, k_SourceDepthNormalsKeyword, false);
					break;
			}

			// Set up the descriptors
			// RenderTextureDescriptor descriptor = cameraTargetDescriptor;
			// descriptor.msaaSamples = 1;
			// descriptor.depthBufferBits = 0;

			Vector2Int textureSize = CustomCameraConfig.GetSuperSampledSize(camera);
			m_AOPassDescriptor = new RenderTextureDescriptor(
				width: textureSize.x, height: textureSize.y,
				colorFormat: RenderTextureFormat.ARGB32,
				depthBufferBits: 0
			);
			m_AOPassDescriptor.width /= downsampleDivider;
			m_AOPassDescriptor.height /= downsampleDivider;
			m_AOPassDescriptor.colorFormat = RenderTextureFormat.ARGB32;

			m_BlurPassesDescriptor = new RenderTextureDescriptor(
				width: textureSize.x, height: textureSize.y,
				colorFormat: RenderTextureFormat.ARGB32,
				depthBufferBits: 0
			);

			m_FinalDescriptor = new RenderTextureDescriptor(
				width: textureSize.x, height: textureSize.y,
				colorFormat: RenderTextureFormat.ARGB32,
				depthBufferBits: 0
			);

			// Get temporary render textures
			_buffer.GetTemporaryRT(s_SSAOTexture1ID, m_AOPassDescriptor, FilterMode.Bilinear);
			_buffer.GetTemporaryRT(s_SSAOTexture2ID, m_BlurPassesDescriptor, FilterMode.Bilinear);
			_buffer.GetTemporaryRT(s_SSAOTexture3ID, m_BlurPassesDescriptor, FilterMode.Bilinear);
			_buffer.GetTemporaryRT(s_SSAOTextureFinalID, m_FinalDescriptor, FilterMode.Bilinear);

			// Configure targets and clear color
			// _buffer.SetRenderTarget(s_SSAOTexture2ID,
			// 	RenderBufferLoadAction.Load,
			// 	RenderBufferStoreAction.Store);
			// ConfigureTarget(s_SSAOTexture2ID);
			
			context.ExecuteAndClearBuffer(_buffer);
		}
		
		private void RenderAndSetBaseMap(RenderTargetIdentifier baseMap, RenderTargetIdentifier target, ShaderPasses pass)
		{
			_buffer.SetGlobalTexture(s_BaseMapID, baseMap);
			Render(target, pass);
		}
		
		private void Render(RenderTargetIdentifier target, ShaderPasses pass)
		{
			_buffer.SetRenderTarget(
				target,
				RenderBufferLoadAction.DontCare,
				RenderBufferStoreAction.Store,
				target,
				RenderBufferLoadAction.DontCare,
				RenderBufferStoreAction.DontCare
			);
			_buffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, SSAOMaterial, 0, (int)pass);
		}

		public void Draw(ScriptableRenderContext context, Camera camera, RenderTargetIdentifier src, RenderTargetIdentifier dst)
		{
			Setup(context, camera);
			
			CoreUtils.SetKeyword(_buffer, ShaderKeywordStrings.ScreenSpaceOcclusion, true);
			SetSourceSize(m_AOPassDescriptor);

			Vector4 scaleBiasRt = new Vector4(-1, 1.0f, -1.0f, 1.0f);
			_buffer.SetGlobalVector(Shader.PropertyToID("_ScaleBiasRt"), scaleBiasRt);

			// Execute the SSAO
			Render(m_SSAOTexture1Target, ShaderPasses.AO);
			

			// Execute the Blur Passes
			RenderAndSetBaseMap(m_SSAOTexture1Target, m_SSAOTexture2Target, ShaderPasses.BlurHorizontal);

			SetSourceSize(m_BlurPassesDescriptor);
			RenderAndSetBaseMap(m_SSAOTexture2Target, m_SSAOTexture3Target, ShaderPasses.BlurVertical);
			RenderAndSetBaseMap(m_SSAOTexture3Target, m_SSAOTextureFinalTarget, ShaderPasses.BlurFinal);

			// Set the global SSAO texture and AO Params
			_buffer.SetGlobalTexture(k_SSAOTextureName, m_SSAOTextureFinalTarget);
			_buffer.SetGlobalVector(k_SSAOAmbientOcclusionParamName, new Vector4(0f, 0f, 0f, Settings.directLightingStrength));

			// SetRenderTarget has logic to flip projection matrix when rendering to render texture. Flip the uv to account for that case.
			bool isCameraColorFinalTarget = false;
			bool yflip = !isCameraColorFinalTarget;
			float flipSign = yflip ? -1.0f : 1.0f;
			scaleBiasRt = (flipSign < 0.0f)
				? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
				: new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
			_buffer.SetGlobalVector(Shader.PropertyToID("_ScaleBiasRt"), scaleBiasRt);

			// This implicitly also bind depth attachment. Explicitly binding m_Renderer.cameraDepthTarget does not work.
			_buffer.SetRenderTarget(
				dst,
				RenderBufferLoadAction.Load,
				RenderBufferStoreAction.Store
			);
			_buffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, SSAOMaterial, 0,
			(int)ShaderPasses.AfterOpaque);


			context.ExecuteAndClearBuffer(_buffer);
		}

		public void CleanUp(ScriptableRenderContext context)
		{
			
			CoreUtils.SetKeyword(_buffer, ShaderKeywordStrings.ScreenSpaceOcclusion, false);

			_buffer.ReleaseTemporaryRT(s_SSAOTexture1ID);
			_buffer.ReleaseTemporaryRT(s_SSAOTexture2ID);
			_buffer.ReleaseTemporaryRT(s_SSAOTexture3ID);
			_buffer.ReleaseTemporaryRT(s_SSAOTextureFinalID);
			// Nothing to clean up
		}
		
		private void SetSourceSize(RenderTextureDescriptor desc)
		{
			_buffer.SetGlobalVector(_SourceSize, new Vector4(desc.width, desc.height, 1.0f / desc.width, 1.0f / desc.height));
		}
		
		private enum ShaderPasses
		{
			AO = 0,
			BlurHorizontal = 1,
			BlurVertical = 2,
			BlurFinal = 3,
			AfterOpaque = 4
		}
		
	}
}