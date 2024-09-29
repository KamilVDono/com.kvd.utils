using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using KVD.Utils.Debugging;
using KVD.Utils.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace KVD.Utils.DataStructures
{
	[DebuggerDisplay("Length = {Length}, IsCreated = {IsCreated}")]
	[DebuggerTypeProxy(typeof(UnsafeArray<>.UnsafeArrayTDebugView))]
	public unsafe struct UnsafeArray<T> where T : unmanaged
	{
		public static readonly UnsafeArray<T> Empty = default;

		[NativeDisableUnsafePtrRestriction] T* _array;
		readonly uint _length;
		readonly Allocator _allocator;

		public readonly uint Length => _length;
		public readonly int LengthInt => (int)_length;
		public readonly bool IsCreated => _array != null;

		public ref T this[uint index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
#if UNITY_EDITOR || DEBUG
				Assert.ArrayIndex(index, _length);
#endif
				return ref *(_array+index);
			}
		}

		public ref T this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
#if UNITY_EDITOR || DEBUG
				Assert.ArrayIndex(index, _length);
#endif
				return ref *(_array+index);
			}
		}

		public T* Ptr => _array;
		public Allocator Allocator => _allocator;

		public static UnsafeArray<T> Move(ref UnsafeList<T> list, Allocator allocator)
		{
			var array = new UnsafeArray<T>((uint)list.Length, allocator, NativeArrayOptions.UninitializedMemory);
			UnsafeUtility.MemCpy(array.Ptr, list.Ptr, list.Length*UnsafeUtility.SizeOf<T>());
			list.Dispose();
			return array;
		}

		public UnsafeArray(uint length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
		{
			_length    = length;
			_allocator = allocator;
#if TRACK_MEMORY
			_array =
 (T*)UnsafeUtility.MallocTracked(UnsafeUtility.SizeOf<T>() * _length, UnsafeUtility.AlignOf<T>(), _allocator, 1);
#else
			_array = (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>()*_length, UnsafeUtility.AlignOf<T>(),
				_allocator);
#endif
			if ((options & NativeArrayOptions.ClearMemory) != NativeArrayOptions.ClearMemory)
			{
				return;
			}
			UnsafeUtility.MemClear(_array, this.Length*UnsafeUtility.SizeOf<T>());
		}

		public UnsafeArray(T* backingArray, uint length)
		{
			_length    = length;
			_allocator = Allocator.None;
			_array     = backingArray;
		}

		public void Dispose()
		{
			if (_allocator > Allocator.None)
			{
#if TRACK_MEMORY
				UnsafeUtility.FreeTracked(_array, _allocator);
#else
				UnsafeUtility.Free(_array, _allocator);
#endif
			}
			_array = null;
		}

		public readonly JobHandle Dispose(JobHandle dependencies)
		{
			if (!IsCreated)
			{
				return dependencies;
			}
			if (_allocator > Allocator.None)
			{
				var job = new NativeCollectionsExt.DisposeJob
				{
					array     = _array,
					allocator = _allocator
				};
				return job.Schedule(dependencies);
			}

			return dependencies;
		}

		public Enumerator GetEnumerator()
		{
			return new(this);
		}

		public static UnsafeArray<T>.Span FromExistingData(T* data, uint length)
		{
			return new UnsafeArray<T>.Span(data, length);
		}

		// From native array
		public ref U ReinterpretLoad<U>(uint sourceIndex) where U : unmanaged
		{
			CheckReinterpretRange<U>(sourceIndex);

			var startPtr = _array+sourceIndex;
			return ref *(U*)startPtr;
		}

		// From native array
		public void ReinterpretStore<U>(uint destIndex, in U data) where U : unmanaged
		{
			CheckReinterpretRange<U>(destIndex);

			var startPtr = _array+destIndex;
			*(U*)startPtr = data;
		}

		public readonly NativeArray<T> AsNativeArray()
		{
			var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(_array, (int)_length,
				Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			var atomicSafetyHandler = AtomicSafetyHandle.GetTempMemoryHandle();
			NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, atomicSafetyHandler);
#endif
			return array;
		}

		public NativeArray<T> ToNativeArray(Allocator allocator)
		{
			return new NativeArray<T>(AsNativeArray(), allocator);
		}

		public T[] ToManagedArray()
		{
			var      array    = new T[_length];
			var gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
			UnsafeUtility.MemCpy(gcHandle.AddrOfPinnedObject().ToPointer(), Ptr, _length*UnsafeUtility.SizeOf<T>());
			gcHandle.Free();
			return array;
		}

		public NativeAllocation AsAllocation()
		{
			return new NativeAllocation(_array, _allocator);
		}

		public static implicit operator UnsafeArray<T>.Span(UnsafeArray<T> array)
		{
			return FromExistingData(array._array, array._length);
		}

		// From native array
		[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
		void CheckReinterpretRange<U>(uint sourceIndex) where U : struct
		{
			var bytesSize       = _length*UnsafeUtility.SizeOf<T>();
			var bytesStartRange = sourceIndex*UnsafeUtility.SizeOf<T>();
			var bytesEndRange   = bytesStartRange+UnsafeUtility.SizeOf<U>();
			if (bytesEndRange > bytesSize)
			{
				throw new ArgumentOutOfRangeException(nameof(sourceIndex),
					"byte range must fall inside container bounds");
			}
		}

		[Serializable]
		public ref struct Enumerator
		{
			readonly T* _array;
			readonly uint _length;
			uint _index;

			internal Enumerator(UnsafeArray<T> array)
			{
				_array  = array._array;
				_length = array._length;
				_index  = uint.MaxValue;
			}

			public void Dispose() {}

			public bool MoveNext()
			{
				return ++_index < _length;
			}

			public ref T Current => ref _array[_index];
		}

		public readonly struct Span
		{
			[NativeDisableUnsafePtrRestriction] readonly T* _array;
			readonly uint _length;

			public T* Ptr => _array;
			public uint Length => _length;
			public int LengthInt => (int)_length;
			public bool IsValid => _array != null;

			public Span(T* array, uint length)
			{
				_array  = array;
				_length = length;
			}

			public ref T this[uint index]
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
#if UNITY_EDITOR || DEBUG
					Assert.ArrayIndex(index, _length);
#endif
					return ref *(_array+index);
				}
			}

			public UnsafeArray<T> AsUnsafeArray()
			{
				return new UnsafeArray<T>(_array, _length);
			}
		}

		sealed class UnsafeArrayTDebugView
		{
			UnsafeArray<T> _data;

			public UnsafeArrayTDebugView(UnsafeArray<T> data)
			{
				_data = data;
			}

			public T[] Items
			{
				get
				{
					if (!_data.IsCreated)
					{
						return Array.Empty<T>();
					}
					return _data.ToManagedArray();
				}
			}
		}
	}
}
