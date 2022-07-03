using System;
using System.Collections.Generic;
using KVD.Utils.Extensions;

// Based on: https://blog.teamleadnet.com/2012/08/murmurhash3-ultra-fast-hash-algorithm.html
#nullable enable

namespace KVD.Utils.Maths
{
    public class MurmurHash
    {
        private const ulong ReadSize = 16;
        private const ulong C1 = 0x87c37b91114253d5L;
        private const ulong C2 = 0x4cf5ad432745937fL;

        private ulong _length;
        private readonly Result _result;

        public Result CurrentHash => _result;

        public MurmurHash(uint seed = 691)
        {
            _result = new(new ulong[2]);
            _length = 0;
            _result.H1 = seed;
        }

        public void Add(byte[] bytes)
        {
            unsafe
            {
                fixed (byte* bytesPointer = &bytes[0])
                {
                    ProcessBytes(bytesPointer, (ulong)bytes.Length);
                }
            }
        }
        
        public void Add<T>(T value) where T : unmanaged
        {
            unsafe
            {
                var bytes = (byte*)&value;
                ProcessBytes(bytes, sizeof(int));
            }
        }
        
        public unsafe void Add(byte* bytes, ulong length)
        {
            ProcessBytes(bytes, length);
        }

        public Result Bake()
        {
            _result.H1 ^= _length;
            _result.H2 ^= _length;

            _result.H1 += _result.H2;
            _result.H2 += _result.H1;

            _result.H1 = MixFinal(_result.H1);
            _result.H2 = MixFinal(_result.H2);

            _result.H1 += _result.H2;
            _result.H2 += _result.H1;

            return _result;
        }

        public void Clear(uint seed = 691)
        {
            _result.H1 = seed;
            _result.H2 = 0;
            _length    = 0;
        }

        private void MixBody(ulong k1, ulong k2)
        {
            _result.H1 ^= MixKey1(k1);

            _result.H1 =  _result.H1.RotateLeft(27);
            _result.H1 += _result.H2;
            _result.H1 =  _result.H1*5+0x52dce729;

            _result.H2 ^= MixKey2(k2);

            _result.H2 =  _result.H2.RotateLeft(31);
            _result.H2 += _result.H1;
            _result.H2 =  _result.H2*5+0x38495ab5;
        }

        private static ulong MixKey1(ulong k1)
        {
            k1 *= C1;
            k1 =  k1.RotateLeft(31);
            k1 *= C2;
            return k1;
        }

        private static ulong MixKey2(ulong k2)
        {
            k2 *= C2;
            k2 =  k2.RotateLeft(33);
            k2 *= C1;
            return k2;
        }

        private static ulong MixFinal(ulong k)
        {
            k ^= k >> 33;
            k *= 0xff51afd7ed558ccdL;
            k ^= k >> 33;
            k *= 0xc4ceb9fe1a85ec53L;
            k ^= k >> 33;
            return k;
        }

        private unsafe void ProcessBytes(byte* bytes, ulong length)
        {
            var pos       = 0;
            var remaining = length;

            while (remaining >= ReadSize)
            {
                var k1 = LongExt.BytesToLong(bytes, pos);
                pos += 8;

                var k2 = LongExt.BytesToLong(bytes, pos);
                pos += 8;

                _length   += ReadSize;
                remaining -= ReadSize;

                MixBody(k1, k2);
            }

            if (remaining > 0)
            {
                ProcessBytesRemaining(bytes, remaining, pos);
            }
        }

        private unsafe void ProcessBytesRemaining(byte* bytes, ulong remaining, int pos)
        {
            ulong k1 = 0;
            ulong k2 = 0;
            _length += remaining;

            switch (remaining)
            {
                case 15:
                    k2 ^= (ulong)bytes[pos+14] << 48;
                    goto case 14;
                case 14:
                    k2 ^= (ulong)bytes[pos+13] << 40;
                    goto case 13;
                case 13:
                    k2 ^= (ulong)bytes[pos+12] << 32;
                    goto case 12;
                case 12:
                    k2 ^= (ulong)bytes[pos+11] << 24;
                    goto case 11;
                case 11:
                    k2 ^= (ulong)bytes[pos+10] << 16;
                    goto case 10;
                case 10:
                    k2 ^= (ulong)bytes[pos+9] << 8;
                    goto case 9;
                case 9:
                    k2 ^= bytes[pos+8];
                    goto case 8;
                case 8:
                    k1 ^= LongExt.BytesToLong(bytes, pos);
                    break;
                case 7:
                    k1 ^= (ulong)bytes[pos+6] << 48;
                    goto case 6;
                case 6:
                    k1 ^= (ulong)bytes[pos+5] << 40;
                    goto case 5;
                case 5:
                    k1 ^= (ulong)bytes[pos+4] << 32;
                    goto case 4;
                case 4:
                    k1 ^= (ulong)bytes[pos+3] << 24;
                    goto case 3;
                case 3:
                    k1 ^= (ulong)bytes[pos+2] << 16;
                    goto case 2;
                case 2:
                    k1 ^= (ulong)bytes[pos+1] << 8;
                    goto case 1;
                case 1:
                    k1 ^= bytes[pos];
                    break;
                default:
                    throw new("Something went wrong with remaining bytes calculation.");
            }

            _result.H1 ^= MixKey1(k1);
            _result.H2 ^= MixKey2(k2);
        }

        public readonly struct Result : IEquatable<Result>
        {
            public static IEqualityComparer<Result> Comparer{ get; } = new HashEqualityComparer();
            
            private readonly ulong[] _hash;

            public ulong[] Hash => _hash;

            internal ulong H1
            {
                get => _hash[0];
                set => _hash[0] = value;
            }
            
            internal ulong H2
            {
                get => _hash[1];
                set => _hash[1] = value;
            }

            public Result(ulong[] hash)
            {
                _hash = hash;
            }

            #region Equality
            public bool Equals(Result other)
            {
                return _hash[0] == other._hash[0] && _hash[1] == other._hash[1];
            }
            public override bool Equals(object? obj)
            {
                return obj is Result other && Equals(other);
            }
            public override int GetHashCode()
            {
                return HashCode.Combine(_hash[0], _hash[1]);
            }
            public static bool operator ==(Result left, Result right)
            {
                return left.Equals(right);
            }
            public static bool operator !=(Result left, Result right)
            {
                return !left.Equals(right);
            }
            
            private sealed class HashEqualityComparer : IEqualityComparer<Result>
            {
                public bool Equals(Result x, Result y)
                {
                    return x.Equals(y);
                }
                public int GetHashCode(Result obj)
                {
                    return obj.GetHashCode();
                }
            }
            #endregion Equality
        }
    }
}
