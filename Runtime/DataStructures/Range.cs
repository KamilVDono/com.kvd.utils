using System;

namespace KVD.Utils.DataStructures
{
	[Serializable]
	public struct Range<T> where T : struct, IComparable<T>
	{
		public T min;
		public T max;

		public Range(T min, T max)
		{
			this.min = min;
			this.max = max;
		}

		public bool Contains(T value)
		{
			return min.CompareTo(value) <= 0 && max.CompareTo(value) >= 0;
		}
	}
}
