using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace KVD.Utils.Extensions
{
	public static class BlobArrayExt
	{
		public static unsafe NativeArray<T> ToNativeArray<T>(this ref BlobArray<T> blobArray, Allocator allocator) where T : unmanaged
		{
			var array = new NativeArray<T>(blobArray.Length, allocator);

			if (array.Length <= 0)
			{
				return array;
			}
			var src = blobArray.GetUnsafePtr();
			UnsafeUtility.MemCpy(array.GetUnsafePtr(), src, array.Length * UnsafeUtility.SizeOf<T>());

			return array;
		}

		public static unsafe NativeArray<(T, int)> ToNativeArrayWithIndex<T>(this ref BlobArray<T> blobArray, Allocator allocator) where T : unmanaged
		{
			var array = new NativeArray<(T, int)>(blobArray.Length, allocator);

			if (array.Length <= 0)
			{
				return array;
			}
			var src = (T*)blobArray.GetUnsafePtr();
			for (var i = 0; i < array.Length; i++)
			{
				array[i] = (src[i], i);
			}

			return array;
		}

		public static string[] ToStringArray(this ref BlobArray<BlobString> blobArray)
		{
			if (blobArray.Length <= 0)
			{
				return Array.Empty<string>();
			}

			var array = new string[blobArray.Length];
			for (var i = 0; i < blobArray.Length; i++)
			{
				array[i] = blobArray[i].ToString();
			}

			return array;
		}
	}
}
