using System.IO;
using UnityEditor;
using UnityEngine;

namespace SRP.Shared.Editor
{
	public static class EditorUtils
	{
		public const string IconPath = "Assets/SRP/Shared/Editor/textures/icons/";

		public static Texture2D LoadIcon(string name)
		{
			return EditorGUIUtility.Load(Path.Join(IconPath, name)) as Texture2D;
		}
		
		public static void DrawUILine(Rect position, Color color, int thickness = 2, int padding = 10)
		{
			Rect r = position;
			r.height = thickness;
			r.y+=padding/2.0f;
			r.x-=2;
			r.width +=6;
			EditorGUI.DrawRect(r, color);
		}
		
		public static void DrawUILine(Color color, int thickness = 2, int padding = 10)
		{
			Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding+thickness));
			r.height = thickness;
			r.y+=padding/2.0f;
			r.x-=2;
			r.width +=6;
			EditorGUI.DrawRect(r, color);
		}
	}
}