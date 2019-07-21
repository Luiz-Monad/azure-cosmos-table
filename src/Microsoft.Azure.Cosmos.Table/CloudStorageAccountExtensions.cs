using System;

namespace Microsoft.Azure.Cosmos.Table
{
	public static class CloudStorageAccountExtensions
	{
		public static CloudTableClient CreateCloudTableClient(this CloudStorageAccount account, TableClientConfiguration configuration = null)
		{
			if (account.TableEndpoint == null)
			{
				throw new InvalidOperationException("No table endpoint configured.");
			}
			if (account.Credentials == null)
			{
				throw new InvalidOperationException("No credentials provided.");
			}
			return new CloudTableClient(account.TableStorageUri, account.Credentials, configuration);
		}
	}
}
