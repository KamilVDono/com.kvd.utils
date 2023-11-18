using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KVD.Utils.DataStructures
{
	public unsafe ref struct RemoveOnlyUnsafeList<T> where T : unmanaged
	{
		[NativeDisableUnsafePtrRestriction]
		readonly T* _ptr;
		uint _length;

		public uint Length => _length;
		public unsafe ref T this[int index] => ref _ptr[index];

		public RemoveOnlyUnsafeList(NativeArray<T> array)
		{
			_ptr = (T*)array.GetUnsafePtr();
			_length = (uint)array.Length;
		}

		public void RemoveAtSwapBack(uint index)
		{
			_length--;
			(_ptr[index], _ptr[_length]) = (_ptr[_length], _ptr[index]);
		}

		public void RemoveAtSwapBack<U>(U item) where U : unmanaged, IEquatable<T>
		{
			for (var i = (int)_length-1; i >= 0; i--)
			{
				if (item.Equals(_ptr[i]))
				{
					RemoveAtSwapBack((uint)i);
				}
			}
		}
	}
}
