using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

#nullable enable

namespace KVD.Utils.Extensions
{
	public static class ReflectionExtension
	{
		/// <summary>
		/// Copy field value from source field(sourceFieldName) to target field(targetFieldName)
		/// with possible object conversion via fieldValueConverter
		/// </summary>
		/// <param name="source">Source object to copy from</param>
		/// <param name="sourceFieldName">Name of field to copy from</param>
		/// <param name="target">Target object to paste to</param>
		/// <param name="targetFieldName">Name of field to paste to</param>
		/// <param name="fieldValueConverter">Function to convert source value to proper target value</param>
		public static void CopyField(object source, string sourceFieldName, object target, string targetFieldName, Func<object, object>? fieldValueConverter = null)
		{
			var sourceField = source.GetType().GetFieldRecursive(sourceFieldName);
			if (sourceField == null)
			{
				Debug.LogWarning($"Object: {source} of type {source.GetType()} has not field named {sourceFieldName}");
				return;
			}

			var targetField = target.GetType().GetFieldRecursive(targetFieldName);
			if (targetField == null)
			{
				Debug.LogWarning($"Object: {target} of type {target.GetType()} has not field named {targetFieldName}");
				return;
			}

			fieldValueConverter ??= (x) => x;

			try
			{
				targetField.SetValue(target, fieldValueConverter(sourceField.GetValue(source)));
			}
			catch (Exception e)
			{
				Debug.LogError($"On CopyField  {sourceFieldName}({source.GetType()}) => {targetFieldName}({target.GetType()}). Error {e.GetType()} with message {e.Message}");
			}
		}

		/// <summary>
		/// Set field value in target object
		/// </summary>
		/// <param name="target">Target object</param>
		/// <param name="fieldName">Field to paste to</param>
		/// <param name="value">New field value</param>
		public static void SetField(object target, string fieldName, object value)
		{
			var field = target.GetType().GetFieldRecursive(fieldName);
			if (field == null)
			{
				Debug.LogWarning($"Object: {target} of type {target.GetType()} has not field named {fieldName}");
				return;
			}
			try
			{
				field.SetValue(target, value);
			}
			catch (Exception e)
			{
				Debug.LogError($"On SetPrivateField  {fieldName}({target.GetType()}) => ({value}). Error {e.GetType()} with message {e.Message}");
			}
		}

		public static void SetFieldOrProperty(object target, string fieldName, object value)
		{
			var field = AllFields(target).FirstOrDefault(m => m.Name.Contains(fieldName));
			if (field == null)
			{
				Debug.LogWarning($"Object: {target} of type {target.GetType()} has not field or property named {fieldName}");
				return;
			}
			try
			{
				field.SetMemberValue(target, value);
			}
			catch (Exception e)
			{
				Debug.LogError($"Can not set {fieldName}({target.GetType()}) => ({value}). Error {e.GetType()} with message {e.Message}");
			}
		}

		/// <summary>
		/// Obtain FieldInfo of field with fieldName
		/// Work even for private fields from base classes
		/// </summary>
		/// <param name="sourceType">Object type</param>
		/// <param name="fieldName">Field name</param>
		/// <returns>If found FieldInfo, otherwise null</returns>
		public static FieldInfo? GetFieldRecursive(this Type sourceType, string fieldName)
		{
			FieldInfo? field;
			var        type = sourceType;
			do
			{
				field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField);
				type  = type.BaseType;

			}
			while (field == null && type != null);

			return field;
		}

		/// <summary>
		/// Obtain PropertyInfo of property with Name
		/// Work even for private properties from base classes
		/// </summary>
		/// <param name="sourceType">Object type</param>
		/// <param name="name">Property name</param>
		/// <returns>If found PropertyInfo, otherwise null</returns>
		public static PropertyInfo? GetPropertyRecursive(this Type sourceType, string name)
		{
			PropertyInfo? property;
			var           currentType = sourceType;
			do
			{
				property = currentType
					.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty)
					.Where(prop => prop.GetMethod != null)
					.Where(f => f.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length < 1)
					.FirstOrDefault(prop => prop.Name == name);
				currentType = currentType.BaseType;
			}
			while (property == null && currentType != null);

			return property;
		}

		public static IEnumerable<T> FieldsOfType<T>(object obj)
		{
			Type type = typeof(T);

			foreach (var field in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
			{
				if (type.IsAssignableFrom(field.FieldType))
				{
					// normal field
					var temp = (T)field.GetValue(obj);
					yield return temp;
				}
				else if (field.FieldType.IsArray && type.IsAssignableFrom(field.FieldType.GetElementType()))
				{
					// array
					foreach (var temp in (T[])field.GetValue(obj))
					{
						yield return temp;
					}
				}
				else if (typeof(IEnumerable<T>).IsAssignableFrom(field.FieldType) && type.IsAssignableFrom(field.FieldType.GenericTypeArguments[0]))
				{
					// enumerable
					foreach (var temp in (IEnumerable<T>)field.GetValue(obj))
					{
						yield return temp;
					}
				}
			}
		}

		public static Dictionary<MemberInfo, object> GetAllFieldsValues(this object source)
		{
			Dictionary<MemberInfo, object> valuesDictionary = new Dictionary<MemberInfo, object>();

			var distinctFields = source.AllFields().GroupBy(f => f.Name).Select(gr => gr.Last());
			foreach (MemberInfo distinctField in distinctFields)
			{
				if (distinctField is FieldInfo fieldInfo)
				{
					valuesDictionary[distinctField] = fieldInfo.GetValue(source);
				}
				else if (distinctField is MethodInfo getterInfo)
				{
					valuesDictionary[distinctField] = getterInfo.Invoke(source, null);
				}
			}

			return valuesDictionary;
		}

		public static IEnumerable<MemberInfo> AllFields(this object source)
		{
			return source.GetType().AllFields();
		}

		public static IEnumerable<MemberInfo> AllFields(this Type type)
		{
			HashSet<MemberInfo> fieldsSet   = new();
			var                 currentType = type;
			while (currentType != null)
			{
				var fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField)
					.Where(f => f.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length < 1);
				fieldsSet.UnionWith(fields);
				var getters = currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty)
					.Where(prop => prop.GetMethod != null)
					.Where(f => f.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length < 1)
					.Select(prop => prop.GetMethod);
				fieldsSet.UnionWith(getters);
				currentType = currentType.BaseType;
			}

			return fieldsSet;
		}

		public static IEnumerable<MethodInfo> AllMethods(this object source)
		{
			return source.GetType().AllMethods();
		}

		public static IEnumerable<MethodInfo> AllMethods(this Type type)
		{
			HashSet<MethodInfo> methodsSet  = new HashSet<MethodInfo>();
			var                 currentType = type;
			while (currentType != null)
			{
				var fields = currentType
					.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.Where(m => !(m.IsSpecialName || AllMethodsIsSetOrGet(m.Name)));
				methodsSet.UnionWith(fields);
				currentType = currentType.BaseType;
			}

			return methodsSet;
		}

		private static bool AllMethodsIsSetOrGet(string input)
		{
			if (input.Length < 4)
			{
				return false;
			}
			var starts = true;
			starts = starts && (input[0] == 'g' || input[0] == 's');
			starts = starts && input[1] == 'e';
			starts = starts && input[2] == 't';
			starts = starts && input[3] == '_';
			return starts;
		}

		public static object? MemberValue(this MemberInfo memberInfo, object relatedObject) =>
			memberInfo switch
			{
				FieldInfo fieldInfo                          => fieldInfo.GetValue(relatedObject),
				PropertyInfo { CanRead: true, } propertyInfo => propertyInfo.GetValue(relatedObject),
				MethodInfo getterInfo                        => getterInfo.Invoke(relatedObject, null),
				_                                            => null,
			};

		/// <summary>
		/// Sets the member's value on the target object.
		/// </summary>
		/// <param name="member">The member.</param>
		/// <param name="target">The target.</param>
		/// <param name="value">The value.</param>
		public static void SetMemberValue(this MemberInfo member, object target, object value)
		{
			if (member.MemberType == MemberTypes.Field)
			{
				((FieldInfo)member).SetValue(target, value);
			}
			else if (member.MemberType == MemberTypes.Property)
			{
				((PropertyInfo)member).SetValue(target, value, null);
			}
		}

		public static bool IsWriteable(this MemberInfo member)
		{
			if (member.MemberType == MemberTypes.Field)
			{
				return true;
			}
			else if (member.MemberType == MemberTypes.Property)
			{
				return ((PropertyInfo)member).CanWrite;
			}

			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Type? PointType(this MemberInfo member)
		{
			if (member.MemberType == MemberTypes.Field)
			{
				return ((FieldInfo)member).FieldType;
			}
			if (member.MemberType == MemberTypes.Property)
			{
				return ((PropertyInfo)member).PropertyType;
			}
			if (member.MemberType == MemberTypes.Method)
			{
				return ((MethodInfo)member).ReturnType;
			}
			return null;
		}

		public static IEnumerable<MethodInfo> AllMethods(Func<MethodInfo, bool> filter)
		{
			var currentDomain = AppDomain.CurrentDomain;
			var assemblies    = currentDomain.GetAssemblies();
			var allTypes      = assemblies.SelectMany(asm => asm.GetTypes());
			var allMethods = allTypes.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
			return allMethods.Where(filter);
		}

		/// <summary>
		/// All classes inheriting from TBaseType
		/// </summary>
		public static IEnumerable<Type> SubClasses<TBaseType>()
		{
			var baseType = typeof(TBaseType);
			return SubClasses(baseType);
		}

		/// <summary>
		/// All classes inheriting from TBaseType
		/// </summary>
		public static IEnumerable<Type> SubClasses(this Type baseType)
		{
#if UNITY_EDITOR
			return UnityEditor.TypeCache.GetTypesDerivedFrom(baseType);
#else
			var assembly = baseType.Assembly;
			return assembly.GetTypes().Where(t => baseType.IsAssignableFrom(t));
#endif
		}

		public static IEnumerable<Type> SubClassesWithBase(this Type baseType)
		{
#if UNITY_EDITOR
			return UnityEditor.TypeCache.GetTypesDerivedFrom(baseType).Append(baseType);
#else
			return baseType.SubClasses().Append(baseType);
#endif
		}

		/// <summary>
		/// All public functions
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static IEnumerable<MethodInfo> PublicFunctions(this Type type)
		{
			return type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
		}
	}
}
