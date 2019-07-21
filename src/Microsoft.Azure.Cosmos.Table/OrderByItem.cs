namespace Microsoft.Azure.Cosmos.Table
{
	internal class OrderByItem
	{
		public const string ASC = "asc";

		public const string DESC = "desc";

		public string PropertyName
		{
			get;
			private set;
		}

		public string Order
		{
			get;
			private set;
		}

		public OrderByItem(string propertyName, string order = "asc")
		{
			PropertyName = propertyName;
			Order = order;
		}
	}
}
