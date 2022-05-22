using System.Collections.Generic;
using System.Runtime.Serialization;

namespace KVD.Utils.DataStructures
{
	public class MultiDictionary<TKey, TElem> : Dictionary<TKey, List<TElem>>
	{
		public MultiDictionary()
		{
		}
		public MultiDictionary(IDictionary<TKey, List<TElem>> dictionary) : base(dictionary)
		{
		}
		public MultiDictionary(IDictionary<TKey, List<TElem>> dictionary, IEqualityComparer<TKey> comparer)
			: base(dictionary, comparer)
		{
		}
		public MultiDictionary(IEqualityComparer<TKey> comparer) : base(comparer)
		{
		}
		public MultiDictionary(int capacity) : base(capacity)
		{
		}
		public MultiDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer)
		{
		}
		protected MultiDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
		public MultiDictionary(IEnumerable<KeyValuePair<TKey, List<TElem>>> collection) : base(collection)
		{
		}
		public MultiDictionary(IEnumerable<KeyValuePair<TKey, List<TElem>>> collection, IEqualityComparer<TKey> comparer) : base(collection, comparer)
		{
		}

		public void Add(TKey key, TElem element)
		{
			if (!TryGetValue(key, out var elements))
			{
				elements = new();
				Add(key, elements);
			}
			elements.Add(element);
		}
	}
}
