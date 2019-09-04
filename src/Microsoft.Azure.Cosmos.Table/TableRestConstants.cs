using System;
using System.Globalization;

namespace Microsoft.Azure.Cosmos.Table
{
	internal static class TableRestConstants
	{
		internal static class HeaderConstants
		{
			public static readonly string UserAgentComment = string.Format(CultureInfo.InvariantCulture, "(.NET CLR {0}; {1} {2})", Environment.Version, Environment.OSVersion.Platform, Environment.OSVersion.Version);

			public const string UserAgentProductName = "Azure-Cosmos-Table";

			internal const string AcceptCharset = "Accept-Charset";

			internal const string AcceptCharsetValue = "UTF-8";

			internal const string MaxDataServiceVersion = "MaxDataServiceVersion";

			internal const string MaxDataServiceVersionValue = "3.0;NetFx";

			internal const string DataServiceVersion = "DataServiceVersion";

			internal const string DataServiceVersionValue = "3.0;";

			internal const string PostTunnelling = "X-HTTP-Method";

			internal const string IfMatch = "If-Match";

			internal const string Prefer = "Prefer";

			internal const string PreferReturnContent = "return-content";

			internal const string PreferReturnNoContent = "return-no-content";

			public const string StorageVersionHeader = "x-ms-version";

			public const string TargetStorageVersion = "2018-03-28";

			public const string Date = "x-ms-date";

			public const string RequestIdHeader = "x-ms-request-id";

			internal const string PayloadContentTypeHeader = "Content-Type";

			public const string EtagHeader = "ETag";

			internal const string StorageErrorCodeHeader = "x-ms-error-code";
		}

		public static class QueryConstants
		{
			public const string Snapshot = "snapshot";

			public const string ShareSnapshot = "sharesnapshot";

			public const string SignedStart = "st";

			public const string SignedExpiry = "se";

			public const string SignedResource = "sr";

			public const string SignedResourceTypes = "srt";

			public const string SignedServices = "ss";

			public const string SignedProtocols = "spr";

			public const string SignedIP = "sip";

			public const string SasTableName = "tn";

			public const string SignedPermissions = "sp";

			public const string StartPartitionKey = "spk";

			public const string StartRowKey = "srk";

			public const string EndPartitionKey = "epk";

			public const string EndRowKey = "erk";

			public const string SignedIdentifier = "si";

			public const string SignedKey = "sk";

			public const string SignedVersion = "sv";

			public const string Signature = "sig";

			public const string CacheControl = "rscc";

			public const string ContentType = "rsct";

			public const string ContentEncoding = "rsce";

			public const string ContentLanguage = "rscl";

			public const string ContentDisposition = "rscd";

			public const string ApiVersion = "api-version";

			public const string MessageTimeToLive = "messagettl";

			public const string VisibilityTimeout = "visibilitytimeout";

			public const string NumOfMessages = "numofmessages";

			public const string PopReceipt = "popreceipt";

			public const string ResourceType = "restype";

			public const string Component = "comp";

			public const string CopyId = "copyid";
		}

		public static class AnalyticsConstants
		{
			public const string LogsContainer = "$logs";

			public const string MetricsHourPrimaryTransactionsTable = "$MetricsHourPrimaryTransactionsTable";

			public const string MetricsMinutePrimaryTransactionsTable = "$MetricsMinutePrimaryTransactionsTable";

			public const string MetricsHourSecondaryTransactionsTable = "$MetricsHourSecondaryTransactionsTable";

			public const string MetricsMinuteSecondaryTransactionsTable = "$MetricsMinuteSecondaryTransactionsTable";

			public const string LoggingVersionV1 = "1.0";

			public const string MetricsVersionV1 = "1.0";
		}

		public static class RestExceptionErrorMessage
		{
			public const string BadRequest = "The remote server returned an error: (400) Bad Request.";
		}

		public static readonly TimeSpan DefaultClientSideTimeout = TimeSpan.FromMinutes(5.0);

		public static readonly TimeSpan DefaultHttpClientTimeout = TimeSpan.FromMinutes(2.0);

		public static readonly TimeSpan MaximumRetryBackoff = TimeSpan.FromHours(1.0);

		internal static readonly int MaximumAllowedRetentionDays = 365;

		internal static readonly TimeSpan ResponseParserCancellationFallbackDelay = TimeSpan.FromSeconds(5.0);

		internal const int DefaultBufferSize = 65536;

		public const string ErrorMessage = "Message";

		internal const string ErrorMessagePreview = "message";

		public const string ErrorExceptionMessage = "ExceptionMessage";

		public const string ErrorExceptionStackTrace = "StackTrace";

		public const string ErrorCode = "Code";

		internal const string ErrorCodePreview = "code";

		internal const string ErrorException = "exceptiondetails";

		internal const string JsonLightAcceptHeaderValue = "application/json;odata=minimalmetadata";

		internal const string LightAcceptHeaderValue = "odata=minimalmetadata";

		internal const string JsonFullMetadataAcceptHeaderValue = "application/json;odata=fullmetadata";

		internal const string FullMetadataAcceptHeaderValue = "odata=fullmetadata";

		internal const string JsonNoMetadataAcceptHeaderValue = "application/json;odata=nometadata";

		internal const string NoMetadataAcceptHeaderValue = "odata=nometadata";

		internal const string NoMetadata = "odata=nometadata";

		internal const string JsonContentTypeHeaderValue = "application/json";

		public const string ContentTypeElement = "Content-Type";

		public const int KB = 1024;

		internal const string ETagPrefix = "\"datetime'";

		internal const string OdataTypeString = "@odata.type";

		internal const string EdmBinary = "Edm.Binary";

		internal const string EdmBoolean = "Emd.Boolean";

		internal const string EdmDateTime = "Edm.DateTime";

		internal const string EdmDouble = "Edm.Double";

		internal const string EdmGuid = "Edm.Guid";

		internal const string EdmInt32 = "Edm.Int32";

		internal const string EdmInt64 = "Edm.Int64";

		internal const string EdmString = "Edm.String";

		internal const string BatchContentType = "multipart/mixed";

		internal const string BatchBoundaryPrefix = "boundary=batch_";

		internal const string BatchBoundaryMarker = "multipart/mixed; boundary=batch_";

		internal const string ChangesetBoundaryMarker = "Content-Type: multipart/mixed; boundary=changeset_";

		internal const string BatchSeparator = "--batch_";

		internal const string ChangesetSeparator = "--changeset_";

		internal const string ContentTypeApplicationHttp = "Content-Type: application/http";

		internal const string ContentTransferEncodingBinary = "Content-Transfer-Encoding: binary";

		internal const string ContentTypeApplicationJson = "Content-Type: application/json";

		internal const string HTTP1_1 = "HTTP/1.1";
	}
}
