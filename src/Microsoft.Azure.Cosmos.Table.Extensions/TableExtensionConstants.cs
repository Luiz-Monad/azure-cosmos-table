namespace Microsoft.Azure.Cosmos.Table.Extensions
{
	internal static class TableExtensionConstants
	{
		internal class UriHelper
		{
			internal const string OR = "OR";

			internal const string AND = "AND";

			internal const string NOT = "NOT";

			internal const string EQ = "=";

			internal const string NE = "!=";

			internal const string LT = "<";

			internal const string LE = "<=";

			internal const string GT = ">";

			internal const string GE = ">=";

			internal const string ADD = "+";

			internal const string SUB = "-";

			internal const string MUL = "*";

			internal const string DIV = "/";

			internal const string MOD = "%";

			internal const string NEGATE = "!";
		}

		internal static class AppSettings
		{
			internal const string EnableTableQueryOptions = "EnableTableQueryOptions";

			internal const string TableQueryMaxItemCount = "TableQueryMaxItemCount";

			internal const string TableQueryEnableScan = "TableQueryEnableScan";

			internal const string TableQueryMaxDegreeOfParallelism = "TableQueryMaxDegreeOfParallelism";

			internal const string TableQueryContinuationTokenLimitInKb = "TableQueryContinuationTokenLimitInKb";

			internal const string UseRestApiExecutor = "UseRestApiExecutor";
		}

		internal static class AppSettingsDefaults
		{
			internal const bool EnableTableQueryOptions = false;

			internal const int TableQueryMaxDegreeOfParallelism = -1;

			internal const int TableQueryMaxItemCount = 1000;

			internal const bool TableQueryEnableScan = false;

			internal const bool UseRestApiExecutor = false;
		}

		internal static class SR
		{
			internal const string ApiNotSupported = "{0} api is not supported in the current version.";

			internal const string EntityAlreadyExists = "The specified entity already exists.";

			internal const string EntityNotFound = "The specified entity was not found.";

			internal const string EntityTooLarge = "The entity is larger than the maximum size permitted.";

			internal const string InternalServerError = "Server encountered an internal error.Please try again after some time.";

			internal const string TableAlreadyExists = "The specified table already exists.";

			internal const string TableNotFound = "The specified table was not found.";

			internal const string UpdateConditionNotSatisfied = "The update condition specified in the request was not satisfied.";

			internal const string UpdateConditionNotSatisfiedBatch = "The update condition specified in the request was not satisfied.";

			internal const string RequestBodyTooLarge = "The request body is too large and exceeds the maximum permissible limit.";

			internal const string ResourceNotFound = "The specified resource does not exist.";

			internal const string SettingPropertyValueNotSupported = "Setting the value of the property '{0}' is not supported.";

			internal const string QueryTokenIsNotSupported = "QueryToken of type '{0}' is not supported.";

			internal const string EncryptionNotSupportedMessage = "Encryption policy is not supported in the current version.";

			internal const string LocationModeNotSupportedMessage = "Secondary LocationMode is not supported in the current version.";

			internal const string FilterTextNotSupportedMessage = "FilterText is not supported for query enumeration";

			internal const string TableQueryOptionsNotSupportedInAppConfig = "Options set in the App.config are not supported. Please use TableRequestOptions.";

			internal const string InvalidEtagFormat = "Invalid Etag format.";

			internal const string InvalidDuplicateRow = "The batch request contains multiple changes with same row key. An entity can appear only once in a batch request.";
		}

		internal static class ErrorCodeStrings
		{
			internal const string ResourceNotFound = "ResourceNotFound";

			internal const string TooManyRequests = "TooManyRequests";

			internal const string InvalidDuplicateRow = "InvalidDuplicateRow";
		}

		internal const string TableDatabaseName = "TablesDB";

		internal const string ExtensionName = "cosmos-table-sdk";

		internal const string SystemBatchOperationStoredProcedureId = "__.sys.tablesBatchOperation";

		internal const int MaxPartitionKeySizeInChars = 1024;

		internal const int MaxRowKeySizeInChars = 254;

		internal const string UnusedContinuationTokenIdentifier = "NA";
	}
}
