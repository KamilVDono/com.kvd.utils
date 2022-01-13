using Unity.Mathematics;

namespace KVD.Utils.Extensions
{
	// ReSharper disable once InconsistentNaming
	public static class float2Ext
	{
		public static bool Contains(this float2 range, float value)
		{
			return range.x <= value && value <= range.y;
		}
	}
}
