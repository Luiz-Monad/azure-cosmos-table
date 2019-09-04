namespace Microsoft.Azure.Cosmos.Table
{
	internal class SR
	{
		public const string ArgumentEmptyError = "The argument must not be empty string.";

		public const string ArgumentOutOfRangeError = "The argument is out of range. Value passed: {0}";

		public const string ArgumentTooLargeError = "The argument '{0}' is larger than maximum of '{1}'";

		public const string ArgumentTooSmallError = "The argument '{0}' is smaller than minimum of '{1}'";

		public const string AttemptedEdmTypeForTheProperty = "Attempting to deserialize '{0}' as type '{1}'";

		public const string BatchWithRetreiveContainsOtherOperations = "A batch transaction with a retrieve operation cannot contain any other operations.";

		public const string BatchExceededMaximumNumberOfOperations = "The maximum number of operations allowed in one batch has been exceeded.";

		public const string BatchOperationRequiresPartitionKeyRowKey = "A batch non-retrieve operation requires a non-null partition key and row key.";

		public const string BatchErrorInOperation = "Element {0} in the batch returned an unexpected response code.";

		public const string BlobTypeMismatch = "Blob type of the blob reference doesn't match blob type of the blob.";

		public const string BufferManagerProvidedIncorrectLengthBuffer = "The IBufferManager provided an incorrect length buffer to the stream, Expected {0}, received {1}. Buffer length should equal the value returned by IBufferManager.GetDefaultBufferSize().";

		public const string CannotCreateSASSignatureForGivenCred = "Cannot create Shared Access Signature as the credentials does not have account name information. Please check that the credentials used support creating Shared Access Signature.";

		public const string CannotCreateSASWithoutAccountKey = "Cannot create Shared Access Signature unless Account Key credentials are used.";

		public const string Container = "container";

		public const string DelegatingHandlerNonNullInnerHandler = "Innermost DelegatingHandler must have a null InnerHandler.";

		public const string EmptyBatchOperation = "Cannot execute an empty batch operation";

		public const string ETagMissingForDelete = "Delete requires an ETag (which may be the '*' wildcard).";

		public const string ETagMissingForMerge = "Merge requires an ETag (which may be the '*' wildcard).";

		public const string ETagMissingForReplace = "Replace requires an ETag (which may be the '*' wildcard).";

		public const string ExtendedErrorUnavailable = "An unknown error has occurred, extended error information not available.";

		public const string File = "file";

		public const string FailParseProperty = "Failed to parse property '{0}' with value '{1}' as type '{2}'";

		public const string GetServiceStatsInvalidOperation = "GetServiceStats cannot be run with a 'PrimaryOnly' location mode.";

		public const string InternalStorageError = "Unexpected internal storage client error.";

		public const string InvalidCorsRule = "A CORS rule must contain at least one allowed origin and allowed method, and MaxAgeInSeconds cannot have a value less than zero.";

		public const string InvalidLoggingLevel = "Invalid logging operations specified.";

		public const string InvalidMetricsLevel = "Invalid metrics level specified.";

		public const string InvalidDeleteRetentionDaysValue = "The delete retention policy is enabled but the RetentionDays property is not specified or has an invalid value. RetentionDays must be greater than 0 and less than or equal to 365 days.";

		public const string InvalidProtocolsInSAS = "Invalid value {0} for the SharedAccessProtocol parameter when creating a SharedAccessSignature.  Use 'null' if you do not wish to include a SharedAccessProtocol.";

		public const string InvalidTypeInJsonDictionary = "Invalid type in JSON object. Detected type is {0}, which is not a valid JSON type.";

		public const string IQueryableExtensionObjectMustBeTableQuery = "Query must be a TableQuery<T>";

		public const string JsonReaderNotInCompletedState = "The JSON reader has not yet reached the completed state.";

		public const string LoggingVersionNull = "The logging version is null or empty.";

		public const string MetricVersionNull = "The metrics version is null or empty.";

		public const string MissingAccountInformationInUri = "Cannot find account information inside Uri '{0}'";

		public const string MissingCredentials = "No credentials provided.";

		public const string StorageUriMustMatch = "Primary and secondary location URIs in a StorageUri must point to the same resource.";

		public const string NegativeBytesRequestedInCopy = "Internal Error - negative copyLength requested when attempting to copy a stream.  CopyLength = {0}, totalBytes = {1}.";

		public const string NoPropertyResolverAvailable = "No property resolver available. Deserializing the entity properties as strings.";

		public const string OperationCanceled = "Operation was canceled by user.";

		public const string ParseError = "Error parsing value";

		public const string PartitionKey = "All entities in a given batch must have the same partition key.";

		public const string PayloadFormat = "Setting payload format for the request to '{0}'.";

		public const string PropertyDelimiterExistsInPropertyName = "Property delimiter: {0} exists in property name: {1}. Object Path: {2}";

		public const string PropertyResolverCacheDisabled = "Property resolver cache is disabled.";

		public const string PropertyResolverThrewError = "The custom property resolver delegate threw an exception. Check the inner exception for more details.";

		public const string RecursiveReferencedObject = "Recursive reference detected. Object Path: {0} Property Type: {1}.";

		public const string RelativeAddressNotPermitted = "Address '{0}' is a relative address. Only absolute addresses are permitted.";

		public const string RetrieveWithContinuationToken = "Retrieved '{0}' results with continuation token '{1}'.";

		public const string SetServicePropertiesRequiresNonNullSettings = "At least one service property needs to be non-null for SetServiceProperties API.";

		public const string Share = "share";

		public const string StreamLengthError = "The length of the stream exceeds the permitted length.";

		public const string StreamLengthMismatch = "Cannot specify both copyLength and maxLength.";

		public const string StreamLengthShortError = "The requested number of bytes exceeds the length of the stream remaining from the specified position.";

		public const string Table = "table";

		public const string TableEndPointNotConfigured = "No table endpoint configured.";

		public const string TableQueryDynamicPropertyAccess = "Accessing property dictionary of DynamicTableEntity requires a string constant for property name.";

		public const string TableQueryEntityPropertyInQueryNotSupported = "Referencing {0} on EntityProperty only supported with properties dictionary exposed via DynamicTableEntity.";

		public const string TableQueryMustHaveQueryProvider = "Unknown Table. The TableQuery does not have an associated CloudTable Reference. Please execute the query via the CloudTable ExecuteQuery APIs.";

		public const string TableQueryTypeMustImplementITableEnitty = "TableQuery Generic Type must implement the ITableEntity Interface";

		public const string TableQueryTypeMustHaveDefaultParameterlessCtor = "TableQuery Generic Type must provide a default parameterless constructor.";

		public const string TakeCountNotPositive = "Take count must be positive and greater than 0.";

		public const string TimeoutExceptionMessage = "The client could not finish the operation within specified timeout.";

		public const string TraceDownloadError = "Downloading error response body.";

		public const string TraceRetryInfo = "The extended retry policy set the next location to {0} and updated the location mode to {1}.";

		public const string TraceGenericError = "Exception thrown during the operation: {0}.";

		public const string TraceGetResponse = "Waiting for response.";

		public const string TraceIgnoreAttribute = "Omitting property '{0}' from serialization/de-serialization because IgnoreAttribute has been set on that property.";

		public const string TraceInitLocation = "Starting operation with location {0} per location mode {1}.";

		public const string TraceInitRequestError = "Exception thrown while initializing request: {0}.";

		public const string TraceMissingDictionaryEntry = "Omitting property '{0}' from de-serialization because there is no corresponding entry in the dictionary provided.";

		public const string TraceNextLocation = "The next location has been set to {0}, based on the location mode.";

		public const string TraceNonPublicGetSet = "Omitting property '{0}' from serialization/de-serialization because the property's getter/setter are not public.";

		public const string TraceNonExistingGetter = "Omitting property: {0} from serialization/de-serialization because the property does not have a getter. Object path: {1}";

		public const string TraceNonExistingSetter = "Omitting property: {0} from serialization/de-serialization because the property does not have a setter. The property needs to have at least a private setter. Object Path: {1}";

		public const string TracePreProcessDone = "Response headers were processed successfully, proceeding with the rest of the operation.";

		public const string TraceResponse = "Response received. Status code = {0}, Request ID = {1}, Content-MD5 = {2}, ETag = {3}.";

		public const string TraceRetry = "Retrying failed operation.";

		public const string TraceRetryCheck = "Checking if the operation should be retried. Retry count = {0}, HTTP status code = {1}, Retryable exception = {2}, Exception = {3}.";

		public const string TraceRetryDecisionPolicy = "Retry policy did not allow for a retry. Failing with {0}.";

		public const string TraceRetryDecisionTimeout = "Operation cannot be retried because the maximum execution time has been reached. Failing with {0}.";

		public const string TraceRetryDelay = "Operation will be retried after {0}ms.";

		public const string TraceSetPropertyError = "Exception thrown while trying to set property value. Property Path: {0} Property Value: {1}. Exception Message: {2}";

		public const string TraceStartRequestAsync = "Starting asynchronous request to {0}.";

		public const string TraceStringToSign = "StringToSign = {0}.";

		public const string UnexpectedEDMType = "Unexpected EDM type from the Table Service: {0}.";

		public const string UnexpectedParameterInSAS = "The parameter `api-version` should not be included in the SAS token. Please allow the library to set the  `api-version` parameter.";

		public const string UnexpectedResponseCode = "Unexpected response code, Expected:{0}, Received:{1}";

		public const string UnsupportedPropertyTypeForEntityPropertyConversion = "Unsupported type : {0} encountered during conversion to EntityProperty. Object Path: {1}";

		public const string UsingDefaultPropertyResolver = "Using the default property resolver to deserialize the entity.";

		public const string UsingUserProvidedPropertyResolver = "Using the property resolver provided via TableRequestOptions to deserialize the entity.";

		public const string ALinqCouldNotConvert = "Could not convert constant {0} expression to string.";

		public const string ALinqMethodNotSupported = "The method '{0}' is not supported.";

		public const string ALinqUnaryNotSupported = "The unary operator '{0}' is not supported.";

		public const string ALinqBinaryNotSupported = "The binary operator '{0}' is not supported.";

		public const string ALinqConstantNotSupported = "The constant for '{0}' is not supported.";

		public const string ALinqTypeBinaryNotSupported = "An operation between an expression and a type is not supported.";

		public const string ALinqConditionalNotSupported = "The conditional expression is not supported.";

		public const string ALinqParameterNotSupported = "The parameter expression is not supported.";

		public const string ALinqMemberAccessNotSupported = "The member access of '{0}' is not supported.";

		public const string ALinqLambdaNotSupported = "Lambda Expressions not supported.";

		public const string ALinqNewNotSupported = "New Expressions not supported.";

		public const string ALinqMemberInitNotSupported = "Member Init Expressions not supported.";

		public const string ALinqListInitNotSupported = "List Init Expressions not supported.";

		public const string ALinqNewArrayNotSupported = "New Array Expressions not supported.";

		public const string ALinqInvocationNotSupported = "Invocation Expressions not supported.";

		public const string ALinqUnsupportedExpression = "The expression type {0} is not supported.";

		public const string ALinqCanOnlyProjectTheLeaf = "Can only project the last entity type in the query being translated.";

		public const string ALinqCantCastToUnsupportedPrimitive = "Can't cast to unsupported type '{0}'";

		public const string ALinqCantTranslateExpression = "The expression {0} is not supported.";

		public const string ALinqCantNavigateWithoutKeyPredicate = "Navigation properties can only be selected from a single resource. Specify a key predicate to restrict the entity set to a single instance.";

		public const string ALinqCantReferToPublicField = "Referencing public field '{0}' not supported in query option expression.  Use public property instead.";

		public const string ALinqCannotConstructKnownEntityTypes = "Construction of entity type instances must use object initializer with default constructor.";

		public const string ALinqCannotCreateConstantEntity = "Referencing of local entity type instances not supported when projecting results.";

		public const string ALinqExpressionNotSupportedInProjectionToEntity = "Initializing instances of the entity type {0} with the expression {1} is not supported.";

		public const string ALinqExpressionNotSupportedInProjection = "Constructing or initializing instances of the type {0} with the expression {1} is not supported.";

		public const string ALinqProjectionMemberAssignmentMismatch = "Cannot initialize an instance of entity type '{0}' because '{1}' and '{2}' do not refer to the same source entity.";

		public const string ALinqPropertyNamesMustMatchInProjections = "Cannot assign the value from the {0} property to the {1} property.  When projecting results into a entity type, the property names of the source type and the target type must match for the properties being projected.";

		public const string ALinqQueryOptionOutOfOrder = "The {0} query option cannot be specified after the {1} query option.";

		public const string ALinqQueryOptionsOnlyAllowedOnLeafNodes = "Can only specify query options (orderby, where, take, skip) after last navigation.";
	}
}
