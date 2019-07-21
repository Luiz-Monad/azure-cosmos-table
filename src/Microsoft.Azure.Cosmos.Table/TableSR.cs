namespace Microsoft.Azure.Cosmos.Table
{
	internal class TableSR
	{
		public const string ApiNotSupported = "{0} api is not supported in the current version.";

		public const string DirectModeNotSupported = "Direct mode is only supported with Azure Cosmos table API service.";

		public const string PremiumFeaturesSupportedInPremiumEndpoint = "Extension features are only supported by Azure Cosmos Table endpoints.";

		public const string PremiumFeaturesSupprotedInDirectModeOnly = "Only direct mode supports throughput and indexing policy.";

		public const string DirectModeSASNotSupported = "SAS is only supported in Gateway mode for Azure CosmosDB table API service.";

		public const string DocumentsEndpointNotSupported = "Only Cosmos table endpoint or azure storage table endpoint are supported.";

		public const string OperationNotSupportedInPremiumEndpoint = "The operation is not supported by Azure Cosmos Table endpoints. ";

		public const string SingleOrderBySupport = "Only single order by is supported";
	}
}
