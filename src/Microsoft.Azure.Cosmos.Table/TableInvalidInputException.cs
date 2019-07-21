using System.Net;

namespace Microsoft.Azure.Cosmos.Table
{
	internal sealed class TableInvalidInputException : TableException
	{
		internal TableInvalidInputException(string errorCode, string errorMessage)
			: base(HttpStatusCode.BadRequest, "Bad Request", errorCode, errorMessage)
		{
		}
	}
}
