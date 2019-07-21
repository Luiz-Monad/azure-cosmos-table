using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class TableBatchResult : List<TableResult>
	{
		private IList<TableResult> tableResultList = new List<TableResult>();

		public double? RequestCharge
		{
			get;
			set;
		}
	}
}
