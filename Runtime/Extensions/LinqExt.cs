using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;

#nullable enable

namespace KVD.Utils.Extensions
{
	public static class LinqExt
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IEnumerable<T> Yield<T>(this T obj)
		{
			yield return obj;
		}

		public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> iterator)
		{
			return iterator.Where(el => el != null).Cast<T>();
		}

		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			HashSet<TKey> seenKeys = new HashSet<TKey>();
			foreach (var element in source)
			{
				if (seenKeys.Add(keySelector(element)))
				{
					yield return element;
				}
			}
		}

		public static void ForEachSlow<T>(this IEnumerable<T> elements, Action<T> action)
		{
			foreach (var element in elements)
			{
				action(element);
			}
		}
		
		public static void ForEach<T>(this List<T> elements, Action<T> action)
		{
			foreach (var element in elements)
			{
				action(element);
			}
		}
		
		public static void ForEach<T>(this T[] elements, Action<T> action)
		{
			foreach (var element in elements)
			{
				action(element);
			}
		}
		
		public static void ForEach<T>(this NativeArray<T> elements, Action<T> action) where T : struct
		{
			foreach (var element in elements)
			{
				action(element);
			}
		}

		public static bool HasAtLeast<T>(this IEnumerable<T> source, int minCount)
		{
			return source is ICollection<T> collection ? collection.Count >= minCount : source.Skip(minCount-1).Any();
		}

		public static IEnumerable<T> SkipLastN<T>(this IEnumerable<T> source, int n)
		{
			var  it = source.GetEnumerator();
			bool hasRemainingItems;
			var  cache = new Queue<T>(n+1);

			do
			{
				hasRemainingItems = it.MoveNext();
				if (!hasRemainingItems)
				{
					continue;
				}
				cache.Enqueue(it.Current);
				if (cache.Count > n)
				{
					yield return cache.Dequeue();
				}
			}
			while (hasRemainingItems);

			it.Dispose();
		}

		public static T FirstOrAny<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : class
		{
			// ReSharper disable once PossibleMultipleEnumeration
			var itemThatSatisfyPredicate = source.FirstOrDefault(predicate);
			// ReSharper disable once PossibleMultipleEnumeration
			return itemThatSatisfyPredicate ?? source.First();
		}
	}
}
