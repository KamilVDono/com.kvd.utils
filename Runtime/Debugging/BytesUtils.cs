using System;

namespace KVD.Utils.Debugging
{
	public static class BytesUtils
	{
		static readonly string[] Suffixes = new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

		public static string HumanReadableBytes(long byteCount)
		{
			return HumanReadableBytes((ulong)byteCount);
		}

		public static string HumanReadableBytes(float byteCount)
		{
			return HumanReadableBytes((ulong)byteCount);
		}

		public static string HumanReadableBytes(ulong byteCount)
		{
			if (byteCount == 0)
			{
				return "0"+Suffixes[0];
			}
			var place = Convert.ToInt32(Math.Floor(Math.Log(byteCount, 1024)));
			var num = Math.Round(byteCount/Math.Pow(1024, place), 1);
			return $"{num:f1} {Suffixes[place]}";
		}
	}
}
