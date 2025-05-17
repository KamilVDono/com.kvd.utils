using System;
using KVD.Utils.DataStructures;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;

namespace KVD.Utils.IO
{
	public static unsafe class UnityFileRead
	{
		public static UnsafeArray<T> ToNewBuffer<T>(string filepath, Allocator allocator) where T : unmanaged
		{
			var fileInfo = default(FileInfoResult);
			var fileInfoHandle = AsyncReadManager.GetFileInfo(filepath, &fileInfo);
			fileInfoHandle.JobHandle.Complete();

			var dataCount = (uint)(fileInfo.FileSize/UnsafeUtility.SizeOf<T>());
			fileInfoHandle.Dispose();

			return ToNewBuffer<T>(filepath, 0, dataCount, allocator);
		}

		public static UnsafeArray<T> ToNewBuffer<T>(string filepath, long offset, uint count, Allocator allocator) where T : unmanaged
		{
			var buffer = new UnsafeArray<T>(count, allocator, NativeArrayOptions.UninitializedMemory);
			var readCommand = new ReadCommand
			{
				Offset = offset,
				Size = UnsafeUtility.SizeOf<T>()*count,
				Buffer = buffer.Ptr,
			};
			var readHandle = AsyncReadManager.Read(filepath, &readCommand, 1);
			AsyncReadManager.CloseCachedFileAsync(filepath, readHandle.JobHandle).Complete();
			readHandle.Dispose();
			return buffer;
		}

		public static UnsafeArray<T> ToNewBuffer<T>(in FileHandle file, long offset, uint count, Allocator allocator) where T : unmanaged
		{
			var buffer = new UnsafeArray<T>(count, allocator, NativeArrayOptions.UninitializedMemory);
			var readCommand = new ReadCommand
			{
				Offset = offset,
				Size = UnsafeUtility.SizeOf<T>()*count,
				Buffer = buffer.Ptr,
			};
			var readCommandArray = new ReadCommandArray
			{
				ReadCommands = &readCommand,
				CommandCount = 1,
			};
			var readHandle = AsyncReadManager.Read(file, readCommandArray);
			readHandle.JobHandle.Complete();
			readHandle.Dispose();
			return buffer;
		}

		public static ReadHandle ToNewBufferAsync<T>(string filepath, Allocator allocator, out UnsafeArray<T> result, out UnsafeArray<ReadCommand> readCommand) where T : unmanaged
		{
			var fileInfo = default(FileInfoResult);
			AsyncReadManager.GetFileInfo(filepath, &fileInfo).JobHandle.Complete();

			var dataCount = (uint)(fileInfo.FileSize/UnsafeUtility.SizeOf<T>());

			return ToNewBufferAsync(filepath, 0, dataCount, allocator, out result, out readCommand);
		}

		public static ReadHandle ToNewBufferAsync<T>(
			string filepath, long offset, uint count, Allocator allocator, out UnsafeArray<T> result, out UnsafeArray<ReadCommand> readCommand) where T : unmanaged
		{
			result = new UnsafeArray<T>(count, allocator, NativeArrayOptions.UninitializedMemory);
			readCommand = new UnsafeArray<ReadCommand>(1, allocator, NativeArrayOptions.UninitializedMemory);
			readCommand[0] = new()
			{
				Offset = offset,
				Size = UnsafeUtility.SizeOf<T>()*count,
				Buffer = result.Ptr,
			};
			return AsyncReadManager.Read(filepath, readCommand.Ptr, 1);
		}

		public static ReadHandle ToExistingBuffer(string filepath, long offset, long size, void* buffer)
		{
			var readCommand = new ReadCommand
			{
				Offset = offset,
				Size = size,
				Buffer = buffer,
			};
			return AsyncReadManager.Read(filepath, &readCommand, 1);
		}

		public static ReadStatus ToExistingBufferWithClose(string filepath, long offset, Span<byte> buffer)
		{
			fixed (byte* ptr = buffer)
			{
				var readHandle = ToExistingBuffer(filepath, offset, buffer.Length, ptr);
				readHandle.JobHandle.Complete();
				var status = readHandle.Status;
				if (status != ReadStatus.Complete)
				{
					UnityEngine.Debug.LogError($"Failed to read file: {filepath} read status: {readHandle.Status}");
				}

				readHandle.Dispose();
				AsyncReadManager.CloseCachedFileAsync(filepath).Complete();
				return status;
			}
		}

		public static void ToExistingBuffer(string filepath, long offset, NativeSlice<byte> buffer)
		{
			var readHandle = ToExistingBuffer(filepath, offset, buffer.Length, buffer.GetUnsafePtr());
			AsyncReadManager.CloseCachedFileAsync(filepath, readHandle.JobHandle).Complete();
			readHandle.Dispose();
		}
	}
}
