using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using KVD.Utils.Debugging;
using KVD.Utils.Extensions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

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
			_length = length;
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

		public UnsafeArray(T* backingArray, uint length) : this(backingArray, length, Allocator.None)
		{
		}

		UnsafeArray(T* backingArray, uint length, Allocator allocator)
		{
			_length = length;
			_allocator = allocator;
			_array = backingArray;
		}

		public void Dispose()
		{
#if DEBUG || UNITY_EDITOR
			if (_allocator == Allocator.Invalid)
			{
				UnityEngine.Debug.LogError($"Calling Dispose on already Disposed {nameof(UnsafeArray<T>)}");
			}
#endif
			if (_allocator > Allocator.None)
			{
#if TRACK_MEMORY
				UnsafeUtility.FreeTracked(_array, _allocator);
#else
				UnsafeUtility.Free(_array, _allocator);
#endif
			}
			this = default;
		}

		public JobHandle Dispose(JobHandle dependencies)
		{
#if DEBUG || UNITY_EDITOR
			if (_allocator == Allocator.Invalid) {
				UnityEngine.Debug.LogError($"Calling Dispose on already Disposed {nameof(UnsafeArray<T>)}");
			}
#endif
			if (_allocator > Allocator.None)
			{
				var job = new NativeCollectionsExts.DisposeJob
				{
					array = _array,
					allocator = _allocator
				};
				this = default;
				return job.Schedule(dependencies);
			}

			this = default;
			return dependencies;
		}

		public UnsafeEnumerator<T> GetEnumerator()
		{
			return new(_array, _length);
		}

		public void Fill(T value)
		{
			UnsafeUtility.MemCpyReplicate(_array, &value, UnsafeUtility.SizeOf<T>(), (int)_length);
		}

		public UnsafeArray<TU> Move<TU>() where TU : unmanaged
		{
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
			if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<TU>())
			{
				throw new InvalidOperationException($"Types {typeof(T)} and {typeof(TU)} are different sizes - direct reinterpretation is not possible");
			}
#endif
			var result = new UnsafeArray<TU>((TU*)_array, _length, _allocator);
			_array = null;
			return result;
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
			var array = new T[_length];
			var gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
			UnsafeUtility.MemCpy(gcHandle.AddrOfPinnedObject().ToPointer(), Ptr, _length*UnsafeUtility.SizeOf<T>());
			gcHandle.Free();
			return array;
		}

		public NativeAllocation AsAllocation()
		{
			return new NativeAllocation(_array, _allocator);
		}

		public static implicit operator UnsafeSpan<T>(UnsafeArray<T> array)
		{
			return new UnsafeSpan<T>(array._array, array._length);
		}

		public static void Resize(ref UnsafeArray<T> array, uint newLength, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
		{
			var newArray = new UnsafeArray<T>(newLength, array._allocator);
			var copyLength = math.min(array._length, newLength);
			UnsafeUtility.MemCpy(newArray._array, array._array, copyLength*UnsafeUtility.SizeOf<T>());
			array.Dispose();
			array = newArray;

			var clearLength = newLength-copyLength;
			if ((options & NativeArrayOptions.ClearMemory) != NativeArrayOptions.ClearMemory | clearLength < 1)
			{
				return;
			}
			UnsafeUtility.MemClear(newArray._array+copyLength, clearLength*UnsafeUtility.SizeOf<T>());
		}

		// From native array
		[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
		void CheckReinterpretRange<U>(uint sourceIndex) where U : struct
		{
			var bytesSize = _length*UnsafeUtility.SizeOf<T>();
			var bytesStartRange = sourceIndex*UnsafeUtility.SizeOf<T>();
			var bytesEndRange = bytesStartRange+UnsafeUtility.SizeOf<U>();
			if (bytesEndRange > bytesSize)
			{
				throw new ArgumentOutOfRangeException(nameof(sourceIndex),
					"byte range must fall inside container bounds");
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
