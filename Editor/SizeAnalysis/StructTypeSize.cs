//
// -- Code adopted from https://gist.github.com/mzaks/ec261ac853621af8503b73391ebd18f1
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;

#nullable enable

namespace KVD.Utils.Editor.SizeAnalysis
{
	public static class StructTypeSize
	{
		private static readonly ConcurrentDictionary<Type, int> Cache = new();

		public static int GetTypeSize(Type type) => Cache.GetOrAdd(type, static (_, t) => UnsafeUtility.SizeOf(t), type);

		public static int GetPossibleStructSize(Type type, bool fullDecomposition)
		{
			var fields = new List<Type>();
			CollectFields(type, fields, fullDecomposition);
			return GetPossibleStructSize(fields);
		}

		public static void CollectFields(Type type, List<Type> list, bool fullDecomposition)
		{
			if (type.IsPrimitive)
			{
				list.Add(type);
				return;
			}
			var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var field in fields)
			{
				if (field.FieldType == type)
				{
					continue;
				}
				if (field.IsStatic)
				{
					continue;
				}

				if (field.FieldType.IsExplicitLayout)
				{
					list.Add(field.FieldType);
					continue;
				}
				if (field.FieldType.IsPrimitive || field.FieldType.IsEnum)
				{
					list.Add(field.FieldType);
				}
				else if (field.FieldType.IsValueType)
				{
					if (fullDecomposition)
					{
						CollectFields(field.FieldType, list, fullDecomposition);
					}
					else
					{
						list.Add(field.FieldType);
					}
				}
				else
				{
					list.Add(typeof(IntPtr));
				}
			}
		}
		
		private static int GetPossibleStructSize(IEnumerable<Type> fieldTypes)
		{
			var biggestSize = 1;
			var sum         = 0;
			foreach (var fieldType in fieldTypes)
			{
				var fieldSize = GetTypeSize(fieldType);
				sum         += fieldSize;
				biggestSize =  fieldSize > biggestSize ? fieldSize : biggestSize;
			}

			if (sum%biggestSize == 0)
			{
				return sum == 0 ? 1 : sum;
			}
			return sum+(biggestSize - sum%biggestSize);
		}
	}
}
