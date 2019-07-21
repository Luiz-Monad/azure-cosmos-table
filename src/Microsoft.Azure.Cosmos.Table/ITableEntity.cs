using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table
{
	public interface ITableEntity
	{
		string PartitionKey
		{
			get;
			set;
		}

		string RowKey
		{
			get;
			set;
		}

		DateTimeOffset Timestamp
		{
			get;
			set;
		}

		string ETag
		{
			get;
			set;
		}

		void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext);

		IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext);
	}
}
