using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using KVD.Utils.Extensions;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace KVD.Utils.Editor
{
	public static class SerializedPropertyExtension
	{
		private const BindingFlags BindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
		private static readonly Regex DataIndexExtractRegex = new(@"(?<=data\[)\d+(?=\])");

		public static T[] ExtractAttributes<T>(this SerializedProperty serializedProperty) where T : Attribute
		{
			var targetFieldInfo = serializedProperty.FieldInfo();
			return (T[])targetFieldInfo.GetCustomAttributes(typeof(T), true);
		}

		public static T? ExtractAttribute<T>(this SerializedProperty serializedProperty) where T : Attribute
		{
			var targetFieldInfo = serializedProperty.FieldInfoArrayAware();
			return (T?)targetFieldInfo.GetCustomAttribute(typeof(T), true);
		}

		public static FieldInfo FieldInfo(this SerializedProperty serializedProperty)
		{
			var targetType = GetParentType(serializedProperty);
			return targetType.GetFields(BindingFlags).First(fi => fi.Name.Equals(serializedProperty.name));
		}

		public static FieldInfo FieldInfoArrayAware(this SerializedProperty serializedProperty)
		{
			var   targetType   = GetParentType(serializedProperty, serializedProperty.name.Equals("data") ? 3 : 1);
			var propertyName = serializedProperty.name.Equals("data") ? serializedProperty.propertyPath.Split('.').SkipLastN(2).Last() : serializedProperty.name;
			return targetType.GetField(propertyName, BindingFlags)!;
		}

		public static float Height(this SerializedProperty serializedProperty)
		{
			var children = serializedProperty.GetChildren();
			return children.Sum(child => EditorGUI.GetPropertyHeight(child, true));
		}

		public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty property)
		{
			property = property.Copy();
			var  nextElement    = property.Copy();
			var hasNextElement = nextElement.NextVisible(false);
			if (!hasNextElement)
			{
				nextElement = null;
			}

			property.NextVisible(true);
			while (true)
			{
				if (SerializedProperty.EqualContents(property, nextElement))
				{
					yield break;
				}

				yield return property;

				var hasNext = property.NextVisible(false);
				if (!hasNext)
				{
					break;
				}
			}
		}

		static Type GetParentType(SerializedProperty serializedProperty, int parentDepth = 1)
		{
			var targetObject     = serializedProperty.serializedObject.targetObject;
			var targetObjectType = targetObject.GetType();
			
			if (serializedProperty.depth <= 0)
			{
				return targetObjectType;
			}
			
			var path        = serializedProperty.propertyPath.Split('.');
			var currentType = targetObjectType;
			var i           = 0;
			while (i < path.Length-parentDepth)
			{
				if (path[i] == "Array")
				{
					i++; //skips "data[x]"
					currentType = (currentType.IsArray ? currentType.GetElementType() : currentType.GetGenericArguments()[0])!;
				}
				else
				{
					var fieldInfo = currentType.GetField(path[i],
						BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance)!;
					currentType = fieldInfo.FieldType;
				}
				i++;
			}
			return currentType;

		}

		public static object GetParentValue(this SerializedProperty serializedProperty)
		{
			return serializedProperty.GetPropertyValue(serializedProperty.name.Equals("data") ? 3 : 1)!;
		}

		/// <summary>
		/// Get value of property (or property parent)
		/// </summary>
		/// <param name="serializedProperty">Property which value from we want</param>
		/// <param name="ofUpper">Set to 1 if want parent class instance, set to 2 if parent parent ... If just property value leave as 0</param>
		/// <returns>Real value of property</returns>
		public static object? GetPropertyValue(this SerializedProperty serializedProperty, int ofUpper = 0)
		{
			var slices       = serializedProperty.propertyPath.Split('.');
			var type         = serializedProperty.serializedObject.targetObject.GetType();
			object? currentValue = serializedProperty.serializedObject.targetObject;

			for (var i = 0; i < slices.Length-ofUpper; i++)
			{
				if (slices[i] == "Array")
				{
					//go to 'data[x]'
					i++;
					// extract x
					var index = int.Parse(DataIndexExtractRegex.Match(slices[i]).Value);

					var currentArray = (IEnumerable)currentValue!;
					var enumerator   = currentArray.GetEnumerator();
					enumerator.MoveNext();

					for (var j = 0; j < index; j++)
					{
						enumerator.MoveNext();
					}

					currentValue = enumerator.Current;

					type = (type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0])!;
				}
				else
				{
					var fieldInfo = type.GetField(slices[i],
						BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance)!;
					currentValue = fieldInfo.GetValue(currentValue!);

					type = fieldInfo.FieldType;
				}
			}

			return currentValue;
		}

		public static Type GetPropertyType(this SerializedProperty serializedProperty)
		{
			var slices = serializedProperty.propertyPath.Split('.');
			var type   = serializedProperty.serializedObject.targetObject.GetType();

			for (var i = 0; i < slices.Length; i++)
			{
				if (slices[i] == "Array")
				{
					i++; //skips "data[x]"
					type = (type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0])!;
				}
				else
				{
					var fieldInfo = type.GetField(slices[i],
						BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance)!;
					type = fieldInfo.FieldType;
				}
			}

			return type;
		}

		public static SerializedProperty FindBackedProperty(this SerializedObject serializedObject, string propertyName)
		{
			var backedName = $"<{propertyName}>k__BackingField";
			return serializedObject.FindProperty(backedName);
		}
		
		public static SerializedProperty FindBackedProperty(this SerializedProperty serializedProperty, string propertyName)
		{
			var backedName = $"<{propertyName}>k__BackingField";
			return serializedProperty.FindPropertyRelative(backedName);
		}

		// === Draw array
		public static void DrawArray(this SerializedProperty list, Action<SerializedProperty> elementDraw)
		{
			DrawArrayHeader(list);
			DrawArrayElements(list, elementDraw);
		}

		public static void DrawArrayHeader(SerializedProperty list)
		{
			EditorGUILayout.BeginHorizontal();
			list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, list.displayName, true, EditorStyles.foldoutHeader);
			EditorGUILayout.LabelField("Size:", GUILayout.Width(55));
			if (GUILayout.Button("-", GUILayout.Width(20)) && list.arraySize > 0)
			{
				list.arraySize--;
			}
			var size = list.FindPropertyRelative("Array.size");
			EditorGUILayout.PropertyField(size, GUIContent.none, GUILayout.Width(50));
			if (GUILayout.Button("+", GUILayout.Width(20)))
			{
				list.arraySize++;
			}
			EditorGUILayout.EndHorizontal();
		}

		public static void DrawArrayElements(SerializedProperty list, Action<SerializedProperty> elementDraw)
		{
			if (!list.isExpanded)
			{
				return;
			}
			EditorGUI.indentLevel += 1;
			for (var i = 0; i < list.arraySize; i++)
			{
				var element = list.GetArrayElementAtIndex(i);
				DrawLine();
				elementDraw(element);
			}
			EditorGUI.indentLevel -= 1;
		}

		private static void DrawLine()
		{
			EditorGUILayout.LabelField("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
			EditorGUI.DrawRect(GUILayoutUtility.GetLastRect(), Color.black);
			EditorGUILayout.Space();
		}
	}
}
