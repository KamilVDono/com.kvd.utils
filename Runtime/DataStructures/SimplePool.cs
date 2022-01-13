using System;
using System.Collections.Generic;

#nullable enable

namespace KVD.Utils.DataStructures
{
	public class SimplePool<T>
	{
		private readonly Stack<T> _pool;
		private readonly Func<T> _creator;

		public SimplePool(int initSize, Func<T> creator)
		{
			_pool    = new(initSize);
			_creator = creator;
		}
		
		public T Get()
		{
			return _pool.Count < 1 ? _creator() : _pool.Pop();
		}

		public void Return(T element)
		{
			_pool.Push(element);
		}

		public ScopedBorrow Borrow()
		{
			return new(this, Get());
		} 
		
		public readonly struct ScopedBorrow : IDisposable
		{
			private readonly SimplePool<T> _pool;
			private readonly T _element;

			public T Element => _element;

			internal ScopedBorrow(SimplePool<T> pool, T element)
			{
				_pool    = pool;
				_element = element;
			}

			public void Dispose()
			{
				_pool.Return(_element);
			}
			
			public static implicit operator T(ScopedBorrow borrow) => borrow.Element;
		}
	}
}
