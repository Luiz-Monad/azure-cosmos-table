using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Internals;
using System;
using System.Net;

namespace Microsoft.Azure.Cosmos.Table.Extensions.Shared
{
	internal static class ErrorTranslator
	{
		private const string ResourceNotFound = "ResourceNotFound";

		private const string ResourceNotFoundMessage = "The specified resource does not exist.";

		private const string ConditionNotMet = "ConditionNotMet";

		private const string ConditionNotMetMessage = "The update condition specified in the request was not satisfied.";

		private const string EntityAlreadyExists = "EntityAlreadyExists";

		private const string EntityAlreadyExistsMessage = "The specified entity already exists.";

		private const string RequestBodyTooLarge = "RequestBodyTooLarge";

		private const string RequestBodyTooLargeMessage = "The request body is too large and exceeds the maximum permissible limit.";

		internal static Exception TranslateStoredProcedureException(Exception ex)
		{
			DocumentClientException ex2 = ex as DocumentClientException;
			if (ex2.StatusCode == HttpStatusCode.BadRequest)
			{
				if (ex2.Message.Contains("Resource Not Found"))
				{
					return new DocumentClientExceptionInternal("The specified resource does not exist.", ex2, HttpStatusCode.NotFound, null, "ResourceNotFound");
				}
				if (ex2.Message.Contains("One of the specified"))
				{
					return new DocumentClientExceptionInternal("The update condition specified in the request was not satisfied.", ex2, HttpStatusCode.PreconditionFailed, null, "ConditionNotMet");
				}
				if (ex2.Message.Contains("Resource with specified id or name already exists"))
				{
					return new DocumentClientExceptionInternal("The specified entity already exists.", ex2, HttpStatusCode.Conflict, null, "EntityAlreadyExists");
				}
			}
			else if (ex2.StatusCode == HttpStatusCode.RequestEntityTooLarge)
			{
				new DocumentClientExceptionInternal("The request body is too large and exceeds the maximum permissible limit.", ex2, HttpStatusCode.RequestEntityTooLarge, null, "RequestBodyTooLarge");
			}
			return ex;
		}
	}
}
