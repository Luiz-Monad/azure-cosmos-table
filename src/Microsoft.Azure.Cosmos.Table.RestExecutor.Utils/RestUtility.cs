using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Utils
{
	internal static class RestUtility
	{
		public static string GetFirstHeaderValue<T>(IEnumerable<T> headerValues) where T : class
		{
			if (headerValues != null)
			{
				T val = headerValues.FirstOrDefault();
				if (val != null)
				{
					return val.ToString().TrimStart(Array.Empty<char>());
				}
			}
			return null;
		}

		public static TimeSpan MaxTimeSpan(TimeSpan val1, TimeSpan val2)
		{
			if (!(val1 > val2))
			{
				return val2;
			}
			return val1;
		}

		internal static T RunWithoutSynchronizationContext<T>(Func<T> actionToRun)
		{
			SynchronizationContext current = SynchronizationContext.Current;
			try
			{
				SynchronizationContext.SetSynchronizationContext(null);
				return actionToRun();
			}
			finally
			{
				SynchronizationContext.SetSynchronizationContext(current);
			}
		}
	}
}
