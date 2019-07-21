using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal abstract class ResponseParsingBase<T> : IDisposable
	{
		protected IList<T> outstandingObjectsToParse = new List<T>();

		protected bool allObjectsParsed;

		protected XmlReader reader;

		private IEnumerator<T> parser;

		private bool enumerableConsumed;

		protected IEnumerable<T> ObjectsToParse
		{
			get
			{
				if (enumerableConsumed)
				{
					throw new InvalidOperationException("Resource consumed");
				}
				enumerableConsumed = true;
				while (!allObjectsParsed && parser.MoveNext())
				{
					if (parser.Current != null)
					{
						yield return parser.Current;
					}
				}
				foreach (T item in outstandingObjectsToParse)
				{
					yield return item;
				}
				outstandingObjectsToParse = null;
			}
		}

		protected ResponseParsingBase(Stream stream)
		{
			reader = XmlReader.Create(stream, new XmlReaderSettings
			{
				IgnoreWhitespace = false
			});
			parser = ParseXmlAndClose().GetEnumerator();
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected abstract IEnumerable<T> ParseXml();

		protected virtual void Dispose(bool disposing)
		{
			if (disposing && reader != null)
			{
				reader.Close();
			}
			reader = null;
		}

		protected void Variable(ref bool consumable)
		{
			if (consumable)
			{
				return;
			}
			while (parser.MoveNext())
			{
				if (parser.Current != null)
				{
					outstandingObjectsToParse.Add(parser.Current);
				}
				if (consumable)
				{
					break;
				}
			}
		}

		private IEnumerable<T> ParseXmlAndClose()
		{
			foreach (T item in ParseXml())
			{
				yield return item;
			}
			reader.Close();
			reader = null;
		}
	}
}
