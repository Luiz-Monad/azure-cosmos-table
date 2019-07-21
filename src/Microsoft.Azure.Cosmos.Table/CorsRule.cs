using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class CorsRule
	{
		private IList<string> allowedOrigins;

		private IList<string> exposedHeaders;

		private IList<string> allowedHeaders;

		public IList<string> AllowedOrigins
		{
			get
			{
				return allowedOrigins ?? (allowedOrigins = new List<string>());
			}
			set
			{
				allowedOrigins = value;
			}
		}

		public IList<string> ExposedHeaders
		{
			get
			{
				return exposedHeaders ?? (exposedHeaders = new List<string>());
			}
			set
			{
				exposedHeaders = value;
			}
		}

		public IList<string> AllowedHeaders
		{
			get
			{
				return allowedHeaders ?? (allowedHeaders = new List<string>());
			}
			set
			{
				allowedHeaders = value;
			}
		}

		public CorsHttpMethods AllowedMethods
		{
			get;
			set;
		}

		public int MaxAgeInSeconds
		{
			get;
			set;
		}
	}
}
