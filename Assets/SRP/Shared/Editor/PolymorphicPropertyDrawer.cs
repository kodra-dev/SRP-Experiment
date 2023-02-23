// https://medium.com/@trepala.aleksander/serializereference-in-unity-b4ee10274f48

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Example;
using SRP.Shared.Attributes;
using SRP.Shared.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SRP.Shared.Editor
{

	[CustomPropertyDrawer(typeof(PolymorphicAttribute))]
	public class PolymorphicPropertyDrawer : PropertyDrawer
	{
		private Type[] _implementations;
		private int _displayIndex = -1;
		private int _userSelectedIndex = -1;
		private object _currentSelecting;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Rect initPos = position;

			position.y += 4;
			EditorUtils.DrawUILine(position, Color.gray, 1, 4);
			position.y += 8;
		
			if (_implementations == null)
			{
				RefreshImplementations();
			}

			Type currentType = property.managedReferenceValue?.GetType();
			if(property.managedReferenceValue == _currentSelecting)
			{
				_displayIndex = _userSelectedIndex;
			}
			else
			{
				_displayIndex = Array.IndexOf(_implementations, currentType);
			}
			
			//select implementation from editor popup
			position.width -= 48;
			position.height = EditorGUIUtility.singleLineHeight;
			int newSelected = EditorGUI.Popup(
				position,
				$"Implementation",
				_displayIndex,
				_implementations.Select(impl => impl.Name).ToArray());
			if(newSelected != _displayIndex)
			{
				_currentSelecting = property.managedReferenceValue;
				// Because all items in a list share the same PolymorphicPropertyDrawer
				// We need to store the user selected index in a separate variable
				// And only update it when the selection is changed
				// Otherwise the default return value (=_displayIndex) of one item
				// will override user selection of other items
				_userSelectedIndex = newSelected;
			}

			position.x += position.width + 6;
			position.width = 24;

			Texture2D refresh = EditorUtils.LoadIcon("refresh.png");
			Texture2D create = EditorUtils.LoadIcon("create.png");

			if (GUI.Button(position, refresh, EditorStyles.iconButton))
			{
				RefreshImplementations();
				_currentSelecting = null;
				_userSelectedIndex = -1;
			}

			position.x += position.width;
			if (GUI.Button(position, create, EditorStyles.iconButton))
			{
				property.managedReferenceValue = Activator.CreateInstance(_implementations[newSelected]);
				_currentSelecting = null;
				_userSelectedIndex = -1;
			}

			position = initPos;
			position.y += EditorGUIUtility.singleLineHeight + 12;
			position.height = EditorGUI.GetPropertyHeight(property);
			if (property.managedReferenceValue != null)
			{
				EditorGUI.PropertyField(position, property, new GUIContent(property.name), true);
			}
			else
			{
				EditorGUI.LabelField(position, property.name, "NULL");
			}
		
		
			void RefreshImplementations()
			{
				_implementations = TypeUtils.GetImplementations(((attribute as PolymorphicAttribute)!).FieldType);
			}

			position.y += position.height + 4;
			position.height = 4;
			EditorUtils.DrawUILine(position, Color.gray, 1, 4);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property)
			       + EditorGUIUtility.singleLineHeight
			       + 20;
		}
	}

}