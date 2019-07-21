using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Utils
{
	internal static class XMLReaderExtensions
	{
		public static XmlReader CreateAsAsync(Stream stream)
		{
			return XmlReader.Create(stream, new XmlReaderSettings
			{
				IgnoreWhitespace = true,
				Async = true
			});
		}

		public static async Task ReadStartElementAsync(this XmlReader reader, string localname, string ns)
		{
			if (await reader.MoveToContentAsync().ConfigureAwait(continueOnCapturedContext: false) != XmlNodeType.Element)
			{
				throw new InvalidOperationException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
			}
			if (reader.LocalName == localname && reader.NamespaceURI == ns)
			{
				await reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			throw new InvalidOperationException("localName or namespace doesn’t match");
		}

		public static async Task ReadStartElementAsync(this XmlReader reader)
		{
			if (await reader.MoveToContentAsync().ConfigureAwait(continueOnCapturedContext: false) != XmlNodeType.Element)
			{
				throw new InvalidOperationException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
			}
			await reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		}

		public static async Task ReadEndElementAsync(this XmlReader reader)
		{
			if (await reader.MoveToContentAsync().ConfigureAwait(continueOnCapturedContext: false) != XmlNodeType.EndElement)
			{
				throw new InvalidOperationException();
			}
			await reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		}

		public static async Task<bool> IsStartElementAsync(this XmlReader reader)
		{
			return await reader.MoveToContentAsync().ConfigureAwait(continueOnCapturedContext: false) == XmlNodeType.Element;
		}

		public static async Task<bool> IsStartElementAsync(this XmlReader reader, string name)
		{
			return await reader.MoveToContentAsync().ConfigureAwait(continueOnCapturedContext: false) == XmlNodeType.Element && reader.Name == name;
		}

		public static async Task<string> ReadElementContentAsStringAsync(this XmlReader reader, string localName, string namespaceURI)
		{
			reader.CheckElement(localName, namespaceURI);
			return await reader.ReadElementContentAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
		}

		private static void CheckElement(this XmlReader reader, string localName, string namespaceURI)
		{
			if (localName == null || localName.Length == 0)
			{
				throw new InvalidOperationException("localName is null or empty");
			}
			if (namespaceURI == null)
			{
				throw new ArgumentNullException("namespaceURI");
			}
			if (reader.NodeType != XmlNodeType.Element)
			{
				throw new InvalidOperationException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
			}
			if (reader.LocalName != localName || reader.NamespaceURI != namespaceURI)
			{
				throw new InvalidOperationException("localName or namespace doesn’t match");
			}
		}
	}
}
