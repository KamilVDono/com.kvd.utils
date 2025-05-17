namespace KVD.Utils.DataStructures
{
	public unsafe struct UnsafeEnumerator<T> where T : unmanaged
	{
		readonly T* _array;
		readonly uint _length;
		uint _index;

		public UnsafeEnumerator(T* array, uint length)
		{
			_array = array;
			_length = length;
			_index = uint.MaxValue;
		}

		public void Dispose() {}

		public bool MoveNext()
		{
			unchecked
			{
				return ++_index < _length;
			}
		}

		public ref T Current => ref _array[_index];
	}
}
