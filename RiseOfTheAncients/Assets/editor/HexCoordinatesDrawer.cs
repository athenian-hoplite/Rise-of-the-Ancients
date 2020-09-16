﻿using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom property drawer to display HexCell coordinates cleanly in UntiyEditor.
/// </summary>
[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer {

    public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {

        HexCoordinates coordinates = new HexCoordinates(
			property.FindPropertyRelative("x").intValue,
			property.FindPropertyRelative("z").intValue
		);
		
        position = EditorGUI.PrefixLabel(position, label);
		GUI.Label(position, coordinates.ToString());
	}

}