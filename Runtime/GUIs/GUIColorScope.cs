using System;
using UnityEngine;

namespace KVD.Utils.GUIs
{
	public readonly struct GUIColorScope : IDisposable
	{
		private readonly Color _oldColor;
		private readonly Color _oldGizmosColor;
		
		public GUIColorScope(Color color, bool withGizmos = false)
		{
			_oldColor       = GUI.color;
			GUI.color       = color;
			_oldGizmosColor = Gizmos.color;
			if (withGizmos)
			{
				Gizmos.color = color;
			}
		}
		
		public void Dispose()
		{
			GUI.color    = _oldColor;
			Gizmos.color = _oldGizmosColor;
		}
	}
}
