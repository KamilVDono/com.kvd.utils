using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using KVD.Utils.DataStructures;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace KVD.Utils.Extensions
{
	public static class NativeCollectionsExts
	{
		public static int FindIndexOf<T, U>(this in NativeArray<T> array, U search, int startIndex = 0) where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			for (var i = startIndex; i < array.Length; i++)
			{
				if (search.Equals(array[i]))
				{
					return i;
				}
			}
			return -1;
		}

		public static int FindIndexOf<T, U>(this in UnsafeList<T> array, U search, int startIndex = 0) where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			for (var i = startIndex; i < array.Length; i++)
			{
				if (search.Equals(array[i]))
				{
					return i;
				}
			}
			return -1;
		}

		public static int FindIndexOf<T, U>(this in UnsafeArray<T> array, U search, uint startIndex = 0) where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			for (var i = startIndex; i < array.Length; i++)
			{
				if (search.Equals(array[i]))
				{
					return (int)i;
				}
			}
			return -1;
		}

		public static int FindIndexOf<T, U>(this in UnsafeSpan<T> span, U search, uint startIndex = 0) where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			for (var i = startIndex; i < span.Length; i++)
			{
				if (search.Equals(span[i]))
				{
					return (int)i;
				}
			}
			return -1;
		}

		[BurstCompile]
		public struct FindIndexJob<T, U> : IJob where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			[ReadOnly] public NativeArray<T> array;
			public U search;
			public NativeReference<int> output;

			public void Execute()
			{
				output.Value = -1;
				for (var index = 0; index < array.Length; index++)
				{
					if (search.Equals(array[index]))
					{
						output.Value = index;
						break;
					}
				}
			}
		}

		public static ReverseNativeListSearchIterator<T, U> FindAllIndicesRevers<T, U>(this in NativeList<T> list, U searchElement) where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			return new ReverseNativeListSearchIterator<T, U>(list, searchElement);
		}

		public static ReverseUnsafeListSearchIterator<T, U> FindAllIndicesRevers<T, U>(this in UnsafeList<T> list, U searchElement) where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			return new ReverseUnsafeListSearchIterator<T, U>(list, searchElement);
		}

		public static ReverseUnsafeArraySearchIterator<T, U> FindAllIndicesRevers<T, U>(this in UnsafeArray<T> array, U searchElement) where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			return new ReverseUnsafeArraySearchIterator<T, U>(array, searchElement);
		}

		public static ReverseOccupiedArraySearchIterator<T, U> FindAllIndicesRevers<T, U>(this in OccupiedArray<T> array, U searchElement) where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			return new ReverseOccupiedArraySearchIterator<T, U>(array, searchElement);
		}

		public static void EnsureLength<T>(this ref NativeList<T> list, int length) where T : unmanaged
		{
			if (list.Length < length)
			{
				list.Resize(length, NativeArrayOptions.ClearMemory);
			}
		}

		public static bool RemoveSwapBack<T, U>(this ref UnsafeList<T> list, U value) where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			var index = list.FindIndexOf(value);
			if (index >= 0)
			{
				list.RemoveAtSwapBack(index);
				return true;
			}
			return false;
		}

		public static bool Remove<T, U>(this ref UnsafeList<T> list, U value) where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			var index = list.FindIndexOf(value);
			if (index >= 0)
			{
				list.RemoveAt(index);
				return true;
			}
			return false;
		}

		public static unsafe void Sort<T, U>(in UnsafeSpan<T> span, U comparer) where T : unmanaged where U : IComparer<T>
		{
			NativeSortExtension.Sort(span.Ptr, (int)span.Length, comparer);
		}

		public static unsafe void Sort<T, U>(in UnsafeArray<T> array, U comparer) where T : unmanaged where U : IComparer<T>
		{
			Sort((UnsafeSpan<T>)array, comparer);
		}

		public static unsafe void Sort<T>(in UnsafeSpan<T> span) where T : unmanaged, IComparable<T>
		{
			NativeSortExtension.Sort(span.Ptr, (int)span.Length, new NativeSortExtension.DefaultComparer<T>());
		}

		public static unsafe void Sort<T>(in UnsafeArray<T> array) where T : unmanaged, IComparable<T>
		{
			Sort((UnsafeSpan<T>)array);
		}

		public static unsafe T[] ToArray<T>(this in UnsafeList<T>.ReadOnly list) where T : unmanaged
		{
			var dst = new T[list.Length];
			var gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
			UnsafeUtility.MemCpy(gcHandle.AddrOfPinnedObject().ToPointer(), list.Ptr, list.Length * UnsafeUtility.SizeOf<T>());
			gcHandle.Free();
			return dst;
		}

		public static unsafe UnsafeSpan<T> AsUnsafeSpan<T>(this in UnsafeList<T> list) where T : unmanaged
		{
			return new UnsafeArray<T>(list.Ptr, (uint)list.Length);
		}

		public static unsafe UnsafeSpan<T> AsUnsafeSpan<T>(this in UnsafeList<T>.ReadOnly list) where T : unmanaged
		{
			return new UnsafeArray<T>(list.Ptr, (uint)list.Length);
		}

		public static unsafe ReadOnlySpan<byte> ToByteSpan<T>(this in NativeArray<T> array) where T : unmanaged
		{
			var bytesCount = array.Length * UnsafeUtility.SizeOf<T>();
			return new ReadOnlySpan<byte>(array.GetUnsafePtr(), bytesCount);
		}

		public static unsafe ReadOnlySpan<byte> ToByteSpan<T>(this in NativeList<T> list) where T : unmanaged
		{
			var bytesCount = list.Length * UnsafeUtility.SizeOf<T>();
			return new ReadOnlySpan<byte>(list.GetUnsafePtr(), bytesCount);
		}

		public static unsafe ReadOnlySpan<byte> ToByteSpan<T>(this in UnsafeArray<T> array) where T : unmanaged
		{
			var bytesCount = (int)(array.Length * UnsafeUtility.SizeOf<T>());
			return new ReadOnlySpan<byte>(array.Ptr, bytesCount);
		}

		public static NativeArray<T> AsNativeArray<T>(this UnsafeList<T> list) where T : unmanaged
		{
			return list.AsNativeArray(0, list.Length);
		}

		public static unsafe NativeArray<T> AsNativeArray<T>(this UnsafeList<T> list, int startIndex, int length) where T : unmanaged
		{
			var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(list.Ptr + startIndex, length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			var atomicSafetyHandler = AtomicSafetyHandle.GetTempMemoryHandle();
			NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, atomicSafetyHandler);
#endif
			return array;
		}

		public static UnsafeArray<T> ToUnsafeArray<T>(this UnsafeList<T> list, Allocator allocator) where T : unmanaged
		{
			return list.ToUnsafeArray(allocator, 0, list.Length);
		}

		public static unsafe UnsafeArray<T> ToUnsafeArray<T>(this UnsafeList<T> list, Allocator allocator, int startIndex, int length) where T : unmanaged
		{
			var array = new UnsafeArray<T>((uint)length, allocator, NativeArrayOptions.UninitializedMemory);
			UnsafeUtility.MemCpy(array.Ptr, list.Ptr + startIndex, length * UnsafeUtility.SizeOf<T>());
			return array;
		}

		[BurstDiscard]
		public static void LogErrorIndexIsOutOfRangeForCapacity(int index, int capacity)
		{
			Debug.LogError($"Index {index} is out of range. Capacity = {capacity}");
		}

		[BurstDiscard]
		public static void LogErrorIndexIsOutOfRange(int index, int length)
		{
			Debug.LogError($"Index {index} is out of range [0, {length})");
		}

		[BurstDiscard]
		public static void LogErrorTrimmingSubArrayLength(int startIndex, int wantedLength, int arrayLength)
		{
			Debug.LogError($"Length {wantedLength} is too big for start index {startIndex} and array with length {arrayLength}. Trimming length");
		}

		public ref struct ReverseNativeListSearchIterator<T, U> where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			readonly NativeList<T> _list;
			readonly U _searchElement;
			int _currentIndex;

			public ReverseNativeListSearchIterator(NativeList<T> list, U searchElement)
			{
				_list = list;
				_searchElement = searchElement;
				_currentIndex = list.Length;
			}

			public ReverseNativeListSearchIterator<T, U> GetEnumerator() => this;

			public bool MoveNext()
			{
				do
				{
					_currentIndex--;
				}
				while (_currentIndex >= 0 && !_searchElement.Equals(_list[_currentIndex]));
				return _currentIndex >= 0;
			}

			public int Current => _currentIndex;
		}

		public ref struct ReverseUnsafeListSearchIterator<T, U> where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			readonly UnsafeList<T> _list;
			readonly U _searchElement;
			int _currentIndex;

			public ReverseUnsafeListSearchIterator(in UnsafeList<T> list, U searchElement)
			{
				_list = list;
				_searchElement = searchElement;
				_currentIndex = list.Length;
			}

			public ReverseUnsafeListSearchIterator<T, U> GetEnumerator() => this;

			public bool MoveNext()
			{
				do
				{
					_currentIndex--;
				}
				while (_currentIndex >= 0 && !_searchElement.Equals(_list[_currentIndex]));
				return _currentIndex >= 0;
			}

			public int Current => _currentIndex;
		}

		public ref struct ReverseUnsafeArraySearchIterator<T, U> where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			readonly UnsafeArray<T> _array;
			readonly U _searchElement;
			long _currentIndex;

			public ReverseUnsafeArraySearchIterator(UnsafeArray<T> array, U searchElement)
			{
				_array = array;
				_searchElement = searchElement;
				_currentIndex = array.LengthInt;
			}

			public ReverseUnsafeArraySearchIterator<T, U> GetEnumerator() => this;

			public bool MoveNext()
			{
				do
				{
					_currentIndex--;
				}
				while (_currentIndex >= 0 && !_searchElement.Equals(_array[(uint)_currentIndex]));
				return _currentIndex >= 0;
			}

			public uint Current => (uint)_currentIndex;
		}

		public ref struct ReverseOccupiedArraySearchIterator<T, U> where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			readonly OccupiedArray<T> _array;
			readonly U _searchElement;
			long _currentIndex;

			public ReverseOccupiedArraySearchIterator(OccupiedArray<T> array, U searchElement)
			{
				_array = array;
				_searchElement = searchElement;
				_currentIndex = array.LastTakenCount;
			}

			public ReverseOccupiedArraySearchIterator<T, U> GetEnumerator() => this;

			public bool MoveNext()
			{
				do
				{
					_currentIndex--;
				}
				while (_currentIndex >= 0 && (!_array.IsOccupied((uint)_currentIndex) || !_searchElement.Equals(_array[(uint)_currentIndex])));
				return _currentIndex >= 0;
			}

			public uint Current => (uint)_currentIndex;
		}

		public static UnsafeArray<T> ToUnsafeArray<T>(this List<T> list, Allocator allocator) where T : unmanaged
		{
			var array = new UnsafeArray<T>((uint)list.Count, allocator);
			for (var i = 0; i < list.Count; i++)
			{
				array[i] = list[i];
			}
			return array;
		}

		public static UnsafeArray<U> ToType<T, U, T2U>(this in UnsafeArray<T> array, T2U converter, Allocator allocator) where T : unmanaged where U : unmanaged where T2U : unmanaged, IConverter<T, U>
		{
			var newArray = new UnsafeArray<U>(array.Length, allocator);
			for (var i = 0; i < array.Length; i++)
			{
				newArray[i] = converter.Convert(array[i]);
			}
			return newArray;
		}

		public interface IConverter<in T, out U> where T : unmanaged where U : unmanaged
		{
			U Convert(T value);
		}

		public static unsafe int ThreadSafeAddNoResize<T>(this ref UnsafeList<T> list, T value) where T : unmanaged
		{
			var idx = Interlocked.Increment(ref list.m_length)-1;
			UnsafeUtility.WriteArrayElement(list.Ptr, idx, value);
			return idx;
		}

		public static unsafe int ThreadSafeAddNoResize<T>(this ref NativeList<T> list, T value) where T : unmanaged
		{
			var idx = Interlocked.Increment(ref list.GetUnsafeList()->m_length)-1;
			UnsafeUtility.WriteArrayElement(list.GetUnsafeList()->Ptr, idx, value);
			return idx;
		}

		public static void Resize<T>(this ref NativeArray<T> array, int newSize, Allocator allocator, NativeArrayOptions nativeArrayOptions = NativeArrayOptions.ClearMemory)
			where T : unmanaged
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			Assert.IsTrue(newSize > array.Length);
#endif
			var arrayCopy = new NativeArray<T>(newSize, allocator, nativeArrayOptions);
			arrayCopy.GetSubArray(0, array.Length).CopyFrom(array);
			array = arrayCopy;
		}

		public static unsafe void Resize<T>(this ref UnsafeArray<T> array, uint newSize, NativeArrayOptions nativeArrayOptions = NativeArrayOptions.ClearMemory) where T : unmanaged
		{
			var copyCount = math.min(array.Length, newSize);
			var arrayCopy = new UnsafeArray<T>(newSize, array.Allocator, NativeArrayOptions.UninitializedMemory);
			UnsafeUtility.MemCpy(arrayCopy.Ptr, array.Ptr, copyCount*UnsafeUtility.SizeOf<T>());
			if (((nativeArrayOptions & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory) & newSize > array.Length)
			{
				UnsafeUtility.MemClear(arrayCopy.Ptr+array.Length, (newSize-array.Length)*UnsafeUtility.SizeOf<T>());
			}
			array = arrayCopy;
		}

		public static NativeArray<T> CreateCopy<T>(this NativeArray<T> array, Allocator allocator) where T : unmanaged
		{
			var arrayCopy = new NativeArray<T>(array.Length, allocator, NativeArrayOptions.UninitializedMemory);
			arrayCopy.CopyFrom(array);
			return arrayCopy;
		}

		public static bool SequenceEqual<T, U>(this in UnsafeArray<T> array, in UnsafeArray<U> other) where T : unmanaged where U : unmanaged, IEquatable<T>
		{
			if (!array.IsCreated)
			{
				return !other.IsCreated;
			}
			var length = array.Length;
			if (length != other.Length)
			{
				return false;
			}
			for (var i = 0u; i < length; i++)
			{
				if (!other[i].Equals(array[i]))
				{
					return false;
				}
			}
			return true;
		}

		public static int SequenceHashCode<T>(this in UnsafeArray<T> array) where T : unmanaged
		{
			if (!array.IsCreated)
			{
				return 0;
			}
			if (array.Length == 0)
			{
				return 0;
			}
			var hash = (int)array.Allocator;
			unchecked
			{
				hash = (hash*397) ^ (int)array.Length;
				for (var i = 0u; i < array.Length; i++)
				{
					hash = (hash*397) ^ array[i].GetHashCode();
				}
			}
			return hash;
		}

		[BurstCompile]
		public unsafe struct DisposeJob : IJob
		{
			[NativeDisableUnsafePtrRestriction] public void* array;
			public Allocator allocator;

			public void Execute()
			{
#if TRACK_MEMORY
				UnsafeUtility.FreeTracked(array, allocator);
#else
				UnsafeUtility.Free(array, allocator);
#endif
			}
		}
	}
}
