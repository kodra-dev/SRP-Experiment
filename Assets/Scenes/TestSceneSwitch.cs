using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class TestSceneSwitch : MonoBehaviour
{
    public InspectorButton test = new InspectorButton("Test");
    public string sceneName = "Lit";
    
    public void Test()
    {
        SceneManager.LoadScene(sceneName);
    }
}


[Serializable]
public class InspectorButton
{
    public string function;

    public InspectorButton(string function)
    {
        this.function = function;
    }
}
	
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(InspectorButton))]
public class InspectorButtonDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        string function = property.FindPropertyRelative("function").stringValue;
        position.y += 2;
        position.height = EditorGUIUtility.singleLineHeight;
        if (GUI.Button(position, function))
        {
            Object target = property.serializedObject.targetObject;
            target.GetType().GetMethod(function, 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy
            )?.Invoke(target, null);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight + 4;
    }
}
#endif