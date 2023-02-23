using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// NOTE: This component can be cached in render pipeline, don't manually destroy it at runtime.
[RequireComponent(typeof(Light))]
public class CustomLightConfig : MonoBehaviour
{
	[SerializeField][ColorUsage(showAlpha:false)]
	private Color shadowColor = Color.black;
	
	public Color ShadowColor => shadowColor;
}
