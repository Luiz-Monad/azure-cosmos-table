using Microsoft.Azure.Cosmos.Tables.SharedFiles;
using System;

namespace Microsoft.Azure.Cosmos.Table.Extensions
{
	internal static class DocumentHelpers
	{
		public static StorageException ToStorageException(this Exception exception, TableOperation tableOperation)
		{
			if (exception is StorageException)
			{
				return (StorageException)exception;
			}
			RequestResult requestResult = null;
			if (exception is TableException)
			{
				TableException ex = exception as TableException;
				requestResult = TableExtensionOperationHelper.GenerateRequestResult(ex.HttpStatusMessage, (int)ex.HttpStatusCode, ex.ErrorCode, ex.ErrorMessage, null, null);
			}
			else
			{
				TableOperationWrapper tableOperation2 = null;
				if (tableOperation != null)
				{
					tableOperation2 = new TableOperationWrapper
					{
						IsTableEntity = tableOperation.IsTableEntity,
						OperationType = tableOperation.OperationType
					};
				}
				TableErrorResult tableErrorResult = exception.TranslateDocumentErrorForStoredProcs(tableOperation2);
				requestResult = TableExtensionOperationHelper.GenerateRequestResult(tableErrorResult.HttpStatusMessage, tableErrorResult.HttpStatusCode, tableErrorResult.ExtendedErrorCode, tableErrorResult.ExtendedErroMessage, tableErrorResult.ServiceRequestID, tableErrorResult.RequestCharge);
			}
			return new StorageException(requestResult, requestResult.ExtendedErrorInformation.ErrorMessage, exception);
		}
	}
}
