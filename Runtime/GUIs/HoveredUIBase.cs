using System.Collections.Generic;
using UnityEngine;

namespace KVD.Utils.GUIs
{
	public abstract class HoveredUIBase : MonoBehaviour
	{
		protected static readonly HashSet<HoveredUIBase> HoveredUIs = new();
		public static bool IsPointerOverUI => HoveredUIs.Count > 0;
	}
}
