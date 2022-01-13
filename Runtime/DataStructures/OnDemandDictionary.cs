using System;
using System.Collections.Generic;

#nullable enable

namespace KVD.Utils.DataStructures
{
	public class OnDemandDictionary<TKey, TVal> : Dictionary<TKey, TVal>
	{
		private readonly Func<TKey, TVal> _creatorFunc;

		public OnDemandDictionary(Func<TKey, TVal>? creatorFunc = null)
		{
#pragma warning disable 8603
			_creatorFunc = creatorFunc ?? (_ => default(TVal));
#pragma warning restore 8603
		}
		
		public new TVal this[TKey key]
		{
			get
			{
				if (TryGetValue(key, out var value))
				{
					return value;
				}
				value     = _creatorFunc(key);
				base[key] = value;
				return value;
			}
			set => base[key] = value;
		}
	}
}
