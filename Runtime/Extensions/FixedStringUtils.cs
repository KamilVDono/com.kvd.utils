using Unity.Collections;

namespace KVD.Utils.Extensions
{
	public static class FixedStringUtils
	{
		public static FixedString32Bytes ToFixedString32(this string str)
		{
			var result = new FixedString32Bytes();
			result.CopyFromTruncated(str);
			return result;
		}
	}
}
