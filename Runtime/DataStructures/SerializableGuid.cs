using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace KVD.Utils.DataStructures
{
	[StructLayout(LayoutKind.Explicit), Serializable]
	public unsafe struct SerializableGuid : IComparable<SerializableGuid>, IComparable<Guid>, IEquatable<SerializableGuid>, IEquatable<Guid>, IFormattable
	{
		[FieldOffset(0)] public Guid Guid;
		[FieldOffset(0), SerializeField] fixed ulong _value[2];

		public SerializableGuid(ulong part1, ulong part2)
		{
			Guid = Guid.Empty;
			_value[0] = part1;
			_value[1] = part2;
		}

		public SerializableGuid(in Guid guid)
		{
			_value[0] = 0;
			_value[1] = 0;
			Guid = guid;
		}

#if UNITY_EDITOR
		public SerializableGuid(UnityEditor.GUID guid) : this(guid.ToString())
		{
		}
#endif

		public SerializableGuid(string guidString) : this(Guid.Parse(guidString)) {}

		public static SerializableGuid NewGuid()
		{
			return new SerializableGuid(Guid.NewGuid());
		}

		public static implicit operator Guid(SerializableGuid uGuid)
		{
			return uGuid.Guid;
		}

		public readonly int CompareTo(SerializableGuid other)
		{
			return Guid.CompareTo(other.Guid);
		}

		public readonly int CompareTo(Guid other)
		{
			return Guid.CompareTo(other);
		}

		public readonly int CompareTo(object obj)
		{
			return obj switch
			{
				SerializableGuid serializableGuid => Guid.CompareTo(serializableGuid.Guid),
				Guid guid                         => Guid.CompareTo(guid),
				_                                 => -1
			};
		}

		public readonly bool Equals(SerializableGuid other)
		{
			return Guid == other;
		}

		public readonly bool Equals(Guid other)
		{
			return Guid == other;
		}

		public readonly override bool Equals(object obj)
		{
			return obj switch
			{
				SerializableGuid serializableGuid => Guid == serializableGuid.Guid,
				Guid guid                         => Guid == guid,
				_                                 => false
			};
		}

		public override int GetHashCode()
		{
			return Guid.GetHashCode();
		}

		public static bool operator ==(SerializableGuid a, SerializableGuid b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(SerializableGuid a, SerializableGuid b)
		{
			return !a.Equals(b);
		}

		public readonly override string ToString()
		{
			return Guid.ToString();
		}

		public readonly string ToString(string format)
		{
			return Guid.ToString(format);
		}

		public readonly string ToString(string format, IFormatProvider formatProvider)
		{
			return Guid.ToString(format, formatProvider);
		}

		public struct EditorAccess
		{
			public static ulong Value0(in SerializableGuid guid) => guid._value[0];
			public static ulong Value1(in SerializableGuid guid) => guid._value[1];
		}
	}
}
