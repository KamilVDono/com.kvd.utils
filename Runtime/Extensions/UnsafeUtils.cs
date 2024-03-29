using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KVD.Utils.Extensions
{
	public static class UnsafeUtils
	{
		public static unsafe void Fill<T>(T* destination, T value, int count) where T : unmanaged
		{
			UnsafeUtility.MemCpyReplicate(destination, &value, UnsafeUtility.SizeOf<T>(), count);
		}

		public static unsafe void Resize<T>(ref T* destination, Allocator allocator, int oldCount, int newCount) where T : unmanaged
		{
			var newArray = (T*)UnsafeUtility.Malloc(newCount*UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);
			UnsafeUtility.MemCpy(newArray, destination, oldCount*UnsafeUtility.SizeOf<T>());
			UnsafeUtility.Free(destination, allocator);
			destination = newArray;
		}

		public static unsafe void Resize<T>(ref void* destination, Allocator allocator, int oldCount, int newCount) where T : unmanaged
		{
			var newArray = UnsafeUtility.Malloc(newCount*UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);
			UnsafeUtility.MemCpy(newArray, destination, oldCount*UnsafeUtility.SizeOf<T>());
			UnsafeUtility.Free(destination, allocator);
			destination = newArray;
		}

		public static unsafe void Resize<T, TU>(ref TU* destination, Allocator allocator, int oldCount, int newCount)
			where T : unmanaged where TU : unmanaged
		{
			var newArray = UnsafeUtility.Malloc(newCount*UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);
			UnsafeUtility.MemCpy(newArray, destination, oldCount*UnsafeUtility.SizeOf<T>());
			UnsafeUtility.Free(destination, allocator);
			destination = (TU*)newArray;
		}

		public static unsafe void Resize<T>(ref T* destination, Allocator allocator, int oldCount, int newCount, int elementSize, int elementAlignment)
			where T : unmanaged
		{
			var newArray = UnsafeUtility.Malloc(newCount*elementSize, elementAlignment, allocator);
			UnsafeUtility.MemCpy(newArray, destination, oldCount*elementSize);
			UnsafeUtility.Free(destination, allocator);
			destination = (T*)newArray;
		}

		public static unsafe void Resize(ref void* destination, Allocator allocator, int oldCount, int newCount, int elementSize, int elementAlignment)
		{
			var newArray = UnsafeUtility.Malloc(newCount*elementSize, elementAlignment, allocator);
			UnsafeUtility.MemCpy(newArray, destination, oldCount*elementSize);
			UnsafeUtility.Free(destination, allocator);
			destination = newArray;
		}

		public static unsafe T* AsPtr<T>(ref T value) where T : unmanaged
		{
			return (T*)UnsafeUtility.AddressOf(ref value);
		}
	}
}
