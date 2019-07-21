using System;

namespace Microsoft.Azure.Cosmos.Table
{
	internal class SasQueryBuilder : UriQueryBuilder
	{
		public bool RequireHttps
		{
			get;
			private set;
		}

		public SasQueryBuilder(string sasToken)
		{
			AddRange(HttpWebUtility.ParseQueryString(sasToken));
		}

		public override void Add(string name, string value)
		{
			if (value != null)
			{
				value = Uri.EscapeDataString(value);
			}
			if (string.CompareOrdinal(name, "spr") == 0 && string.CompareOrdinal(value, "https") == 0)
			{
				RequireHttps = true;
			}
			base.Parameters.Add(name, value);
		}

		public override Uri AddToUri(Uri uri)
		{
			CommonUtility.AssertNotNull("uri", uri);
			if (RequireHttps && string.CompareOrdinal(uri.Scheme, Uri.UriSchemeHttps) != 0)
			{
				throw new ArgumentException("Cannot transform a Uri object using a StorageCredentials object that is marked HTTPS only.");
			}
			return AddToUriCore(uri);
		}
	}
}
