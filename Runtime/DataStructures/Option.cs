using System;

namespace KVD.Utils.DataStructures
{
	public readonly struct Option<T>
	{
		readonly T _value;
		readonly BlittableBool _hasValue;

		public bool HasValue => _hasValue;
		public T Value => _value;

		Option(T value)
		{
			_value = value;
			_hasValue = true;
		}

		public static Option<T> Some(T value) => new Option<T>(value);
		public static Option<T> None => new Option<T>();

		public void Deconstruct(out bool hasValue, out T value)
		{
			hasValue = _hasValue;
			value = _value;
		}

		public bool Deconstruct(out T value)
		{
			if (HasValue)
			{
				value = Value;
				return true;
			}
			else
			{
				value = default;
				return false;
			}
		}

		public bool TryGetValue(out T value)
		{
			value = _value;
			return _hasValue;
		}

		public T GetValueOrDefault(T defaultValue = default)
		{
			return _hasValue ? _value : defaultValue;
		}

		public T GetValueOrThrow(string message = "Option does not have a value")
		{
			if (!_hasValue)
			{
				throw new InvalidOperationException(message);
			}
			return _value;
		}

		public override string ToString()
		{
			return _hasValue ? $"Some({_value})" : "None";
		}

		public override int GetHashCode()
		{
			return _hasValue ? _value.GetHashCode() : 0;
		}

		public static implicit operator Option<T>(T value)
		{
			return new Option<T>(value);
		}

		public static implicit operator bool(Option<T> optional)
		{
			return optional._hasValue;
		}
	}
}
