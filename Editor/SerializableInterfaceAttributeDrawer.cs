using KVD.Utils.Attributes;
using UnityEditor;
using UnityEngine;

namespace KVD.Utils.Editor
{
	[CustomPropertyDrawer(typeof(SerializableInterfaceAttribute))]
	public class SerializableInterfaceAttributeDrawer : PropertyDrawer
	{
		// Draw the property inside the given rect
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// First get the attribute since it contains the range for the slider
			if (attribute is not SerializableInterfaceAttribute serializableInterfaceAttribute)
			{
				EditorGUI.LabelField(position, label.text, $"Can not extract {nameof(SerializableInterfaceAttribute)}");
				return;
			}

			var fieldObject = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(MonoBehaviour), true);
			if (serializableInterfaceAttribute.InterfaceType.IsInstanceOfType(fieldObject))
			{
				property.objectReferenceValue = fieldObject;
			}
			else
			{
				property.objectReferenceValue = null;
			}
		}
	}
}
