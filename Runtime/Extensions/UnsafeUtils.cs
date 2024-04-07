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

		public static unsafe void Resize<T>(T** destination, Allocator allocator, int oldCount, int newCount) where T : unmanaged
		{
			Resize((void**)destination, allocator, oldCount, newCount, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>());
		}

		public static unsafe void Resize<T>(void** destination, Allocator allocator, int oldCount, int newCount) where T : unmanaged
		{
			Resize(destination, allocator, oldCount, newCount, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>());
		}

		public static unsafe void Resize(void** destination, Allocator allocator, int oldCount, int newCount, int elementSize, int elementAlignment)
		{
			var newArray = UnsafeUtility.Malloc(newCount*elementSize, elementAlignment, allocator);
			UnsafeUtility.MemCpy(newArray, *destination, oldCount*elementSize);
			UnsafeUtility.Free(*destination, allocator);
			*destination = newArray;
		}

		public static unsafe T* AsPtr<T>(ref T value) where T : unmanaged
		{
			return (T*)UnsafeUtility.AddressOf(ref value);
		}
	}
}
