namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class MetricsProperties
	{
		public string Version
		{
			get;
			set;
		}

		public MetricsLevel MetricsLevel
		{
			get;
			set;
		}

		public int? RetentionDays
		{
			get;
			set;
		}

		public MetricsProperties()
			: this("1.0")
		{
		}

		public MetricsProperties(string version)
		{
			Version = version;
		}
	}
}
