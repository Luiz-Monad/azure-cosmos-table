using System.Text;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth
{
	internal class CanonicalizedString
	{
		private const int DefaultCapacity = 300;

		private const char ElementDelimiter = '\n';

		private readonly StringBuilder canonicalizedString;

		public CanonicalizedString(string initialElement)
			: this(initialElement, 300)
		{
		}

		public CanonicalizedString(string initialElement, int capacity)
		{
			canonicalizedString = new StringBuilder(initialElement, capacity);
		}

		public void AppendCanonicalizedElement(string element)
		{
			canonicalizedString.Append('\n');
			canonicalizedString.Append(element);
		}

		public override string ToString()
		{
			return canonicalizedString.ToString();
		}
	}
}
