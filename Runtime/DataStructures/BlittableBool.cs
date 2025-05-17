using System;

namespace KVD.Utils.DataStructures
{
	[Serializable]
	public struct BlittableBool
	{
		public static readonly BlittableBool False = new BlittableBool { value = 0, };
		public static readonly BlittableBool True = new BlittableBool { value = 1, };

		public byte value;

		public static implicit operator bool(BlittableBool value)
		{
			return value.value != 0;
		}

		public static implicit operator BlittableBool(bool value)
		{
			return new() { value = (byte)(value ? 1 : 0) };
		}

		public bool Equals(BlittableBool other)
		{
			return value == other.value;
		}

		public override bool Equals(object obj)
		{
			return obj is BlittableBool other && Equals(other);
		}

		public override int GetHashCode()
		{
			return value.GetHashCode();
		}

		public static bool operator ==(BlittableBool left, BlittableBool right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(BlittableBool left, BlittableBool right)
		{
			return !left.Equals(right);
		}
	}
}
