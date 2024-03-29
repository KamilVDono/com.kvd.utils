using KVD.Utils.DataStructures;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KVD.Utils.Extensions
{
	public static unsafe class UnsafeArrayExt
	{
		public static void Resize<T>(ref UnsafeArray<T> array, uint newLength, NativeArrayOptions options = NativeArrayOptions.ClearMemory) where T : unmanaged
		{
			var newArray = new UnsafeArray<T>(newLength, array.Allocator, NativeArrayOptions.UninitializedMemory);
			var oldLength = array.Length;
			UnsafeUtility.MemCpy(newArray.Ptr, array.Ptr, oldLength * UnsafeUtility.SizeOf<T>());
			array.Dispose();
			array = newArray;
			if (((options & NativeArrayOptions.ClearMemory) != NativeArrayOptions.ClearMemory) & (newLength > oldLength))
			{
				return;
			}
			UnsafeUtility.MemClear(array.Ptr+oldLength, (newLength-oldLength)*UnsafeUtility.SizeOf<T>());
		}

		public static void MoveFromList<T>(NativeList<T> list, Allocator allocator, out UnsafeArray<T> array)
			where T : unmanaged
		{
			array = new UnsafeArray<T>((uint)list.Length, allocator);
			UnsafeUtility.MemCpy(array.Ptr, list.GetUnsafeList()->Ptr, array.Length * UnsafeUtility.SizeOf<T>());
			list.Dispose();
		}

		public static void MoveFromList<T>(UnsafeList<T> list, Allocator allocator, out UnsafeArray<T> array)
			where T : unmanaged
		{
			array = new UnsafeArray<T>((uint)list.Length, allocator);
			UnsafeUtility.MemCpy(array.Ptr, list.Ptr, array.Length * UnsafeUtility.SizeOf<T>());
			list.Dispose();
		}
	}
}
