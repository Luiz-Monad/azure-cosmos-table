using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class CorsProperties
	{
		public IList<CorsRule> CorsRules
		{
			get;
			internal set;
		}

		public CorsProperties()
		{
			CorsRules = new List<CorsRule>();
		}
	}
}
