using System;
using KVD.Utils.DataStructures;
using KVD.Utils.Extensions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KVD.Utils
{
	public struct SearchPattern : IEquatable<SearchPattern>
	{
		int _hashCode;
		ReadOnlyMemory<char> _backingString;
		UnsafeArray<PartRange> _partRanges;

		public bool IsEmpty => _partRanges.Length == 0;

		public SearchPattern(ReadOnlyMemory<char> searchContext, Allocator allocator) : this()
		{
			SetNewPattern(searchContext, allocator);
		}

		public void Dispose()
		{
			_partRanges.Dispose();
		}

		public bool Update(ReadOnlyMemory<char> searchContext)
		{
			var newHash = searchContext.GetHashCode();
			if (newHash == _hashCode)
			{
				return false;
			}
			SetNewPattern(searchContext, _partRanges.Allocator);
			return true;
		}

		public bool HasSearchInterest(in ReadOnlySpan<char> content)
		{
			if (_partRanges.Length == 0)
			{
				return true;
			}

			foreach (var partRange in _partRanges)
			{
				var pattern = _backingString.Slice(partRange.start, partRange.length).Span;
				if (content.Contains(pattern, StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		public bool HasExactSearch(in ReadOnlySpan<char> content)
		{
			foreach (var partRange in _partRanges)
			{
				var pattern = _backingString.Slice(partRange.start, partRange.length).Span;
				if (content.Equals(pattern, StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		void SetNewPattern(ReadOnlyMemory<char> searchContext, Allocator allocator)
		{
			if (_partRanges.IsCreated)
			{
				_partRanges.Dispose();
			}
			_backingString = searchContext;
			_hashCode = searchContext.GetHashCode();
			if (searchContext.IsEmpty || searchContext.Span.IsWhiteSpace())
			{
				_partRanges = new UnsafeArray<PartRange>(0, allocator);
			}
			else
			{
				var ranges = new UnsafeList<PartRange>(16, Allocator.Temp);

				var start = 0;
				var inQuotes = false;
				var searchString = searchContext.Span;
				for (var i = 0; i < searchContext.Length; i++)
				{
					var c = searchString[i];
					if (c == '\"')
					{
						inQuotes = !inQuotes;

						if (start != i)
						{
							ranges.Add(new PartRange(start, i));
						}
						start = i + 1;
					}
					else if (c is ';' or '.' or ' ' && !inQuotes)
					{
						if (start != i)
						{
							ranges.Add(new PartRange(start, i));
						}
						start = i + 1;
					}
				}

				if (start != searchContext.Length)
				{
					ranges.Add(new PartRange(start, searchContext.Length));
				}

				_partRanges = ranges.ToUnsafeArray(allocator);
				ranges.Dispose();
			}
		}

		public bool Equals(SearchPattern other)
		{
			return _hashCode == other._hashCode;
		}

		public override bool Equals(object obj)
		{
			return obj is SearchPattern other && Equals(other);
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}

		public static bool operator ==(SearchPattern left, SearchPattern right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(SearchPattern left, SearchPattern right)
		{
			return !left.Equals(right);
		}

		readonly struct PartRange
		{
			public readonly int start;
			public readonly int length;

			public PartRange(int start, int end)
			{
				this.start = start;
				length = end - start;
			}
		}
	}
}
