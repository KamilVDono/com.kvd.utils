using Unity.Collections;

namespace NativeCollectionsExposed
{
	public static class NativeCollectionsExposed
	{
		public static Allocator ExtractAllocator<T>(this NativeArray<T> array) where T : unmanaged
		{
			return array.m_AllocatorLabel;
		}
	}
}
