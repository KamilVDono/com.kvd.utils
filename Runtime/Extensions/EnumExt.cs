using System;
using UnityEngine.Scripting;

// ReSharper disable UnusedMember.Local

namespace KVD.Utils.Extensions
{
	[Preserve]
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
				var    typ1   = Enum.GetUnderlyingType(typeof(T1));
				Type[] types  = { typ1, typ1, };
				var    method = typeof(HasFlagFastEnumHelper<T1>).GetMethod("Overlaps", types)!;
				testOverlapProc = (Func<T1, T1, bool>)Delegate.CreateDelegate(typeof(Func<T1, T1, bool>), method);
				return testOverlapProc(p1, p2);
			}
		}
		
		// It's super annoying that HasFlag boxes (ugh)
		public static bool HasFlagFast<T>(this T p1, T p2) where T : Enum
		{
			return HasFlagFastEnumHelper<T>.testOverlapProc(p1, p2);
		}
	}
}
