#if DEBUG && UNSAFE_MEMORY_TRACKING
#define TRACK_MEMORY
#endif
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using KVD.Utils.Extensions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;
using Unity.Jobs;
using Unity.Mathematics;

namespace KVD.Utils.DataStructures
{
	[DebuggerDisplay("Buckets = {BucketsLength}"), DebuggerTypeProxy(typeof(UnsafeBitmaskDebugView))]
	public unsafe struct UnsafeBitmask
	{
		public static readonly UnsafeBitmask Empty = default;

		const char ControlCharacter = 'b';
		internal const uint IndexMask = 63;
		internal const int BucketOffset = 6;

		[NativeDisableUnsafePtrRestriction] ulong* _masks;
		ulong _lastMaskComplement;
		uint _elementsLength;
		Allocator _allocator;

		public readonly bool IsCreated => _masks != null;
		public readonly ushort BucketsLength => _elementsLength == 0 ? (ushort)0 : (ushort)(Bucket(_elementsLength-1)+1);
		public readonly uint ElementsLength => _elementsLength;

		public UnsafeBitmask(uint elementsLength, Allocator allocator)
		{
			_elementsLength     = elementsLength;
			_allocator          = allocator;
			_lastMaskComplement = ~0u; // Not real value, will be recalculated in separate method
			var bucketLength = Bucket(_elementsLength-1)+1;
#if TRACK_MEMORY
			_masks = (ulong*)UnsafeUtility.MallocTracked(UnsafeUtility.SizeOf<ulong>() * bucketLength,
				UnsafeUtility.AlignOf<ulong>(), _allocator, 1);
#else
			_masks = (ulong*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ulong>()*bucketLength,
				UnsafeUtility.AlignOf<ulong>(), _allocator);
#endif
			UnsafeUtility.MemClear(_masks, UnsafeUtility.SizeOf<ulong>()*bucketLength);
			RecalculateLastMaskComplement();
		}

		public UnsafeBitmask(UnsafeBitmask other, Allocator allocator)
		{
			_elementsLength = other._elementsLength;
			_allocator      = allocator;
			_lastMaskComplement = other._lastMaskComplement;
			var bucketLength = Bucket(_elementsLength-1)+1;
#if TRACK_MEMORY
			_masks = (ulong*)UnsafeUtility.MallocTracked(UnsafeUtility.SizeOf<ulong>() * bucketLength,
				UnsafeUtility.AlignOf<ulong>(), _allocator, 1);
#else
			_masks = (ulong*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ulong>()*bucketLength,
				UnsafeUtility.AlignOf<ulong>(), _allocator);
#endif
			UnsafeUtility.MemCpy(_masks, other._masks, UnsafeUtility.SizeOf<ulong>()*bucketLength);
		}

		public void Dispose()
		{
			if (_allocator > Allocator.None)
			{
#if TRACK_MEMORY
				UnsafeUtility.FreeTracked(_masks, _allocator);
#else
				UnsafeUtility.Free(_masks, _allocator);
#endif
			}
			_masks = null;
		}

		public JobHandle Dispose(JobHandle dependency)
		{
			if (!IsCreated)
			{
				return dependency;
			}
			if (_allocator > Allocator.None)
			{
				var job = new NativeCollectionsExt.DisposeJob
				{
					array     = _masks,
					allocator = _allocator
				};
				return job.Schedule(dependency);
			}

			return dependency;
		}

		[Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
		public bool this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this[(uint)AssumePositive(index)];
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => this[(uint)AssumePositive(index)] = value;
		}

		[Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
		public bool this[uint index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				var bucket = Bucket(index);
				var masked = _masks[bucket] & BucketIndexMask(index);
				return masked > 0;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				var bucket = Bucket(index);
				if (value)
				{
					_masks[bucket] |= BucketIndexMask(index);
				}
				else
				{
					_masks[bucket] &= ~BucketIndexMask(index);
				}
			}
		}

		[Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
		public bool Has(uint index)
		{
			return index < _elementsLength && this[index];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
		public void Up(int index)
		{
			Up((uint)AssumePositive(index));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
		public void Up(uint index)
		{
			_masks[Bucket(index)] |= BucketIndexMask(index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
		public void Down(uint index)
		{
			_masks[Bucket(index)] &= ~BucketIndexMask(index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
		public void Down(uint index, uint length)
		{
			var startBucket = Bucket(index);
			var endBucket = Bucket(index + length - 1);
			var startMask = (1ul << (int)BucketIndex(index)) - 1;
			var endMask = ~((1ul << (int)BucketIndex(index + length)) - 1);
			if (startBucket == endBucket)
			{
				_masks[startBucket] &= startMask | endMask;
			}
			else
			{
				_masks[startBucket] &= startMask;
				_masks[endBucket] &= endMask;
				for (var i = startBucket + 1; i < endBucket; i++)
				{
					_masks[i] = 0;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
		public void Zero()
		{
			UnsafeUtility.MemClear(_masks, UnsafeUtility.SizeOf<ulong>()*BucketsLength);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
		public void All()
		{
			UnsafeUtility.MemSet(_masks, byte.MaxValue, UnsafeUtility.SizeOf<ulong>()*BucketsLength);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
		public readonly uint CountOnes()
		{
			var bucketsLength = BucketsLength;
			var count         = 0u;
			for (var i = 0; i < bucketsLength; i++)
			{
				count += (uint)math.countbits(_masks[i]);
			}
			return count;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool AnySet()
		{
			var bucketsLength = BucketsLength;
			for (var i = 0; i < bucketsLength; i++)
			{
				if (_masks[i] > 0)
				{
					return true;
				}
			}
			return false;

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly int FirstZero()
		{
			var bucketsLength = BucketsLength;
			var lastBucket    = bucketsLength-1;
			for (var i = 0; i < lastBucket; i++)
			{
				if (_masks[i] != ulong.MaxValue)
				{
					return i*64+math.tzcnt(~_masks[i]);
				}
			}
			if (lastBucket >= 0 && (_masks[lastBucket] | _lastMaskComplement) != ulong.MaxValue)
			{
				return lastBucket*64+math.tzcnt(~_masks[lastBucket]);
			}
			return -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly int FirstOne()
		{
			var bucketsLength = BucketsLength;
			for (var i = 0; i < bucketsLength; i++)
			{
				if (_masks[i] != 0)
				{
					return i*64+math.tzcnt(_masks[i]);
				}
			}
			return -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly int LastOne()
		{
			var bucketsLength = BucketsLength;
			for (var i = bucketsLength - 1; i >= 0; i--)
			{
				if (_masks[i] != 0)
				{
					return i * 64 + (63 - math.lzcnt(_masks[i]));
				}
			}

			return -1;
		}

		public void Union(in UnsafeBitmask other)
		{
			EnsureCapacity(other._elementsLength);

			// We are at least other._masks size
			var otherBucketLength = other.BucketsLength;
			for (var i = 0; i < otherBucketLength; ++i)
			{
				_masks[i] |= other._masks[i];
			}
		}

		public void Intersect(UnsafeBitmask other)
		{
			var otherBucketLength = other.BucketsLength;
			var myBucketLength = BucketsLength;

			var minBucketLength = myBucketLength < otherBucketLength ? myBucketLength : otherBucketLength;
			for (var i = 0; i < minBucketLength; ++i)
			{
				_masks[i] &= other._masks[i];
			}
			for (var i = minBucketLength; i < myBucketLength; ++i)
			{
				_masks[i] = 0;
			}
		}

		public void Exclude(UnsafeBitmask other)
		{
			var otherBucketLength = other.BucketsLength;
			var myBucketLength    = BucketsLength;

			var minBucketLength = myBucketLength < otherBucketLength ? myBucketLength : otherBucketLength;
			for (var i = 0; i < minBucketLength; ++i)
			{
				_masks[i] &= ~other._masks[i];
			}
		}

		public void EnsureIndex(uint elementIndex)
		{
			EnsureCapacity(elementIndex+1u);
		}

		public void EnsureCapacity(uint elementsLength)
		{
#if UNITY_EDITOR || DEBUG
			if (_allocator <= Allocator.None)
			{
				throw new Exception($"Trying to ensure capacity of invalid bitmask with allocator {_allocator} and size {_elementsLength}");
			}
#endif
			var bucket = Bucket(elementsLength-1)+1;

			if (BucketsLength < bucket)
			{
				Resize(elementsLength);
			}
			else
			{
				_elementsLength = math.max(elementsLength, _elementsLength);
			}
			RecalculateLastMaskComplement();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly OnesEnumerator EnumerateOnes() => new OnesEnumerator(this);

		void Resize(uint elementsLength)
		{
			var oldBucketLength = BucketsLength;
			_elementsLength = elementsLength;
			var newBucketLength = Bucket(elementsLength-1)+1;

#if TRACK_MEMORY
			var newMask = (ulong*)UnsafeUtility.MallocTracked(UnsafeUtility.SizeOf<ulong>() * newBucketLength,
				UnsafeUtility.AlignOf<ulong>(), _allocator, 1);
#else
			var newMask = (ulong*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ulong>()*newBucketLength,
				UnsafeUtility.AlignOf<ulong>(), _allocator);
#endif

			UnsafeUtility.MemCpy(newMask, _masks, UnsafeUtility.SizeOf<ulong>()*oldBucketLength);
			UnsafeUtility.MemClear(newMask+oldBucketLength,
				UnsafeUtility.SizeOf<ulong>()*(newBucketLength-oldBucketLength));

#if TRACK_MEMORY
			UnsafeUtility.FreeTracked(_masks, _allocator);
#else
			UnsafeUtility.Free(_masks, _allocator);
#endif

			_masks = newMask;
		}

		void RecalculateLastMaskComplement()
		{
			var complementIndex = _elementsLength%64;
			_lastMaskComplement = complementIndex == 0 ?
				(_elementsLength == 0 ? ulong.MaxValue : 0) :
				~((1ul << (int)complementIndex)-1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static ushort Bucket(uint index)
		{
			return (ushort)(index >> BucketOffset);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static uint BucketIndex(uint index)
		{
			return index & IndexMask;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static ulong BucketIndexMask(uint index)
		{
			return 1ul << (int)BucketIndex(index);
		}

		[return: AssumeRange(0, int.MaxValue)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int AssumePositive(int value)
		{
			return value;
		}

		public ref struct OnesEnumerator
		{
			readonly ulong* _masks;
			ulong _mask;
			int _index;
			readonly ushort _bucketsLength;
			ushort _bucketIndex;

			public OnesEnumerator(in UnsafeBitmask data)
			{
				_masks         = data._masks;
				_bucketsLength = data.BucketsLength;
				_bucketIndex   = 0;
				_mask          = ulong.MaxValue;
				_index         = -1;
			}

			public bool MoveNext()
			{
				_index = NextOne();
				if (_index != -1)
				{
					_mask ^= 1ul << _index;
					return true;
				}
				return false;
			}

			public uint Current => (uint)(_index+_bucketIndex*64);

			public OnesEnumerator GetEnumerator() => this;

			int NextOne()
			{
				for (; _bucketIndex < _bucketsLength; _bucketIndex++)
				{
					var masked = _masks[_bucketIndex] & _mask;
					if (masked != 0)
					{
						return math.tzcnt(masked);
					}
					_mask = ulong.MaxValue;
				}
				return -1;
			}
		}

		public interface IConverter<out T> where T : unmanaged
		{
			T Convert(uint index);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(ControlCharacter);
			writer.Write(_elementsLength);
			writer.Write(_lastMaskComplement);
			writer.Write((int)_allocator);
			writer.Write(ControlCharacter);
			var bucketsLength = BucketsLength;
			for (var i = 0; i < bucketsLength; i++)
			{
				writer.Write(_masks[i]);
			}
			writer.Write(ControlCharacter);
		}

		public void Deserialize(BinaryReader reader)
		{
			// TODO: Implement
			// Assert.AreEqual(reader.ReadChar(), ControlCharacter);
			// var length = reader.ReadUInt32();
			// EnsureCapacity(length);
			// Assert.AreEqual(reader.ReadChar(), ControlCharacter);
			// for (var i = 0; i < length; i++)
			// {
			//     _masks[i] = reader.ReadUInt64();
			// }
			// Assert.AreEqual(reader.ReadChar(), ControlCharacter);
		}

		internal sealed class UnsafeBitmaskDebugView
		{
			UnsafeBitmask _data;

			public UnsafeBitmaskDebugView(UnsafeBitmask data)
			{
				_data = data;
			}

			public bool[] Items
			{
				get
				{
					var result = new bool[_data._elementsLength];

					var i             = 0;
					var bucketsLength = _data.BucketsLength;
					for (var j = 0; j < bucketsLength; ++j)
					{
						var bucket = _data._masks[j];
						for (var k = 0; k < 64; k++)
						{
							result[i] = (bucket & ((ulong)1 << k)) > 0;
							++i;
						}
					}

					return result;
				}
			}
		}
	}

	[BurstCompile]
	public static class UnsafeBitmaskExtensions
	{
		[BurstCompile]
		public static void ToIndicesOfOneArray(in this UnsafeBitmask bitmask, Allocator allocator,
			out UnsafeArray<uint> result)
		{
			result = new UnsafeArray<uint>(bitmask.CountOnes(), allocator);
			var i = 0u;
			foreach (var index in bitmask.EnumerateOnes())
			{
				result[i++] = index;
			}
		}

		[BurstCompile]
		public static void ToArray<T, TU>(in this UnsafeBitmask bitmask, Allocator allocator, TU converter,
			out UnsafeArray<T> result)
			where T : unmanaged
			where TU : unmanaged, UnsafeBitmask.IConverter<T>
		{
			result = new UnsafeArray<T>(bitmask.CountOnes(), allocator);
			var i      = 0u;
			foreach (var index in bitmask.EnumerateOnes())
			{
				result[i++] = converter.Convert(index);
			}
		}

		public static ulong Size(in this UnsafeBitmask bitmask)
		{
			var ownSize = (ulong)UIntPtr.Size + sizeof(ulong) + sizeof(uint) + sizeof(Allocator);
			var bucketsSize = BucketsSize(bitmask);
			return ownSize + bucketsSize;
		}

		public static ulong BucketsSize(in this UnsafeBitmask bitmask)
		{
			return (ulong)bitmask.BucketsLength * sizeof(ulong);
		}
	}
}
