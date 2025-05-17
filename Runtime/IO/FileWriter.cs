using System;
using System.IO;
using KVD.Utils.DataStructures;

namespace KVD.Utils.IO
{
	public readonly struct FileWriter : IDisposable
	{
		readonly FileStream _stream;

		public uint Position => (uint)_stream.Position;

		public FileWriter(string filepath, FileMode mode = FileMode.Create)
		{
			_stream = new FileStream(filepath, mode, FileAccess.Write, FileShare.None);
		}

		public unsafe void Write<T>(T value) where T : unmanaged
		{
			var size = sizeof(T);
			var bytePtr = (byte*)&value;
			var buffer = new ReadOnlySpan<byte>(bytePtr, size);
			_stream.Write(buffer);
		}

		public unsafe void Write<T>(UnsafeSpan<T> span) where T : unmanaged
		{
			var buffer = new ReadOnlySpan<byte>(span.Ptr, (int)(span.Length*sizeof(T)));
			_stream.Write(buffer);
		}

		public unsafe void Write<T>(T[] array) where T : unmanaged
		{
			fixed (T* arrayPtr = array)
			{
				var buffer = new ReadOnlySpan<byte>(arrayPtr, array.Length*sizeof(T));
				_stream.Write(buffer);
			}
		}

		public void Dispose()
		{
			_stream.Dispose();
		}
	}
}
