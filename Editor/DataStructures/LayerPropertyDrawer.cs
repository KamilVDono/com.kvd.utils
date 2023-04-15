using KVD.Utils.DataStructures;
using UnityEditor;
using UnityEngine;

namespace KVD.Utils.Editor.DataStructures
{
	[CustomPropertyDrawer(typeof(Layer))]
	public class LayerPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			position = EditorGUI.PrefixLabel(position, label);
			var layer = property.FindPropertyRelative(nameof(Layer.Value));
			layer.intValue = EditorGUI.LayerField(position, layer.intValue);

			EditorGUI.EndProperty();
		}
	}
}
