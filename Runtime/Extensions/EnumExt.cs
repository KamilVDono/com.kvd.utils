using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace KVD.Utils.Extensions
{
	public static class EnumExt
	{
		private static class HasFlagFastEnumHelper<T1>
		{
			public static Func<T1, T1, bool> testOverlapProc = InitProc;
			public static bool Overlaps(sbyte p1, sbyte p2) { return (p1 & p2) == p2; }
			public static bool Overlaps(byte p1, byte p2) { return (p1 & p2) == p2; }
			public static bool Overlaps(short p1, short p2) { return (p1 & p2) == p2; }
			public static bool Overlaps(ushort p1, ushort p2) { return (p1 & p2) == p2; }
			public static bool Overlaps(int p1, int p2) { return (p1 & p2) == p2; }
			public static bool Overlaps(uint p1, uint p2) { return (p1 & p2) == p2; }
			private static bool InitProc(T1 p1, T1 p2)
			{
				var typ1 = Enum.GetUnderlyingType(typeof(T1));
				Type[] types = { typ1, typ1, };
				var method = typeof(HasFlagFastEnumHelper<T1>).GetMethod("Overlaps", types)!;
				testOverlapProc = (Func<T1, T1, bool>)Delegate.CreateDelegate(typeof(Func<T1, T1, bool>), method);
				return testOverlapProc(p1, p2);
			}
		}

		// It's super annoying that HasFlag boxes (ugh)
		public static bool HasFlagFast<T>(this T p1, T p2) where T : Enum
		{
			return HasFlagFastEnumHelper<T>.testOverlapProc(p1, p2);
		}

		static class HasCommonBitsEnumHelper<T1>
		{
			public static Func<T1, T1, bool> testOverlapProc = InitProc;
			public static bool Overlaps(sbyte p1, sbyte p2) { return (p1 & p2) != 0; }
			public static bool Overlaps(byte p1, byte p2) { return (p1 & p2) != 0; }
			public static bool Overlaps(short p1, short p2) { return (p1 & p2) != 0; }
			public static bool Overlaps(ushort p1, ushort p2) { return (p1 & p2) != 0; }
			public static bool Overlaps(int p1, int p2) { return (p1 & p2) != 0; }
			public static bool Overlaps(uint p1, uint p2) { return (p1 & p2) != 0; }
			public static bool InitProc(T1 p1, T1 p2)
			{
				Type typ1 = typeof(T1);
				if (typ1.IsEnum) typ1 = Enum.GetUnderlyingType(typ1);
				Type[] types = { typ1, typ1 };
				var method = typeof(HasCommonBitsEnumHelper<T1>).GetMethod("Overlaps", types);
				if (method == null) throw new MissingMethodException("Unknown type of enum");
				testOverlapProc = (Func<T1, T1, bool>)Delegate.CreateDelegate(typeof(Func<T1, T1, bool>), method);
				return testOverlapProc(p1, p2);
			}
		}

		public static bool HasCommonBitsFast<T>(this T p1, T p2) where T : Enum
		{
			return HasCommonBitsEnumHelper<T>.testOverlapProc(p1, p2);
		}

		static class ToStringEnumHelper<T1>
		{
			static Dictionary<T1, string> _cache;

			static ToStringEnumHelper()
			{
				_cache = new Dictionary<T1, string>();
			}

			public static string ToString(T1 p1)
			{
				if (_cache.TryGetValue(p1, out string toStringValue))
				{
					return toStringValue;
				}

				toStringValue = p1.ToString();
				_cache[p1] = toStringValue;
				return toStringValue;
			}
		}

		public static string ToStringFast<T>(this T p1) where T : Enum
		{
			return ToStringEnumHelper<T>.ToString(p1);
		}

		static class ToIntEnumHelper
		{
			public static int ToInt(sbyte value) { return value; }
			public static int ToInt(byte value) { return value; }
			public static int ToInt(short value) { return value; }
			public static int ToInt(ushort value) { return value; }
			public static int ToInt(int value) { return value; }
			public static int ToInt(uint value) { return (int)value; }
		}

		static class ToIntEnumHelper<TEnum> where TEnum : Enum
		{
			public static readonly Func<TEnum, int> ToIntDelegate = CreateToInt();

			static Func<TEnum, int> CreateToInt()
			{
				var underlyingType = typeof(TEnum).GetEnumUnderlyingType();
				var method = typeof(ToIntEnumHelper).GetMethod(nameof(ToIntEnumHelper.ToInt), new[] { underlyingType });
				if (method == null)
				{
					return InvalidConversion;
				}
				return (Func<TEnum, int>)Delegate.CreateDelegate(typeof(Func<TEnum, int>), method);
			}

			static int InvalidConversion(TEnum _)
			{
				throw new InvalidCastException();
			}
		}

		public static int ToInt<TEnum>(this TEnum value) where TEnum : Enum
		{
			return ToIntEnumHelper<TEnum>.ToIntDelegate(value);
		}

		static class ToEnumHelper<TSource, TEnum> where TEnum : Enum
		{
			public static readonly Func<TSource, TEnum> ToEnumDelegate = CreateToEnum();

			static Func<TSource, TEnum> CreateToEnum()
			{
				try
				{
					var sourceType = typeof(TSource);
					var enumType = typeof(TEnum);
					var underlyingType = enumType.GetEnumUnderlyingType();

					var intParameter = Expression.Parameter(sourceType);
					var underlyingCast = Expression.Convert(intParameter, underlyingType);
					var enumCast = Expression.Convert(underlyingCast, enumType);
					return Expression.Lambda<Func<TSource, TEnum>>(enumCast, intParameter).Compile();
				}
				catch
				{
					return InvalidConversion;
				}
			}

			static TEnum InvalidConversion(TSource source)
			{
				throw new InvalidCastException($"Cannot cast {typeof(TSource).Name} {source} to {typeof(TEnum).Name}");
			}
		}

		public static TEnum ToEnum<TEnum>(this sbyte value) where TEnum : Enum => ToEnumHelper<sbyte, TEnum>.ToEnumDelegate(value);
		public static TEnum ToEnum<TEnum>(this byte value) where TEnum : Enum => ToEnumHelper<byte, TEnum>.ToEnumDelegate(value);
		public static TEnum ToEnum<TEnum>(this short value) where TEnum : Enum => ToEnumHelper<short, TEnum>.ToEnumDelegate(value);
		public static TEnum ToEnum<TEnum>(this ushort value) where TEnum : Enum => ToEnumHelper<ushort, TEnum>.ToEnumDelegate(value);
		public static TEnum ToEnum<TEnum>(this int value) where TEnum : Enum => ToEnumHelper<int, TEnum>.ToEnumDelegate(value);
		public static TEnum ToEnum<TEnum>(this uint value) where TEnum : Enum => ToEnumHelper<uint, TEnum>.ToEnumDelegate(value);
		public static Enum ToEnum(this int value, Type enumType)
		{
			try
			{
				var sourceType = typeof(int);
				var underlyingType = enumType.GetEnumUnderlyingType();

				var intParameter = Expression.Parameter(sourceType);
				var underlyingCast = Expression.Convert(intParameter, underlyingType);
				var enumCast = Expression.Convert(underlyingCast, enumType);
				var enumEnumCast = Expression.Convert(enumCast, typeof(Enum));
				return Expression.Lambda<Func<int, Enum>>(enumEnumCast, intParameter).Compile()(value);
			}
			catch
			{
				return default;
			}
		}
	}
}
