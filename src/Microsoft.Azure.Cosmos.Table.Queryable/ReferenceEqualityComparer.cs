using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal class ReferenceEqualityComparer : IEqualityComparer
	{
		protected ReferenceEqualityComparer()
		{
		}

		bool IEqualityComparer.Equals(object x, object y)
		{
			return x == y;
		}

		int IEqualityComparer.GetHashCode(object obj)
		{
			return obj?.GetHashCode() ?? 0;
		}
	}
	internal sealed class ReferenceEqualityComparer<T> : ReferenceEqualityComparer, IEqualityComparer<T>
	{
		private static ReferenceEqualityComparer<T> instance;

		internal static ReferenceEqualityComparer<T> Instance
		{
			get
			{
				if (instance == null)
				{
					ReferenceEqualityComparer<T> value = new ReferenceEqualityComparer<T>();
					Interlocked.CompareExchange(ref instance, value, null);
				}
				return instance;
			}
		}

		private ReferenceEqualityComparer()
		{
		}

		public bool Equals(T x, T y)
		{
			return (object)x == (object)y;
		}

		public int GetHashCode(T obj)
		{
			return obj?.GetHashCode() ?? 0;
		}
	}
}
