using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Protocol;
using Microsoft.Azure.Documents;
using System;
using System.Globalization;
using System.Net;

namespace Microsoft.Azure.Cosmos.Tables.SharedFiles
{
	internal static class DocumentErrorTranslator
	{
		private const string OperationCountPrefix = "OperationCount:";

		public static TableErrorResult TranslateDocumentErrorForTables(this Exception exception, TableOperationWrapper tableOperation = null)
		{
			DocumentClientException ex = exception as DocumentClientException;
			TableErrorResult tableErrorResult = null;
			if (ex == null)
			{
				return HandleNonDocumentClientExceptions(exception);
			}
			tableErrorResult = new TableErrorResult();
			tableErrorResult.HttpStatusCode = (int)ex.StatusCode.Value;
			tableErrorResult.HttpStatusMessage = (string.IsNullOrWhiteSpace(ex.Message) ? 
				GetStatusDescription(tableErrorResult.HttpStatusCode) : ex.Message);
			tableErrorResult.ServiceRequestID = ex.ActivityId;
			tableErrorResult.RequestCharge = ex.RequestCharge;
			tableErrorResult.ExtendedErrorCode = ex.Error.Code;
			tableErrorResult.ExtendedErroMessage = ex.Error.Message;
			return FillTableErrorResult(tableErrorResult, tableOperation);
		}

		private static TableErrorResult HandleNonDocumentClientExceptions(Exception exception)
		{
			if (exception is NotSupportedException || exception is TimeoutException)
			{
				throw exception;
			}
			TableErrorResult tableErrorResult = new TableErrorResult();
			if (exception is InvalidEtagException)
			{
				tableErrorResult.HttpStatusCode = 400;
				tableErrorResult.HttpStatusMessage = /*RMResources.BadRequest*/ HttpStatusCode.BadRequest.ToString();
				tableErrorResult.ExtendedErrorCode = HttpStatusCode.BadRequest.ToString();
				tableErrorResult.ExtendedErroMessage = exception.Message;
				return tableErrorResult;
			}
			if (exception is InvalidFilterException)
			{
				tableErrorResult.HttpStatusCode = 400;
				tableErrorResult.HttpStatusMessage = /*RMResources.BadRequest*/ HttpStatusCode.BadRequest.ToString();
				tableErrorResult.ExtendedErrorCode = TableErrorCodeStrings.InvalidInput;
				tableErrorResult.ExtendedErroMessage = /*RMResources.BadUrl*/ "Request url is invalid.";
				return tableErrorResult;
			}
			tableErrorResult.HttpStatusCode = 500;
			tableErrorResult.HttpStatusMessage = "Server encountered an internal error.Please try again after some time.";
			tableErrorResult.ExtendedErrorCode = HttpStatusCode.InternalServerError.ToString();
			tableErrorResult.ExtendedErroMessage = exception.Message;
			return tableErrorResult;
		}

		public static TableErrorResult TranslateDocumentErrorForStoredProcs(this Exception exception, TableOperationWrapper tableOperation = null, int batchOperationCount = 0)
		{
			DocumentClientException ex = exception as DocumentClientException;
			if (ex == null)
			{
				return HandleNonDocumentClientExceptions(exception);
			}
			TableErrorResult tableErrorResult = new TableErrorResult();
			tableErrorResult.RequestCharge = ex.RequestCharge;
			tableErrorResult.ServiceRequestID = ex.ActivityId;
			if (ex.StatusCode == HttpStatusCode.BadRequest)
			{
				bool flag = true;
				if (ex.Message.Contains("Resource Not Found"))
				{
					tableErrorResult.HttpStatusCode = 404;
				}
				else if (ex.Message.Contains("One of the specified"))
				{
					tableErrorResult.HttpStatusCode = 412;
				}
				else if (ex.Message.Contains("Resource with specified id or name already exists"))
				{
					tableErrorResult.HttpStatusCode = 409;
				}
				else if (ex.Message.Contains("Failed to enqueue operation"))
				{
					tableErrorResult.HttpStatusCode = 429;
					tableErrorResult.HttpStatusMessage = "Too Many Requests";
					tableErrorResult.ExtendedErrorCode = "TooManyRequests";
					tableErrorResult.ExtendedErroMessage = /*RMResources.TooManyRequests*/ "Too Many Requests";
				}
				else
				{
					flag = false;
				}
				tableErrorResult = ((!flag) ? exception.TranslateDocumentErrorForTables(tableOperation) : FillTableErrorResult(tableErrorResult, tableOperation));
			}
			else if (ex.StatusCode == HttpStatusCode.RequestEntityTooLarge)
			{
				tableErrorResult.HttpStatusCode = 413;
				tableErrorResult.ExtendedErrorCode = StorageErrorCodeStrings.RequestBodyTooLarge;
				tableErrorResult.ExtendedErroMessage = "The request body is too large and exceeds the maximum permissible limit.";
			}
			else
			{
				tableErrorResult = exception.TranslateDocumentErrorForTables(tableOperation);
			}
			tableErrorResult.HttpStatusMessage = GetStatusDescription(tableErrorResult.HttpStatusCode);
			int num = -1;
			if (batchOperationCount > 0)
			{
				num = ex.Message.IndexOf("OperationCount:", StringComparison.Ordinal);
			}
			if (num >= 0)
			{
				num += "OperationCount:".Length;
				int num2 = ex.Message.IndexOf('.', num);
				int result = -1;
				int.TryParse(ex.Message.Substring(num, num2 - num), out result);
				if (result >= 0)
				{
					tableErrorResult.ExtendedErroMessage = string.Format(CultureInfo.InvariantCulture, "{0}:{1}\n", result, tableErrorResult.ExtendedErroMessage);
				}
			}
			return tableErrorResult;
		}

		public static TableErrorResult FillTableErrorResult(TableErrorResult reqResult, TableOperationWrapper tableOperation = null)
		{
			switch (reqResult.HttpStatusCode)
			{
			case 409:
				if (tableOperation != null && tableOperation.IsTableEntity)
				{
					reqResult.ExtendedErrorCode = TableErrorCodeStrings.TableAlreadyExists;
					reqResult.ExtendedErroMessage = "The specified table already exists.";
				}
				else
				{
					reqResult.ExtendedErrorCode = TableErrorCodeStrings.EntityAlreadyExists;
					reqResult.ExtendedErroMessage = "The specified entity already exists.";
				}
				break;
			case 404:
				if (tableOperation != null && tableOperation.IsTableEntity)
				{
					if (tableOperation.OperationType != TableOperationType.Delete)
					{
						reqResult.ExtendedErrorCode = TableErrorCodeStrings.TableNotFound;
						reqResult.ExtendedErroMessage = "The specified table was not found.";
					}
					else
					{
						reqResult.ExtendedErrorCode = "ResourceNotFound";
						reqResult.ExtendedErroMessage = "The specified resource does not exist.";
					}
				}
				else
				{
					reqResult.ExtendedErrorCode = "ResourceNotFound";
					reqResult.ExtendedErroMessage = "The specified resource does not exist.";
				}
				break;
			case 412:
				reqResult.ExtendedErrorCode = TableErrorCodeStrings.UpdateConditionNotSatisfied;
				reqResult.ExtendedErroMessage = "The update condition specified in the request was not satisfied.";
				break;
			case 413:
				reqResult.ExtendedErrorCode = TableErrorCodeStrings.EntityTooLarge;
				reqResult.ExtendedErroMessage = "The entity is larger than the maximum size permitted.";
				reqResult.HttpStatusCode = 400;
				break;
			}
			return reqResult;
		}

		private static string GetStatusDescription(int code)
		{
			HttpStatusCode httpStatusCode = (HttpStatusCode)code;
			string text = httpStatusCode.ToString();
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			if (code == 429)
			{
				return "Too Many Requests";
			}
			return "Internal Server Error";
		}
	}
}
