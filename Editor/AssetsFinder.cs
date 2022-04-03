using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KVD.Utils.Editor
{
	public static class AssetsFinder
	{
		public static IEnumerable<T> Find<T>() where T : Object
		{
			var typeFilter = $"t:{typeof(T).Name}";
			var guids = AssetDatabase.FindAssets(typeFilter);
			return guids.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<T>);
		}
	}
}
