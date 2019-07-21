using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class StorageExtendedErrorInformation
	{
		public string ErrorCode
		{
			get;
			internal set;
		}

		public string ErrorMessage
		{
			get;
			internal set;
		}

		public IDictionary<string, string> AdditionalDetails
		{
			get;
			internal set;
		}
	}
}
