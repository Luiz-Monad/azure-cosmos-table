namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class LoggingProperties
	{
		public string Version
		{
			get;
			set;
		}

		public LoggingOperations LoggingOperations
		{
			get;
			set;
		}

		public int? RetentionDays
		{
			get;
			set;
		}

		public LoggingProperties()
			: this("1.0")
		{
		}

		public LoggingProperties(string version)
		{
			Version = version;
		}
	}
}
