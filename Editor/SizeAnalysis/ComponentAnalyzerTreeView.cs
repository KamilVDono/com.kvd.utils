//
// -- Code adopted from https://gist.github.com/mzaks/ec261ac853621af8503b73391ebd18f1
//

using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

#nullable enable

namespace KVD.Utils.Editor.SizeAnalysis
{
	public class ComponentAnalyzerTreeView : TreeView
	{
		private bool _showOnlyProblematicComponents;
		private bool _showEnums;
		private string _excludeString = "";

		public ComponentAnalyzerTreeView(TreeViewState? treeViewState) : base(treeViewState) => Reload();

		public void ShowOnlyProblematic(bool value)
		{
			if (_showOnlyProblematicComponents == value)
			{
				return;
			}
			_showOnlyProblematicComponents = value;
			Reload();
		}

		public void ShowEnums(bool value)
		{
			if (_showEnums == value)
			{
				return;
			}
			_showEnums = value;
			Reload();
		}

		public void Exclude(string excludeString)
		{
			if (_excludeString == excludeString)
			{
				return;
			}

			_excludeString = excludeString;
			Reload();
		}

		protected override TreeViewItem BuildRoot()
		{
			var root       = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
			var id         = 0;
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			var excludedNames = _excludeString.Split(',');

			foreach (var assembly in assemblies)
			{
				if (IsExcluded(assembly.GetName().Name, excludedNames))
				{
					continue;
				}
				var assemblyItem = new TreeViewItem { id = ++id, displayName = assembly.GetName().Name };
				var enums        = new TreeViewItem { id = ++id, depth       = -1, displayName = "Enums" };
				var problems     = 0;
				var fields       = new List<Type>();
				foreach (var type in assembly.DefinedTypes)
				{
					if (IsExcluded(type.Name, excludedNames))
					{
						continue;
					}

					// Enums
					if (_showEnums && type.IsEnum && !type.IsGenericType)
					{
						var enumSize   = StructTypeSize.GetTypeSize(type);
						var valueCount = Enum.GetValues(type).Length;
						if (valueCount <= byte.MaxValue && enumSize > sizeof(byte))
						{
							var componentItem = new TreeViewItem
							{
								id = ++id, displayName = type.Name+" has size "+enumSize+" but can be "+sizeof(byte)+" if you use: \""+type.Name+" : byte\"",
							};
							enums.AddChild(componentItem);
						}
						else if (valueCount <= ushort.MaxValue && enumSize > sizeof(ushort))
						{
							var componentItem = new TreeViewItem
							{
								id = ++id, displayName = type.Name+" has size "+enumSize+" but can be "+sizeof(ushort)+" if you use: \""+type.Name+" : ushort\"",
							};
							enums.AddChild(componentItem);
						}
					}

					// Structs
					if (!(type.IsValueType && !type.IsEnum))
					{
						continue;
					}

					{
						var size = StructTypeSize.GetTypeSize(type);

						var possibleSizeBasicDecomposition = StructTypeSize.GetPossibleStructSize(type, false);
						var possibleSizeFullDecomposition  = StructTypeSize.GetPossibleStructSize(type, true);

						var prefix = (size <= possibleSizeBasicDecomposition ? "✔︎" : "✘️")+
						             (size <= possibleSizeFullDecomposition ? "✔︎" : "✘️");
						if (possibleSizeBasicDecomposition < size || possibleSizeFullDecomposition < size)
						{
							problems++;
						}

						var show = !_showOnlyProblematicComponents ||
						           size > possibleSizeBasicDecomposition ||
						           size > possibleSizeFullDecomposition;

						if (!show)
						{
							continue;
						}

						fields.Clear();
						StructTypeSize.CollectFields(type, fields, false);

						var text = prefix+' '+type.Name+" holds "+fields.Count+" values";
						if (fields.Count > 0)
						{
							text = text+" in "+size+" bytes";
						}
						if (size > possibleSizeBasicDecomposition)
						{
							text = text+", where "+possibleSizeBasicDecomposition+" bytes is possible by rearrange itself";
						}
						if (size > possibleSizeFullDecomposition && possibleSizeFullDecomposition < possibleSizeBasicDecomposition)
						{
							text = text+", where "+possibleSizeFullDecomposition+" bytes is possible by rearrange inner structs";
						}
						var componentItem = new TreeViewItem { id = ++id, displayName = text, };
						assemblyItem.AddChild(componentItem);
					}
				}

				if (problems > 0)
				{
					assemblyItem.displayName = assemblyItem.displayName+'['+problems+']';
				}
				if (enums.hasChildren)
				{
					assemblyItem.AddChild(enums);
				}
				if (assemblyItem.hasChildren)
				{
					root.AddChild(assemblyItem);
				}
			}

			if (!root.hasChildren)
			{
				root.AddChild(new() { id = 1, displayName = "Nothing to display", });
			}

			SetupDepthsFromParentsAndChildren(root);
			return root;
		}

		private static bool IsExcluded(string value, string[] excludedTypes)
		{
			foreach (var exclude in excludedTypes)
			{
				var trimmedExclude = exclude.Trim();
				if (string.IsNullOrWhiteSpace(trimmedExclude))
				{
					continue;
				}
				if (value.StartsWith(trimmedExclude, StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}
	}
}
