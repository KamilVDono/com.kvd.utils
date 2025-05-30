using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using KVD.Utils.Debugging;
using Unity.Collections;

namespace KVD.Utils.DataStructures
{
	[DebuggerDisplay("Length = {Length}, IsCreated = {array.IsCreated}")]
	[DebuggerTypeProxy(typeof(OccupiedArray<>.DebugView))]
	public struct OccupiedArray<T> where T : unmanaged
	{
		static readonly T s_dummy = default;
		public const uint InvalidIndex = unchecked((uint)-1);

		public UnsafeArray<T> array;
		public UnsafeBitmask occupied;

		public readonly uint Length => array.Length;
		public readonly int LengthInt => array.LengthInt;
		public readonly bool IsCreated => array.IsCreated;
		public uint LastTakenCount => (uint)(occupied.LastOne()+1);
		public int JobScheduleLength => occupied.LastOne()+1;

		public ref T this[uint index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
#if UNITY_EDITOR || DEBUG
				Assert.IsTrue(occupied[index], "Index is not occupied");
#endif
				return ref array[index];
			}
		}

		public ref T this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
#if UNITY_EDITOR || DEBUG
				Assert.IsTrue(occupied[index], "Index is not occupied");
#endif
				return ref array[index];
			}
		}

		public OccupiedArray(uint length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
		{
			array = new UnsafeArray<T>(length, allocator, options);
			occupied = new UnsafeBitmask(length, allocator);
		}

		public void Dispose()
		{
			array.Dispose();
			occupied.Dispose();
		}

		public readonly bool IsOccupied(uint index)
		{
			return occupied[index];
		}

		public unsafe ref readonly T TryGet(uint index, out bool success)
		{
#if UNITY_EDITOR
			success = occupied.Has(index);
#else
			success = occupied[index];
#endif

			if (success)
			{
				return ref array[index];
			}
			else
			{
				return ref s_dummy;
			}
		}

		public bool TryInsert(in T value, out uint uIndex)
		{
			var index = occupied.FirstZero();
			if (index == -1)
			{
				uIndex = InvalidIndex;
				return false;
			}

			uIndex = (uint)index;
			occupied.Up(uIndex);
			array[uIndex] = value;
			return true;
		}

		public uint Insert(in T value)
		{
			var index = occupied.FirstZero();
			if (index == -1)
			{
				Resize(Length * 2);
				index = occupied.FirstZero();
			}

			var uIndex = (uint)index;
			occupied.Up(uIndex);
			array[uIndex] = value;
			return uIndex;
		}

		public void Release(uint index, bool clear = true)
		{
			occupied.Down(index);
			if (clear)
			{
				array[index] = default;
			}
		}

		public void Resize(uint newLength, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
		{
			UnsafeArray<T>.Resize(ref array, newLength, options);
			occupied.EnsureElementsCapacity(newLength);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly OccupiedEnumerator EnumerateOccupied() => new OccupiedEnumerator(this);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly OccupiedIndexedEnumerator EnumerateOccupiedIndexed() => new OccupiedIndexedEnumerator(this);

		public unsafe ref struct OccupiedEnumerator
		{
			T* _array;
			UnsafeBitmask.OnesEnumerator _onesEnumerator;

			public OccupiedEnumerator(OccupiedArray<T> occupiedArray)
			{
				_array = occupiedArray.array.Ptr;
				_onesEnumerator = occupiedArray.occupied.EnumerateOnes();
			}

			public bool MoveNext()
			{
				return _onesEnumerator.MoveNext();
			}

			public ref T Current => ref _array[_onesEnumerator.Current];

			public OccupiedEnumerator GetEnumerator() => this;
		}

		public unsafe ref struct OccupiedIndexedEnumerator
		{
			T* _array;
			UnsafeBitmask.OnesEnumerator _onesEnumerator;

			public OccupiedIndexedEnumerator(OccupiedArray<T> occupiedArray)
			{
				_array = occupiedArray.array.Ptr;
				_onesEnumerator = occupiedArray.occupied.EnumerateOnes();
			}

			public bool MoveNext()
			{
				return _onesEnumerator.MoveNext();
			}

			public ItemWithIndex Current => new(_array +_onesEnumerator.Current, _onesEnumerator.Current);

			public OccupiedIndexedEnumerator GetEnumerator() => this;

			public readonly ref struct ItemWithIndex
			{
				public readonly T* item;
				public readonly uint index;

				public ref T Value => ref *item;

				public ItemWithIndex(T* item, uint index)
				{
					this.item = item;
					this.index = index;
				}

				public void Deconstruct(out T* item, out uint index)
				{
					item = this.item;
					index = this.index;
				}
			}
		}

		sealed class DebugView
		{
			OccupiedArray<T> _data;

			public DebugView(OccupiedArray<T> data)
			{
				_data = data;
			}

			public DebugItem[] Items
			{
				get
				{
					if (!_data.IsCreated)
					{
						return Array.Empty<DebugItem>();
					}
					var length = _data.Length;
					var items = new DebugItem[length];
					for (var i = 0; i < length; i++)
					{
						items[i] = new DebugItem
						{
							item = _data.array[i],
							occupied = _data.occupied[i]
						};
					}
					return items;
				}
			}

			public struct DebugItem
			{
				public T item;
				public bool occupied;
			}
		}
	}
}
