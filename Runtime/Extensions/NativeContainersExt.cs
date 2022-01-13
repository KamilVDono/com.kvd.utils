using System;
using System.Reflection;
using Unity.Collections;

namespace KVD.Utils.Extensions
{
	public static class NativeContainersExt
	{
		public static Allocator ExtractAllocator<T>(NativeArray<T> array) where T : struct
		{
			return AllocatorObtainHelper<T>.getAllocatorFunc(array);
		}
		
		private static class AllocatorObtainHelper<T> where T : struct
		{
			private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic;
			public static Func<NativeArray<T>, Allocator> getAllocatorFunc = InitProc;
			private static Allocator InitProc(NativeArray<T> array)
			{
				var allocatorField = typeof(NativeArray<T>).GetField("m_AllocatorLabel", FieldFlags)!;
				getAllocatorFunc = a => (Allocator)allocatorField.GetValue(a);
				return getAllocatorFunc(array);
			}
		}
	}
}
