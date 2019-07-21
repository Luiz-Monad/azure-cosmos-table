namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class DeleteRetentionPolicy
	{
		public bool Enabled
		{
			get;
			set;
		}

		public int? RetentionDays
		{
			get;
			set;
		}
	}
}
