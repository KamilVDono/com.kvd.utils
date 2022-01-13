using UnityEngine;

namespace KVD.Utils.Extensions
{
	public static class Vector2Ext
	{
		public static float Lerp(this Vector2 range, float t)
		{
			return Mathf.Lerp(range.x, range.y, t);
		}
	}
}
