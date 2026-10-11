using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace ExtendedArithmetic
{
	public static class EnumerableExtensionMethods
	{
		public static IEnumerable<T> PadRight<T>(this IEnumerable<T> source, int totalWidth)
		{
			return source.PadRight(totalWidth, default(T));
		}

		public static IEnumerable<T> PadLeft<T>(this IEnumerable<T> source, int totalWidth)
		{
			return source.PadLeft(totalWidth, default(T));
		}

		public static IEnumerable<T> PadRight<T>(this IEnumerable<T> source, int totalWidth, T paddingElement)
		{
			int count = 0;

			// use "Take" in case "length" is smaller than the source's length.
			foreach (T item in source.Take(totalWidth))
			{
				yield return item;
				count++;
			}
			for (; count < totalWidth; count++)
			{
				yield return paddingElement;
			}

			yield break;
		}

		public static IEnumerable<T> PadLeft<T>(this IEnumerable<T> source, int totalWidth, T paddingElement)
		{
			int count = count = source.Count();

			int diff = totalWidth - count;
			if (diff > 0)
			{
				foreach (var item in Enumerable.Repeat(paddingElement, diff))
				{
					yield return item;
				}
			}

			// Yield the rest of the collection
			foreach (T item in source)
			{
				yield return item;
			}

			yield break;
		}
	}
}
