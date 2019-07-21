namespace Microsoft.Azure.Cosmos.Table
{
	public class EntityPropertyConverterOptions
	{
		private string propertyNameDelimiter = "_";

		public string PropertyNameDelimiter
		{
			get
			{
				return propertyNameDelimiter;
			}
			set
			{
				propertyNameDelimiter = value;
			}
		}
	}
}
