using System;
using KVD.Utils.DataStructures;
using UnityEditor;
using UnityEngine;

namespace KVD.Utils.Editor.DataStructures
{
	[CustomPropertyDrawer(typeof(SerializableGuid))]
	public class SerializableGuidDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var value = property.FindPropertyRelative("_value");

			var v0 = value.GetFixedBufferElementAtIndex(0).ulongValue;
			var v1 = value.GetFixedBufferElementAtIndex(1).ulongValue;

			var serializableGuid = new SerializableGuid(v0, v1);

			var stringValue = serializableGuid.ToString();

			EditorGUI.BeginChangeCheck();
			stringValue = EditorGUI.DelayedTextField(position, label, stringValue);
			if (!EditorGUI.EndChangeCheck())
			{
				return;
			}
			if (!Guid.TryParse(stringValue, out var newGuid))
			{
				return;
			}
			var newGuidS = new SerializableGuid(newGuid);
			value.GetFixedBufferElementAtIndex(0).ulongValue = SerializableGuid.EditorAccess.Value0(newGuidS);
			value.GetFixedBufferElementAtIndex(1).ulongValue = SerializableGuid.EditorAccess.Value1(newGuidS);
		}
	}
}
