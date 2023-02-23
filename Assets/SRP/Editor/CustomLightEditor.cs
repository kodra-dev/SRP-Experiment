using System;
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light), typeof(CustomRenderPipelineAsset), true)]
public class CustomLightEditor : LightEditor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		
		if (
			!settings.lightType.hasMultipleDifferentValues &&
			(LightType)settings.lightType.enumValueIndex == LightType.Spot
		)
		{
			settings.DrawInnerAndOuterSpotAngle();
			settings.ApplyModifiedProperties();
		}
		
		var light = target as Light;
		// ReSharper disable once Unity.PerformanceCriticalCodeNullComparison
		if (light != null && light.cullingMask != (((long)1 << 31) - 1)) {
			EditorGUILayout.HelpBox(
				"Culling Mask only affects shadows.",
				MessageType.Warning
			);
		}
	}
}