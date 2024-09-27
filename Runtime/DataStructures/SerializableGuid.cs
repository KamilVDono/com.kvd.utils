using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace KVD.Utils.DataStructures
{
	[StructLayout(LayoutKind.Explicit), Serializable]
	public struct SerializableGuid : IEquatable<SerializableGuid>
	{
		[FieldOffset(0)] public Guid Guid;
		[FieldOffset(0), SerializeField] int _guidPart1;
		[FieldOffset(4), SerializeField] int _guidPart2;
		[FieldOffset(8), SerializeField] int _guidPart3;
		[FieldOffset(12), SerializeField] int _guidPart4;

		public SerializableGuid(Guid guid)
		{
			_guidPart1 = 0;
			_guidPart2 = 0;
			_guidPart3 = 0;
			_guidPart4 = 0;
			Guid = guid;
		}

		public static SerializableGuid NewGuid()
		{
			return new SerializableGuid(Guid.NewGuid());
		}

		public static implicit operator Guid(SerializableGuid uGuid)
		{
			return uGuid.Guid;
		}

		public int CompareTo(SerializableGuid other)
		{
			return Guid.CompareTo(other.Guid);
		}

		public int CompareTo(Guid other)
		{
			return Guid.CompareTo(other);
		}

		public int CompareTo(object obj)
		{
			if (obj == null)
			{
				return -1;
			}

			if (obj is SerializableGuid serializableGuid)
			{
				return serializableGuid.Guid.CompareTo(Guid);
			}

			if (obj is Guid guid)
			{
				return guid.CompareTo(Guid);
			}

			return -1;
		}

		public bool Equals(SerializableGuid other)
		{
			return Guid == other;
		}

		public bool Equals(Guid other)
		{
			return Guid == other;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (obj is SerializableGuid serializableGuid)
			{
				return Guid == serializableGuid.Guid;
			}

			if (obj is Guid guid)
			{
				return Guid == guid;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return Guid.GetHashCode();
		}

		public override string ToString()
		{
			return Guid.ToString();
		}

		public static bool operator ==(SerializableGuid a, SerializableGuid b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(SerializableGuid a, SerializableGuid b)
		{
			return !a.Equals(b);
		}
	}
}
