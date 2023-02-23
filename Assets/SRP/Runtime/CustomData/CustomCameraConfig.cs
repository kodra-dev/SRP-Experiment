using System;
using SRP.Runtime;
using SRP.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

// TODO: Instead of passing camera around in the render pipeline, we can pass this config around
// and get the camera from it. (but how to store a reference to camera in this?)
[RequireComponent(typeof(Camera))]
public class CustomCameraConfig : MonoBehaviour
{
	[Range(1, 4)]
	public int superSampleScale = 1;

	public bool overridePostFXSettings;
	public PostFXSettings postFXSettings;
	
	public PostFXStack PostFX { get; private set; }

	
	public void Initialize(PostFXStack defaultPostFXStack)
	{
		if (overridePostFXSettings)
		{
			PostFX ??= new PostFXStack(postFXSettings);
		}
		else
		{
			PostFX = defaultPostFXStack;
		}
	}


	public static CustomCameraConfig GetOrCreateCustomConfig(Camera camera)
	{
		return SceneCache.ObtainComponent<CustomCameraConfig>(
			camera,
			CacheMissAction.GetOrCreateFromGo);
	}
	
	public static Vector2Int GetSuperSampledSize(Camera camera)
	{
		CustomCameraConfig config = GetOrCreateCustomConfig(camera);
		return new Vector2Int(
			camera.pixelWidth * config.superSampleScale,
			camera.pixelHeight * config.superSampleScale);
	}
	
	// OnValidate
	private void OnValidate()
	{
		if (overridePostFXSettings)
		{
			PostFX = new PostFXStack(postFXSettings);
		}
	}
}