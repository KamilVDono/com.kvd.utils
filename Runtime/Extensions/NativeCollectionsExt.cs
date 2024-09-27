using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using KVD.Utils.DataStructures;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace KVD.Utils.Extensions
{
	public static class NativeCollectionsExt
	{
		public static int SafeCapacity(this in NativeBitArray collection)
		{
			if (collection.IsCreated)
			{
				return collection.Capacity;
			}
			return 0;
		}

		public static int SafeBucketsLength(this in UnsafeBitmask collection)
		{
			if (collection.IsCreated)
			{
				return collection.BucketsLength;
			}
			return 0;
		}

		public static uint SafeElementsLength(this in UnsafeBitmask collection)
		{
			if (collection.IsCreated)
			{
				return collection.ElementsLength;
			}
			return 0;
		}

		public static int SafeCapacity<T>(this in NativeList<T> collection) where T : unmanaged
		{
			if (collection.IsCreated)
			{
				return collection.Capacity;
			}
			return 0;
		}

		public static int SafeCapacity<T>(this in UnsafeList<T> collection) where T : unmanaged
		{
			if (collection.IsCreated)
			{
				return collection.Capacity;
			}
			return 0;
		}

		public static int SafeLength<T>(this in NativeList<T> collection) where T : unmanaged
		{
			if (collection.IsCreated)
			{
				return collection.Length;
			}
			return 0;
		}

		public static int SafeLength<T>(this in NativeArray<T> collection) where T : unmanaged
		{
			if (collection.IsCreated)
			{
				return collection.Length;
			}
			return 0;
		}

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

		public static int FindIndexOf<T, U>(this in UnsafeArray<T> array, U search, int startIndex = 0) where T : unmanaged where U : unmanaged, IEquatable<T>
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

		public static void EnsureLength<T>(this ref NativeList<T> list, int length) where T : unmanaged
		{
			if (list.Length < length)
			{
				list.Resize(length, NativeArrayOptions.ClearMemory);
			}
		}

		public static void EnsureLengthWithFillNewCapacity<T>(this ref NativeList<T> list, int length, T fillValue) where T : unmanaged
		{
			if (list.Length < length)
			{
				var prevCapacity = list.Capacity;
				list.Resize(length, NativeArrayOptions.UninitializedMemory);
				if (list.Capacity <= prevCapacity)
				{
					return;
				}
				list.FillUpToCapacity(prevCapacity, fillValue);
			}
		}

		public static unsafe void FillUpToCapacity<T>(this ref NativeList<T> list, int startIndex, T fillValue) where T : unmanaged
		{
			var capacity = list.Capacity;
			if (startIndex < 0 || startIndex >= capacity)
			{
				LogErrorIndexIsOutOfRangeForCapacity(startIndex, capacity);
				return;
			}
			int sizeOfT = UnsafeUtility.SizeOf<T>();
			void* destPtr = ((byte*)list.GetUnsafePtr()) + (sizeOfT * startIndex);
			T* fillValuePtr = &fillValue;
			var elementsCount = capacity - startIndex;
			UnsafeUtility.MemCpyReplicate(destPtr, fillValuePtr, sizeOfT, elementsCount);
		}

		public static unsafe T[] ToArray<T>(this in UnsafeList<T>.ReadOnly list) where T : unmanaged
		{
			T[] dst = new T[list.Length];
			var gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
			UnsafeUtility.MemCpy(gcHandle.AddrOfPinnedObject().ToPointer(), list.Ptr, list.Length * UnsafeUtility.SizeOf<T>());
			gcHandle.Free();
			return dst;
		}

		public static unsafe UnsafeArray<T>.Span AsUnsafeSpan<T>(this in UnsafeList<T> list) where T : unmanaged
		{
			return UnsafeArray<T>.FromExistingData(list.Ptr, (uint)list.Length);
		}

		public static unsafe UnsafeArray<T>.Span AsUnsafeSpan<T>(this in UnsafeList<T>.ReadOnly list) where T : unmanaged
		{
			return UnsafeArray<T>.FromExistingData(list.Ptr, (uint)list.Length);
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
	}
}
