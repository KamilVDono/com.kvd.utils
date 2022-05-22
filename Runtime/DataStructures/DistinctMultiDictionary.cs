using System.Collections.Generic;
using System.Runtime.Serialization;

namespace KVD.Utils.DataStructures
{
	public class DistinctMultiDictionary<TKey, TElem> : Dictionary<TKey, HashSet<TElem>>
	{
		public DistinctMultiDictionary()
		{
		}
		public DistinctMultiDictionary(IDictionary<TKey, HashSet<TElem>> dictionary) : base(dictionary)
		{
		}
		public DistinctMultiDictionary(IDictionary<TKey, HashSet<TElem>> dictionary, IEqualityComparer<TKey> comparer)
			: base(dictionary, comparer)
		{
		}
		public DistinctMultiDictionary(IEqualityComparer<TKey> comparer) : base(comparer)
		{
		}
		public DistinctMultiDictionary(int capacity) : base(capacity)
		{
		}
		public DistinctMultiDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer)
		{
		}
		protected DistinctMultiDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
		public DistinctMultiDictionary(IEnumerable<KeyValuePair<TKey, HashSet<TElem>>> collection) : base(collection)
		{
		}
		public DistinctMultiDictionary(IEnumerable<KeyValuePair<TKey, HashSet<TElem>>> collection, IEqualityComparer<TKey> comparer) : base(collection, comparer)
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
