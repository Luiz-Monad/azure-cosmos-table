using Microsoft.Azure.Cosmos.Table.RestExecutor.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common
{
	internal static class StorageExtendedErrorInformationRestHelper
	{
		public static async Task<StorageExtendedErrorInformation> ReadFromStreamAsync(Stream inputStream, CancellationToken cancellationToken)
		{
			CommonUtility.AssertNotNull("inputStream", inputStream);
			if (inputStream.CanSeek && inputStream.Length < 1)
			{
				return null;
			}
			try
			{
				using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(inputStream))
				{
					await reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
					cancellationToken.ThrowIfCancellationRequested();
					StorageExtendedErrorInformation result = await ReadXmlAsync(reader, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					cancellationToken.ThrowIfCancellationRequested();
					return result;
				}
			}
			catch (XmlException)
			{
				return null;
			}
		}

		private static async Task<StorageExtendedErrorInformation> ReadXmlAsync(XmlReader reader, CancellationToken cancellationToken)
		{
			StorageExtendedErrorInformation extendedErrorInformation = new StorageExtendedErrorInformation();
			CommonUtility.AssertNotNull("reader", reader);
			extendedErrorInformation.AdditionalDetails = new Dictionary<string, string>();
			await reader.ReadStartElementAsync().ConfigureAwait(continueOnCapturedContext: false);
			cancellationToken.ThrowIfCancellationRequested();
			while (await reader.IsStartElementAsync().ConfigureAwait(continueOnCapturedContext: false))
			{
				if (reader.IsEmptyElement)
				{
					await reader.SkipAsync().ConfigureAwait(continueOnCapturedContext: false);
					cancellationToken.ThrowIfCancellationRequested();
				}
				else if (string.Compare(reader.LocalName, "Code", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(reader.LocalName, "code", StringComparison.Ordinal) == 0)
				{
					StorageExtendedErrorInformation storageExtendedErrorInformation = extendedErrorInformation;
					storageExtendedErrorInformation.ErrorCode = await reader.ReadElementContentAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
					cancellationToken.ThrowIfCancellationRequested();
				}
				else if (string.Compare(reader.LocalName, "Message", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(reader.LocalName, "message", StringComparison.Ordinal) == 0)
				{
					StorageExtendedErrorInformation storageExtendedErrorInformation = extendedErrorInformation;
					storageExtendedErrorInformation.ErrorMessage = await reader.ReadElementContentAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
					cancellationToken.ThrowIfCancellationRequested();
				}
				else if (string.Compare(reader.LocalName, "exceptiondetails", StringComparison.OrdinalIgnoreCase) == 0)
				{
					await reader.ReadStartElementAsync(null, null).ConfigureAwait(continueOnCapturedContext: false);
					cancellationToken.ThrowIfCancellationRequested();
					while (await reader.IsStartElementAsync().ConfigureAwait(continueOnCapturedContext: false))
					{
						string localName = reader.LocalName;
						if (!(localName == "ExceptionMessage"))
						{
							if (localName == "StackTrace")
							{
								IDictionary<string, string> additionalDetails = extendedErrorInformation.AdditionalDetails;
								additionalDetails.Add("StackTrace", await reader.ReadElementContentAsStringAsync("StackTrace", string.Empty).ConfigureAwait(continueOnCapturedContext: false));
								cancellationToken.ThrowIfCancellationRequested();
							}
							else
							{
								await reader.SkipAsync().ConfigureAwait(continueOnCapturedContext: false);
								cancellationToken.ThrowIfCancellationRequested();
							}
						}
						else
						{
							IDictionary<string, string> additionalDetails = extendedErrorInformation.AdditionalDetails;
							additionalDetails.Add("ExceptionMessage", await reader.ReadElementContentAsStringAsync("ExceptionMessage", string.Empty).ConfigureAwait(continueOnCapturedContext: false));
							cancellationToken.ThrowIfCancellationRequested();
						}
					}
					await reader.ReadEndElementAsync().ConfigureAwait(continueOnCapturedContext: false);
					cancellationToken.ThrowIfCancellationRequested();
				}
				else
				{
					IDictionary<string, string> additionalDetails = extendedErrorInformation.AdditionalDetails;
					string localName2 = reader.LocalName;
					additionalDetails.Add(localName2, await reader.ReadInnerXmlAsync().ConfigureAwait(continueOnCapturedContext: false));
					cancellationToken.ThrowIfCancellationRequested();
				}
			}
			await reader.ReadEndElementAsync().ConfigureAwait(continueOnCapturedContext: false);
			cancellationToken.ThrowIfCancellationRequested();
			return extendedErrorInformation;
		}

		public static Task<StorageExtendedErrorInformation> ReadExtendedErrorInfoFromStreamAsync(Stream inputStream, HttpResponseMessage response, string contentType, CancellationToken token)
		{
			CommonUtility.AssertNotNull("inputStream", inputStream);
			CommonUtility.AssertNotNull("response", response);
			if (inputStream.CanSeek && inputStream.Length <= 0)
			{
				return Task.FromResult<StorageExtendedErrorInformation>(null);
			}
			if (response.Content.Headers.ContentType.MediaType.Contains("xml"))
			{
				return ReadFromStreamAsync(inputStream, token);
			}
			return ReadAndParseJsonExtendedErrorAsync(new NonCloseableStream(inputStream), token);
		}

		internal static async Task<StorageExtendedErrorInformation> ReadAndParseJsonExtendedErrorAsync(Stream responseStream, CancellationToken cancellationToken)
		{
			try
			{
				StreamReader reader2 = new StreamReader(responseStream);
				using (JsonReader reader = new JsonTextReader(reader2))
				{
					reader.DateParseHandling = DateParseHandling.None;
					Dictionary<string, object> dictionary = (await JObject.LoadAsync(reader, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)).ToObject<Dictionary<string, object>>();
					StorageExtendedErrorInformation storageExtendedErrorInformation = new StorageExtendedErrorInformation();
					storageExtendedErrorInformation.AdditionalDetails = new Dictionary<string, string>();
					if (dictionary.ContainsKey("odata.error"))
					{
						Dictionary<string, object> dictionary2 = ((JObject)dictionary["odata.error"]).ToObject<Dictionary<string, object>>();
						if (dictionary2.ContainsKey("code"))
						{
							storageExtendedErrorInformation.ErrorCode = (string)dictionary2["code"];
						}
						if (dictionary2.ContainsKey("message"))
						{
							Dictionary<string, object> dictionary3 = ((JObject)dictionary2["message"]).ToObject<Dictionary<string, object>>();
							if (dictionary3.ContainsKey("value"))
							{
								storageExtendedErrorInformation.ErrorMessage = (string)dictionary3["value"];
							}
						}
						if (dictionary2.ContainsKey("innererror"))
						{
							Dictionary<string, object> dictionary4 = ((JObject)dictionary2["innererror"]).ToObject<Dictionary<string, object>>();
							if (dictionary4.ContainsKey("message"))
							{
								storageExtendedErrorInformation.AdditionalDetails["ExceptionMessage"] = (string)dictionary4["message"];
							}
							if (dictionary4.ContainsKey("type"))
							{
								storageExtendedErrorInformation.AdditionalDetails["exceptiondetails"] = (string)dictionary4["type"];
							}
							if (dictionary4.ContainsKey("stacktrace"))
							{
								storageExtendedErrorInformation.AdditionalDetails["StackTrace"] = (string)dictionary4["stacktrace"];
							}
						}
					}
					return storageExtendedErrorInformation;
				}
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
