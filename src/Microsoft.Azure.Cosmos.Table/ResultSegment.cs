using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table
{
	public class ResultSegment<TElement>
	{
		private TableContinuationToken continuationToken;

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

		internal ResultSegment(List<TElement> result)
		{
			Results = result;
		}
	}
}
