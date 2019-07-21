using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Azure.Cosmos.Table
{
	internal class UriQueryBuilder
	{
		protected IDictionary<string, string> Parameters
		{
			get;
			private set;
		}

		public string this[string name]
		{
			get
			{
				if (Parameters.TryGetValue(name, out string value))
				{
					return value;
				}
				throw new KeyNotFoundException(string.Format(CultureInfo.InvariantCulture, "'{0}' key not found in the query builder.", new object[1]
				{
					name
				}));
			}
		}

		public UriQueryBuilder()
			: this(null)
		{
		}

		public UriQueryBuilder(UriQueryBuilder builder)
		{
			object parameters;
			if (builder == null)
			{
				IDictionary<string, string> dictionary = new Dictionary<string, string>();
				parameters = dictionary;
			}
			else
			{
				IDictionary<string, string> dictionary = new Dictionary<string, string>(builder.Parameters);
				parameters = dictionary;
			}
			Parameters = (IDictionary<string, string>)parameters;
		}

		public virtual void Add(string name, string value)
		{
			if (value != null)
			{
				value = Uri.EscapeDataString(value);
			}
			Parameters.Add(name, value);
		}

		public void AddRange(IEnumerable<KeyValuePair<string, string>> parameters)
		{
			CommonUtility.AssertNotNull("parameters", parameters);
			foreach (KeyValuePair<string, string> parameter in parameters)
			{
				Add(parameter.Key, parameter.Value);
			}
		}

		public bool ContainsQueryStringName(string name)
		{
			return Parameters.ContainsKey(name);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = true;
			foreach (KeyValuePair<string, string> parameter in Parameters)
			{
				if (flag)
				{
					flag = false;
					stringBuilder.Append("?");
				}
				else
				{
					stringBuilder.Append("&");
				}
				stringBuilder.Append(parameter.Key);
				if (parameter.Value != null)
				{
					stringBuilder.AppendFormat("={0}", parameter.Value);
				}
			}
			return stringBuilder.ToString();
		}

		public StorageUri AddToUri(StorageUri storageUri)
		{
			CommonUtility.AssertNotNull("storageUri", storageUri);
			return new StorageUri(AddToUri(storageUri.PrimaryUri), AddToUri(storageUri.SecondaryUri));
		}

		public virtual Uri AddToUri(Uri uri)
		{
			return AddToUriCore(uri);
		}

		protected Uri AddToUriCore(Uri uri)
		{
			if (uri == null)
			{
				return null;
			}
			string text = ToString();
			if (text.Length > 1)
			{
				text = text.Substring(1);
			}
			UriBuilder uriBuilder = new UriBuilder(uri);
			uriBuilder.Query = ((uriBuilder.Query == null || uriBuilder.Query.Length <= 1) ? text : (uriBuilder.Query.Substring(1) + "&" + text));
			return uriBuilder.Uri;
		}
	}
}
