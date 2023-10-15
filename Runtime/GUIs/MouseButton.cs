using System;

namespace KVD.Utils.GUIs
{
	[Flags]
	public enum MouseButton : byte
	{
		Left = 1 << 0,
		Middle = 1 << 1,
		Right = 1 << 2,
	}
}
