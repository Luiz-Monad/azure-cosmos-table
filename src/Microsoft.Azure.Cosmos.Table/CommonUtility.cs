using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.Cosmos.Table
{
	internal static class CommonUtility
	{
		private static readonly int[] PathStylePorts = new int[20]
		{
			10000,
			10001,
			10002,
			10003,
			10004,
			10100,
			10101,
			10102,
			10103,
			10104,
			11000,
			11001,
			11002,
			11003,
			11004,
			11100,
			11101,
			11102,
			11103,
			11104
		};

		internal static void AssertNotNullOrEmpty(string paramName, string value)
		{
			AssertNotNull(paramName, value);
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException("The argument must not be empty string.", paramName);
			}
		}

		internal static void AssertNotNull(string paramName, object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException(paramName);
			}
		}

		internal static IEnumerable<T> LazyEnumerable<T>(Func<TableContinuationToken, ResultSegment<T>> segmentGenerator, long maxResults)
		{
			ResultSegment<T> currentSeg = segmentGenerator(null);
			long count = 0L;
			while (true)
			{
				foreach (T result in currentSeg.Results)
				{
					yield return result;
					count++;
					if (count >= maxResults)
					{
						break;
					}
				}
				if (count < maxResults && currentSeg.ContinuationToken != null)
				{
					currentSeg = segmentGenerator(currentSeg.ContinuationToken);
					continue;
				}
				break;
			}
		}

		internal static void ArgumentOutOfRange(string paramName, object value)
		{
			throw new ArgumentOutOfRangeException(paramName, string.Format(CultureInfo.InvariantCulture, "The argument is out of range. Value passed: {0}", value));
		}

		internal static void AssertInBounds<T>(string paramName, T val, T min, T max) where T : IComparable
		{
			if (val.CompareTo(min) < 0)
			{
				throw new ArgumentOutOfRangeException(paramName, string.Format(CultureInfo.InvariantCulture, "The argument '{0}' is smaller than minimum of '{1}'", paramName, min));
			}
			if (val.CompareTo(max) > 0)
			{
				throw new ArgumentOutOfRangeException(paramName, string.Format(CultureInfo.InvariantCulture, "The argument '{0}' is larger than maximum of '{1}'", paramName, max));
			}
		}

		public static TimeSpan MaxTimeSpan(TimeSpan val1, TimeSpan val2)
		{
			if (!(val1 > val2))
			{
				return val2;
			}
			return val1;
		}

		internal static bool UsePathStyleAddressing(Uri uri)
		{
			AssertNotNull("uri", uri);
			if (uri.HostNameType != UriHostNameType.Dns)
			{
				return true;
			}
			return PathStylePorts.Contains(uri.Port);
		}
	}
}
