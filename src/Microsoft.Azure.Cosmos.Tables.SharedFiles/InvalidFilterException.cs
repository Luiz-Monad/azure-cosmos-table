using System;

namespace Microsoft.Azure.Cosmos.Tables.SharedFiles
{
	internal class InvalidFilterException : Exception
	{
		private string filterParam;

		public override string Message => $"{base.Message} FilterParam={filterParam ?? string.Empty}";

		public InvalidFilterException()
		{
		}

		public InvalidFilterException(string message)
			: base(message)
		{
		}

		public InvalidFilterException(string message, string filterParam)
			: base(message)
		{
			this.filterParam = filterParam;
		}

		public InvalidFilterException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
