using Unity.Mathematics;

namespace KVD.Utils.Extensions
{
	// ReSharper disable once InconsistentNaming
	public static class int2Ext
	{
		public static bool Contains(this int2 range, int value)
		{
			return range.x <= value && value <= range.y;
		}
	}
}
