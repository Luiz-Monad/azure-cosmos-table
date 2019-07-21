using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class TableResultSegment : IEnumerable<CloudTable>, IEnumerable
	{
		private TableContinuationToken continuationToken;

		public IList<CloudTable> Results
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

		internal TableResultSegment(List<CloudTable> result)
		{
			Results = result;
		}

		public IEnumerator<CloudTable> GetEnumerator()
		{
			return Results.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Results.GetEnumerator();
		}
	}
}
