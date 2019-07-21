using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common
{
	internal abstract class AccessPolicyResponseBase<T> : ResponseParsingBase<KeyValuePair<string, T>> where T : new()
	{
		public IEnumerable<KeyValuePair<string, T>> AccessIdentifiers => base.ObjectsToParse;

		protected AccessPolicyResponseBase(Stream stream)
			: base(stream)
		{
		}

		protected abstract T ParseElement(XElement accessPolicyElement);

		protected override IEnumerable<KeyValuePair<string, T>> ParseXml()
		{
			IEnumerable<XElement> enumerable = XElement.Load(reader).Elements("SignedIdentifier");
			foreach (XElement item in enumerable)
			{
				string key = (string)item.Element("Id");
				XElement xElement = item.Element("AccessPolicy");
				T value = (xElement == null) ? new T() : ParseElement(xElement);
				yield return new KeyValuePair<string, T>(key, value);
			}
		}
	}
}
