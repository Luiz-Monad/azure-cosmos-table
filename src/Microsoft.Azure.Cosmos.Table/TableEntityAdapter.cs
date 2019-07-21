using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table
{
	public class TableEntityAdapter<T> : TableEntity
	{
		public T OriginalEntity
		{
			get;
			set;
		}

		public TableEntityAdapter()
		{
		}

		public TableEntityAdapter(T originalEntity)
		{
			OriginalEntity = originalEntity;
		}

		public TableEntityAdapter(T originalEntity, string partitionKey, string rowKey)
			: base(partitionKey, rowKey)
		{
			OriginalEntity = originalEntity;
		}

		public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
		{
			OriginalEntity = TableEntity.ConvertBack<T>(properties, operationContext);
		}

		public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
		{
			return TableEntity.Flatten(OriginalEntity, operationContext);
		}
	}
}
