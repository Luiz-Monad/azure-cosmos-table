using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table
{
	public class TableQuerySegment<TElement> : IEnumerable<TElement>, IEnumerable
	{
		private TableContinuationToken continuationToken;

		public double? RequestCharge
		{
			get;
			internal set;
		}

		public List<TElement> Results
		{
			get;
			internal set;
		}

		public TableContinuationToken ContinuationToken
		{
			get
			{
				if (continuationToken != null)
				{
					return continuationToken;
				}
				return null;
			}
			internal set
			{
				continuationToken = value;
			}
		}

		internal TableQuerySegment(List<TElement> result)
		{
			Results = result;
		}

		internal TableQuerySegment(ResultSegment<TElement> resSeg)
			: this(resSeg.Results)
		{
			continuationToken = resSeg.ContinuationToken;
		}

		public IEnumerator<TElement> GetEnumerator()
		{
			return Results.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Results.GetEnumerator();
		}
	}
}
