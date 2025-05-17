using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KVD.Utils.DataStructures
{
	[DebuggerDisplay("Length = {Length}, IsValid = {IsValid}")]
	[DebuggerTypeProxy(typeof(UnsafeSpan<>.SpanTDebugView))]
	public readonly unsafe struct UnsafeSpan<T> where T : unmanaged
	{
		[NativeDisableUnsafePtrRestriction] readonly T* _array;
		readonly uint _length;

		public T* Ptr => _array;
		public uint Length => _length;
		public int LengthInt => (int)_length;
		public bool IsValid => _array != null;

		public UnsafeSpan(T* array, uint length)
		{
			_array = array;
			_length = length;
		}

		public ref T this[uint index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return ref *(_array+index);
			}
		}

		public UnsafeEnumerator<T> GetEnumerator()
		{
			return new(_array, _length);
		}

		public UnsafeSpan<TU> As<TU>() where TU : unmanaged
		{
			var bytes = _length * (uint)UnsafeUtility.SizeOf<T>();
			var resultLength = bytes / (uint)UnsafeUtility.SizeOf<TU>();

#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
			if (bytes != UnsafeUtility.SizeOf<TU>() * resultLength)
			{
				throw new InvalidOperationException($"{bytes} bytes from type {typeof(T)} can not be converted to type {typeof(TU)}");
			}
#endif
			return new UnsafeSpan<TU>((TU*)_array, resultLength);
		}

		public UnsafeArray<T> AsUnsafeArray()
		{
			return new UnsafeArray<T>(_array, _length);
		}

		public NativeArray<T> AsNativeArray()
		{
			return AsUnsafeArray().AsNativeArray();
		}

		public T[] ToManagedArray()
		{
			var array = new T[_length];
			fixed (T* pinnedArray = &array[0])
			{
				UnsafeUtility.MemCpy(pinnedArray, _array, _length*UnsafeUtility.SizeOf<T>());
			}
			return array;
		}

		public static explicit operator UnsafeArray<T>(UnsafeSpan<T> span)
		{
			return span.AsUnsafeArray();
		}

		sealed class SpanTDebugView
		{
			UnsafeSpan<T> _data;

			public SpanTDebugView(UnsafeSpan<T> data)
			{
				_data = data;
			}

			public T[] Items
			{
				get
				{
					if (!_data.IsValid)
					{
						return Array.Empty<T>();
					}
					return _data.AsUnsafeArray().ToManagedArray();
				}
			}
		}
	}
}
