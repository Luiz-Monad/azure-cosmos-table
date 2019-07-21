using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.Cosmos.Table
{
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
	[DebuggerNonUserCode]
	[CompilerGenerated]
	internal class TableResources
	{
		private static ResourceManager resourceMan;

		private static CultureInfo resourceCulture;

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (resourceMan == null)
				{
					resourceMan = new ResourceManager("Microsoft.Azure.Cosmos.Table.TableResources", typeof(TableResources).Assembly);
				}
				return resourceMan;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return resourceCulture;
			}
			set
			{
				resourceCulture = value;
			}
		}

		internal static string AmbiguousAuthScheme => ResourceManager.GetString("AmbiguousAuthScheme", resourceCulture);

		internal static string AtomFormatNotSupported => ResourceManager.GetString("AtomFormatNotSupported", resourceCulture);

		internal static string InvalidAccountDuplicateService => ResourceManager.GetString("InvalidAccountDuplicateService", resourceCulture);

		internal static string InvalidAccountResourceType => ResourceManager.GetString("InvalidAccountResourceType", resourceCulture);

		internal static string InvalidAccountSASServcie => ResourceManager.GetString("InvalidAccountSASServcie", resourceCulture);

		internal static string InvalidCIDRNotation => ResourceManager.GetString("InvalidCIDRNotation", resourceCulture);

		internal static string InvalidDottedQuadNotation => ResourceManager.GetString("InvalidDottedQuadNotation", resourceCulture);

		internal static string InvalidDuplicateExtraPermission => ResourceManager.GetString("InvalidDuplicateExtraPermission", resourceCulture);

		internal static string InvalidExtraPermission => ResourceManager.GetString("InvalidExtraPermission", resourceCulture);

		internal static string InvalidIP => ResourceManager.GetString("InvalidIP", resourceCulture);

		internal static string InvalidIPRange => ResourceManager.GetString("InvalidIPRange", resourceCulture);

		internal static string InvalidIPV4Format => ResourceManager.GetString("InvalidIPV4Format", resourceCulture);

		internal static string InvalidNetworkSuffix => ResourceManager.GetString("InvalidNetworkSuffix", resourceCulture);

		internal static string InvalidSASPermissionForTable => ResourceManager.GetString("InvalidSASPermissionForTable", resourceCulture);

		internal static string InvalidSASPermissionOrder => ResourceManager.GetString("InvalidSASPermissionOrder", resourceCulture);

		internal static string InvalidSignatureFields => ResourceManager.GetString("InvalidSignatureFields", resourceCulture);

		internal static string InvalidSignatureSize => ResourceManager.GetString("InvalidSignatureSize", resourceCulture);

		internal static string InvalidSignedPermission => ResourceManager.GetString("InvalidSignedPermission", resourceCulture);

		internal static string InvalidSignedProtocol => ResourceManager.GetString("InvalidSignedProtocol", resourceCulture);

		internal static string InvalidSignedProtocolHttpOnly => ResourceManager.GetString("InvalidSignedProtocolHttpOnly", resourceCulture);

		internal static string InvalidSignedTimeRange => ResourceManager.GetString("InvalidSignedTimeRange", resourceCulture);

		internal static string InvalidSIPFormat => ResourceManager.GetString("InvalidSIPFormat", resourceCulture);

		internal static string InvalidTableName => ResourceManager.GetString("InvalidTableName", resourceCulture);

		internal static string IPAuthorizationFailed => ResourceManager.GetString("IPAuthorizationFailed", resourceCulture);

		internal static string IPFromDifferentFamilies => ResourceManager.GetString("IPFromDifferentFamilies", resourceCulture);

		internal static string IPIsEmpty => ResourceManager.GetString("IPIsEmpty", resourceCulture);

		internal static string MalformedBatchRequest => ResourceManager.GetString("MalformedBatchRequest", resourceCulture);

		internal static string MandatoryFieldEmpty => ResourceManager.GetString("MandatoryFieldEmpty", resourceCulture);

		internal static string MediaTypeNotSupported => ResourceManager.GetString("MediaTypeNotSupported", resourceCulture);

		internal static string MissingDependentParam => ResourceManager.GetString("MissingDependentParam", resourceCulture);

		internal static string MissingOrInvalidHeader => ResourceManager.GetString("MissingOrInvalidHeader", resourceCulture);

		internal static string OperationTimedOut => ResourceManager.GetString("OperationTimedOut", resourceCulture);

		internal static string OptionalFieldCannotBeEmptyIfSpecified => ResourceManager.GetString("OptionalFieldCannotBeEmptyIfSpecified", resourceCulture);

		internal static string OutOfRangeInput => ResourceManager.GetString("OutOfRangeInput", resourceCulture);

		internal static string OutOfRangeInputGeneric => ResourceManager.GetString("OutOfRangeInputGeneric", resourceCulture);

		internal static string PartitionKeyOrRowKeyEmpty => ResourceManager.GetString("PartitionKeyOrRowKeyEmpty", resourceCulture);

		internal static string PartitionKeyTooLarge => ResourceManager.GetString("PartitionKeyTooLarge", resourceCulture);

		internal static string PropertyNameCurrentlyNotSupported => ResourceManager.GetString("PropertyNameCurrentlyNotSupported", resourceCulture);

		internal static string PropertyNameInvalid => ResourceManager.GetString("PropertyNameInvalid", resourceCulture);

		internal static string RowKeyTooLarge => ResourceManager.GetString("RowKeyTooLarge", resourceCulture);

		internal static string SASNotSupportedForResourceType => ResourceManager.GetString("SASNotSupportedForResourceType", resourceCulture);

		internal static string SignedTimeRangeNotApplicable => ResourceManager.GetString("SignedTimeRangeNotApplicable", resourceCulture);

		internal static string SipSupportSingleIPRange => ResourceManager.GetString("SipSupportSingleIPRange", resourceCulture);

		internal static string TableNameEmpty => ResourceManager.GetString("TableNameEmpty", resourceCulture);

		internal static string TableQueryFluentMethodNotAllowed => ResourceManager.GetString("TableQueryFluentMethodNotAllowed", resourceCulture);

		internal static string UnimplementedOperation => ResourceManager.GetString("UnimplementedOperation", resourceCulture);

		internal static string UnsupportedSASWithPkAndRK => ResourceManager.GetString("UnsupportedSASWithPkAndRK", resourceCulture);

		internal static string VersionDontSupportSAS => ResourceManager.GetString("VersionDontSupportSAS", resourceCulture);

		internal TableResources()
		{
		}
	}
}
