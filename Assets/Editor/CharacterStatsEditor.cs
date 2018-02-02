using UnityEditor;
using UnityEngine;
using System.Collections;
 
[CustomEditor(typeof(CharacterStats))]
public class CharacterStatsEditor : Editor
{
	CharacterStats m_Instance;
	PropertyField[] m_fields;
 
	public void OnEnable()
	{
		m_Instance = target as CharacterStats;
		m_fields = ExposeProperties.GetProperties(m_Instance);
	}
 
	public override void OnInspectorGUI()
	{
		if (m_Instance == null)
			return;
		this.DrawDefaultInspector();
		ExposeProperties.Expose(m_fields);
		// loads the stat set dictionary from the lists to facilitate serialization
		m_Instance.loadStatSet();
		// now begin the custom layout for our inspector
		var emptyOptions = new GUILayoutOption[0];
		EditorGUILayout.BeginVertical(emptyOptions);
		EditorGUILayout.BeginHorizontal(emptyOptions);
		// Going to Test by trying to find a way to get all 3 stat properties into 1 line.
		// LEFT OFF IN THIS TUTORIAL https://unity3d.com/learn/tutorials/topics/interface-essentials/property-drawers-custom-inspectors
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
	}
}