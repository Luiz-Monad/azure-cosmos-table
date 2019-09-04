using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Microsoft.Azure.Cosmos.Table.Extensions
{
	internal static class OperationContextExtensions
	{
		internal static RequestResult ToRequestResult<T>(this ResourceResponse<T> response) where T : Resource, new()
		{
			return new RequestResult
			{
				HttpStatusCode = 202,
				HttpStatusMessage = null,
				ServiceRequestID = response.ActivityId,
				Etag = null,
				RequestDate = null,
				RequestCharge = response.RequestCharge
			};
		}

		internal static RequestResult ToRequestResult<T>(this FeedResponse<T> response)
		{
			return new RequestResult
			{
				HttpStatusCode = 202,
				HttpStatusMessage = null,
				ServiceRequestID = response.ActivityId,
				Etag = null,
				RequestDate = null,
				RequestCharge = response.RequestCharge
			};
		}

		internal static RequestResult ToRequestResult(this StorageException storageException, string serviceRequestId)
		{
			return new RequestResult
			{
				Exception = storageException,
				ExtendedErrorInformation = storageException.RequestInformation.ExtendedErrorInformation,
				HttpStatusCode = storageException.RequestInformation.HttpStatusCode,
				HttpStatusMessage = storageException.RequestInformation.HttpStatusMessage,
				ServiceRequestID = serviceRequestId,
				RequestCharge = storageException.RequestInformation.RequestCharge
			};
		}
	}
}
