namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class TablePermissions
	{
		public SharedAccessTablePolicies SharedAccessPolicies
		{
			get;
			private set;
		}

		public TablePermissions()
		{
			SharedAccessPolicies = new SharedAccessTablePolicies();
		}
	}
}
