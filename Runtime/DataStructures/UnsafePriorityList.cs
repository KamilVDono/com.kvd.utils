using System;
using KVD.Utils.Debugging;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KVD.Utils.DataStructures
{
	/// <summary>
	/// <remarks>
	/// PriorityRight.CompareTo(PriorityLeft) &gt; 0 means that PriorityRight has a higher priority than PriorityLeft.
	/// The highest priority is at the end of the list, so you can iterate from the end and have cheap remove
	/// </remarks>
	/// </summary>
	public unsafe struct UnsafePriorityList<T, TPriority> where T : unmanaged where TPriority : unmanaged, IComparable<TPriority>
	{
		public UnsafeArray<T> items;
		public UnsafeArray<TPriority> priorities;
		public uint count;

		public readonly bool IsCreated => items.IsCreated && priorities.IsCreated;
		public readonly uint Length => count;
		public readonly uint Capacity => items.Length;

		public UnsafePriorityList(uint capacity, Allocator allocator)
		{
			items = new UnsafeArray<T>(capacity, allocator, NativeArrayOptions.UninitializedMemory);
			priorities = new UnsafeArray<TPriority>(capacity, allocator, NativeArrayOptions.UninitializedMemory);
			count = 0;
		}

		public void Add(T item, TPriority priority)
		{
			EnsureCapacity();

			var index = BinarySearch(priority);
			if (index < 0)
			{
				index = ~index;
			}

			UnsafeUtility.MemCpy((items.Ptr + (index + 1)), (items.Ptr + index), (count - index) * UnsafeUtility.SizeOf<T>());
			UnsafeUtility.MemCpy((priorities.Ptr + (index + 1)), (priorities.Ptr + index), (count - index) * UnsafeUtility.SizeOf<TPriority>());

			items[index] = item;
			priorities[index] = priority;
			count++;
		}

		public bool Remove<TU>(TU item) where TU : unmanaged, IEquatable<T>
		{
			var lastIndex = count - 1;
			for (var i = lastIndex;; --i)
			{
				if (item.Equals(items[i]))
				{
					if (lastIndex == i)
					{
						count--;
						return true;
					}
					else
					{
						UnsafeUtility.MemCpy((items.Ptr + i), (items.Ptr + (i + 1)), (count - i - 1) * UnsafeUtility.SizeOf<T>());
						UnsafeUtility.MemCpy((priorities.Ptr + i), (priorities.Ptr + (i + 1)), (count - i - 1) * UnsafeUtility.SizeOf<TPriority>());
						count--;
						return true;
					}
				}

				if (i == 0)
				{
					return false;
				}
			}
		}

		public T Pop()
		{
			Assert.GreaterThan(count, 0);

			var item = items[count - 1];
			count--;
			return item;
		}

		public void Dispose()
		{
			items.Dispose();
			priorities.Dispose();
		}

		public DisposeScope AutoDispose()
		{
			return new DisposeScope(ref this);
		}

		void EnsureCapacity()
		{
			if (Length != Capacity)
			{
				return;
			}

			var newSize = count * 2;
			UnsafeArray<T>.Resize(ref items, newSize, NativeArrayOptions.UninitializedMemory);
			UnsafeArray<TPriority>.Resize(ref priorities, newSize, NativeArrayOptions.UninitializedMemory);
		}

		int BinarySearch(TPriority priority)
		{
			var low = 0;
			var high = (int)(count - 1);
			while (low <= high)
			{
				var mid = (low + high) / 2;
				var cmp = priorities[mid].CompareTo(priority);
				if (cmp == 0)
				{
					return mid;
				}

				if (cmp < 0)
				{
					low = mid + 1;
				}
				else
				{
					high = mid - 1;
				}
			}

			return ~low;
		}

		public readonly ref struct DisposeScope
		{
			readonly UnsafePriorityList<T, TPriority>* _list;

			public DisposeScope(ref UnsafePriorityList<T, TPriority> list)
			{
				_list = (UnsafePriorityList<T, TPriority>*)UnsafeUtility.AddressOf(ref list);
			}

			public void Dispose()
			{
				_list->Dispose();
			}
		}
	}
}
