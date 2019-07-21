using System;

namespace Microsoft.Azure.Cosmos.Tables.SharedFiles
{
	internal class InvalidEtagException : Exception
	{
		private string etag;

		public override string Message => base.Message + " Etag=" + (etag ?? string.Empty);

		public InvalidEtagException()
		{
		}

		public InvalidEtagException(string message)
			: base(message)
		{
		}

		public InvalidEtagException(string message, string etag)
			: base(message)
		{
			this.etag = etag;
		}
	}
}
