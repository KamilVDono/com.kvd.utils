using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;

namespace KVD.Utils.Debugging
{
	public static class Assert
	{
		[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ArrayIndex(uint index, uint length)
		{
			if (index >= length)
			{
				throw new System.IndexOutOfRangeException($"Index {index} is out of range {length}");
			}
		}

		[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ArrayIndex(int index, uint length)
		{
			if (index >= length)
			{
				throw new System.IndexOutOfRangeException($"Index {index} is out of range {length}");
			}
			if (index < 0)
			{
				throw new System.IndexOutOfRangeException($"Index {index} is negative");
			}
		}

		[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void IsTrue(bool value, string message = "")
		{
			if (!value)
			{
				throw new AssertionException("Value is false but expected true", message);
			}
		}

		[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void IsFalse(bool value, string message = "")
		{
			if (value)
			{
				throw new AssertionException("Value is true but expected false", message);
			}
		}
	}
}
