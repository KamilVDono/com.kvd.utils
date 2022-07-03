using System.Runtime.CompilerServices;

namespace KVD.Utils.Extensions
{
	public static class LongExt
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong RotateLeft(this ulong original, int bits)
		{
			return (original << bits) | (original >> (64 - bits));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong RotateRight(this ulong original, int bits)
		{
			return (original >> bits) | (original << (64 - bits));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe ulong BytesToLong(this byte[] bytes, int position)
		{
			fixed (byte* bytePointer = &bytes[position])
			{
				return *(ulong*)bytePointer;
			}
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe ulong BytesToLong(byte* bytes, int position)
		{
			bytes += position;
			return *(ulong*)bytes;
		}
	}
}
