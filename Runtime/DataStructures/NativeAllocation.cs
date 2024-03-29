using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KVD.Utils.DataStructures
{
	public readonly unsafe struct NativeAllocation
	{
		public readonly void* ptr;
		public readonly Allocator allocator;

		public NativeAllocation(void* ptr, Allocator allocator)
		{
			this.ptr = ptr;
			this.allocator = allocator;
		}

		public void Free()
		{
			UnsafeUtility.Free(ptr, allocator);
		}
	}
}
