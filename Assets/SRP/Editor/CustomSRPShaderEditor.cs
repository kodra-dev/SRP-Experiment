using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomSRPShaderEditor : ShaderGUI
{
	private MaterialProperty[] _properties;
	private Object[] _materials;
	
	public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		base.OnGUI(materialEditor, properties);
		_properties = properties;
		_materials = materialEditor.targets;
		
		if(GUILayout.Button("Opaque")) { OpaquePreset(); }
		if(GUILayout.Button("Alpha Clip")) { AlphaClipPreset(); }
		if(GUILayout.Button(
			   new GUIContent("Glossy Transparent",
				   "Diffuse is premultiplied with alpha, while specular isn't."))
		   ) { GlossyTransparentPreset(); }
		if(GUILayout.Button("Transparent")) { TransparentPreset(); }
	}

	private void OpaquePreset()
	{
		SetProperty("_SrcBlend", (float)BlendMode.One);
		SetProperty("_DstBlend", (float)BlendMode.Zero);
		SetProperty("_ZWrite", 1);
		SetKeywordProperty("_AlphaClip", "_ALPHA_CLIP", false);
		SetRenderQueue((int)RenderQueue.Geometry);
	}

	private void AlphaClipPreset()
	{
		SetProperty("_SrcBlend", (float)BlendMode.One);
		SetProperty("_DstBlend", (float)BlendMode.Zero);
		SetProperty("_ZWrite", 1);
		SetKeywordProperty("_AlphaClip", "_ALPHA_CLIP", true);
		SetRenderQueue((int)RenderQueue.AlphaTest);
		
	}
	
	private void GlossyTransparentPreset()
	{
		SetProperty("_SrcBlend", (float)BlendMode.One);
		SetProperty("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
		SetProperty("_ZWrite", 0);
		SetKeywordProperty("_AlphaClip", "_ALPHA_CLIP", false);
		SetKeywordProperty("_AlphaOnSpecular", "_ALPHA_ON_SPECULAR", false);
		SetRenderQueue((int)RenderQueue.Transparent);
	}
	
	private void TransparentPreset()
	{
		SetProperty("_SrcBlend", (float)BlendMode.SrcAlpha);
		SetProperty("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
		SetProperty("_ZWrite", 0);
		SetKeywordProperty("_AlphaClip", "_ALPHA_CLIP", false);
		SetKeywordProperty("_AlphaOnSpecular", "_ALPHA_ON_SPECULAR", true);
		SetRenderQueue((int)RenderQueue.Transparent);
	}
	
	private void SetProperty(string name, float value)
	{
		MaterialProperty property = FindProperty(name, _properties);
		if (property != null)
		{
			property.floatValue = value;
		}
	}
	
	private void SetProperty(string name, int value)
	{
		MaterialProperty property = FindProperty(name, _properties);
		if (property != null)
		{
			property.intValue = value;
		}
	}
	
	private void SetKeyword(string keyword, bool enabled)
	{
		foreach(var o in _materials)
		{
			var m = o as Material;
			if (m != null)
			{
				if (enabled)
				{
					m.EnableKeyword(keyword);
				}
				else
				{
					m.DisableKeyword(keyword);
				}
			}
		}
	}
	
	private void SetKeywordProperty(string name, string keyword, bool enabled)
	{
		MaterialProperty property = FindProperty(name, _properties);
		SetProperty(name, enabled ? 1f : 0f);
		SetKeyword(keyword, enabled);
	}
	
	private void SetRenderQueue(int queue)
	{
		foreach(var o in _materials)
		{
			var m = o as Material;
			if (m != null)
			{
				m.renderQueue = queue;
			}
		}
	}
}