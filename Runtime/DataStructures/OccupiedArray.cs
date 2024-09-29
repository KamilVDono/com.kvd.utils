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

		public bool TryInsert(T value, out uint uSlot)
		{
			var index = occupied.FirstZero();
			if (index == -1)
			{
				uSlot = InvalidIndex;
				return false;
			}

			uSlot = (uint)index;
			occupied.Up(uSlot);
			array[uSlot] = value;
			return true;
		}

		public void Release(uint index, bool clear = true)
		{
			occupied.Down(index);
			if (clear)
			{
				array[index] = default;
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
