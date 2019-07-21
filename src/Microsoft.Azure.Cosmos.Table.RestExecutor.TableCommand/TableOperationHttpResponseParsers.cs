using Microsoft.Azure.Cosmos.Table.RestExecutor.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal static class TableOperationHttpResponseParsers
	{
		internal static TableResult TableOperationPreProcess(TableResult result, TableOperation operation, HttpResponseMessage resp, Exception ex)
		{
			result.HttpStatusCode = (int)resp.StatusCode;
			if (operation.OperationType == TableOperationType.Retrieve)
			{
				if (resp.StatusCode != HttpStatusCode.OK && resp.StatusCode != HttpStatusCode.NotFound)
				{
					CommonUtility.AssertNotNull("ex", ex);
					throw ex;
				}
			}
			else
			{
				if (ex != null)
				{
					throw ex;
				}
				if (operation.OperationType == TableOperationType.Insert)
				{
					if (operation.EchoContent)
					{
						if (resp.StatusCode != HttpStatusCode.Created)
						{
							throw ex;
						}
					}
					else if (resp.StatusCode != HttpStatusCode.NoContent)
					{
						throw ex;
					}
				}
				else if (resp.StatusCode != HttpStatusCode.NoContent)
				{
					throw ex;
				}
			}
			string text = (resp.Headers.ETag != null) ? resp.Headers.ETag.ToString() : null;
			if (text != null)
			{
				result.Etag = text;
				if (operation.Entity != null)
				{
					operation.Entity.ETag = result.Etag;
				}
			}
			return result;
		}

		internal static async Task<TableResult> TableOperationPostProcessAsync(TableResult result, TableOperation operation, RESTCommand<TableResult> cmd, HttpResponseMessage resp, OperationContext ctx, TableRequestOptions options, string accountName, CancellationToken cancellationToken)
		{
			string text = (resp.Headers.ETag != null) ? resp.Headers.ETag.ToString() : null;
			if (operation.OperationType != TableOperationType.Retrieve && operation.OperationType != 0)
			{
				result.Etag = text;
				operation.Entity.ETag = result.Etag;
			}
			else if (operation.OperationType == TableOperationType.Insert && !operation.EchoContent)
			{
				if (text != null)
				{
					result.Etag = text;
					operation.Entity.ETag = result.Etag;
					operation.Entity.Timestamp = ParseETagForTimestamp(result.Etag);
				}
			}
			else
			{
				MediaTypeHeaderValue contentType = resp.Content.Headers.ContentType;
				if (!contentType.MediaType.Equals("application/json") || !contentType.Parameters.Contains(NameValueHeaderValue.Parse("odata=nometadata")))
				{
					await ReadOdataEntityAsync(result, operation, cmd.ResponseStream, ctx, accountName, options, cancellationToken);
				}
				else
				{
					result.Etag = text;
					await ReadEntityUsingJsonParserAsync(result, operation, cmd.ResponseStream, ctx, options, cancellationToken);
				}
			}
			return result;
		}

		internal static async Task<TableBatchResult> TableBatchOperationPostProcessAsync(TableBatchResult result, TableBatchOperation batch, RESTCommand<TableBatchResult> cmd, HttpResponseMessage resp, OperationContext ctx, TableRequestOptions options, string accountName, CancellationToken cancellationToken)
		{
			Stream responseStream = cmd.ResponseStream;
			StreamReader streamReader = new StreamReader(responseStream);
			await streamReader.ReadLineAsync().ConfigureAwait(continueOnCapturedContext: false);
			string currentLine3 = await streamReader.ReadLineAsync().ConfigureAwait(continueOnCapturedContext: false);
			int index = 0;
			bool failError = false;
			while (currentLine3 != null && !currentLine3.StartsWith("--batchresponse"))
			{
				while (!currentLine3.StartsWith("HTTP"))
				{
					currentLine3 = await streamReader.ReadLineAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				int statusCode = int.Parse(currentLine3.Substring(9, 3));
				Dictionary<string, string> headers = new Dictionary<string, string>();
				currentLine3 = await streamReader.ReadLineAsync().ConfigureAwait(continueOnCapturedContext: false);
				while (!string.IsNullOrWhiteSpace(currentLine3))
				{
					int num = currentLine3.IndexOf(':');
					headers[currentLine3.Substring(0, num)] = currentLine3.Substring(num + 2);
					currentLine3 = await streamReader.ReadLineAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				MemoryStream bodyStream = null;
				currentLine3 = await streamReader.ReadLineAsync().ConfigureAwait(continueOnCapturedContext: false);
				if (statusCode != 204)
				{
					bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(currentLine3));
				}
				await streamReader.ReadLineAsync().ConfigureAwait(continueOnCapturedContext: false);
				currentLine3 = await streamReader.ReadLineAsync().ConfigureAwait(continueOnCapturedContext: false);
				TableOperation tableOperation = batch[index];
				TableResult obj = new TableResult
				{
					Result = tableOperation.Entity
				};
				result.Add(obj);
				string arg = null;
				if (headers.ContainsKey("Content-Type"))
				{
					arg = headers["Content-Type"];
				}
				obj.HttpStatusCode = statusCode;
				bool flag;
				if (tableOperation.OperationType == TableOperationType.Insert)
				{
					failError = (statusCode == 409);
					flag = ((!tableOperation.EchoContent) ? (statusCode != 204) : (statusCode != 201));
				}
				else if (tableOperation.OperationType == TableOperationType.Retrieve)
				{
					if (statusCode == 404)
					{
						index++;
						continue;
					}
					flag = (statusCode != 200);
				}
				else
				{
					failError = (statusCode == 404);
					flag = (statusCode != 204);
				}
				if (failError)
				{
					if (cmd.ParseErrorAsync != null)
					{
						cmd.CurrentResult.ExtendedErrorInformation = cmd.ParseErrorAsync(bodyStream, resp, arg, CancellationToken.None).Result;
					}
					cmd.CurrentResult.HttpStatusCode = statusCode;
					if (!string.IsNullOrEmpty(cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage))
					{
						string errorMessage = cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage;
						cmd.CurrentResult.HttpStatusMessage = errorMessage.Substring(0, errorMessage.IndexOf("\n", StringComparison.Ordinal));
					}
					else
					{
						cmd.CurrentResult.HttpStatusMessage = statusCode.ToString(CultureInfo.InvariantCulture);
					}
					throw new StorageException(cmd.CurrentResult, (cmd.CurrentResult.ExtendedErrorInformation != null) ? cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage : "An unknown error has occurred, extended error information not available.", null)
					{
						IsRetryable = false
					};
				}
				if (flag)
				{
					if (cmd.ParseErrorAsync != null)
					{
						cmd.CurrentResult.ExtendedErrorInformation = cmd.ParseErrorAsync(bodyStream, resp, arg, CancellationToken.None).Result;
					}
					cmd.CurrentResult.HttpStatusCode = statusCode;
					if (!string.IsNullOrEmpty(cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage))
					{
						string errorMessage2 = cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage;
						cmd.CurrentResult.HttpStatusMessage = errorMessage2.Substring(0, errorMessage2.IndexOf("\n", StringComparison.Ordinal));
					}
					else
					{
						cmd.CurrentResult.HttpStatusMessage = statusCode.ToString(CultureInfo.InvariantCulture);
					}
					string arg2 = Convert.ToString(index, CultureInfo.InvariantCulture);
					if (cmd.CurrentResult.ExtendedErrorInformation != null && !string.IsNullOrEmpty(cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage))
					{
						string text = ExtractEntityIndexFromExtendedErrorInformation(cmd.CurrentResult);
						if (!string.IsNullOrEmpty(text))
						{
							arg2 = text;
						}
					}
					throw new StorageException(cmd.CurrentResult, string.Format(CultureInfo.CurrentCulture, "Element {0} in the batch returned an unexpected response code.", arg2), null)
					{
						IsRetryable = true
					};
				}
				if (headers.ContainsKey("ETag") && !string.IsNullOrEmpty(headers["ETag"]))
				{
					obj.Etag = headers["ETag"];
					if (tableOperation.Entity != null)
					{
						tableOperation.Entity.ETag = obj.Etag;
					}
				}
				if (tableOperation.OperationType == TableOperationType.Retrieve || (tableOperation.OperationType == TableOperationType.Insert && tableOperation.EchoContent))
				{
					if (!headers["Content-Type"].Contains("application/json;odata=nometadata"))
					{
						await ReadOdataEntityAsync(obj, tableOperation, bodyStream, ctx, accountName, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					}
					else
					{
						await ReadEntityUsingJsonParserAsync(obj, tableOperation, bodyStream, ctx, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					}
				}
				else if (tableOperation.OperationType == TableOperationType.Insert)
				{
					tableOperation.Entity.Timestamp = ParseETagForTimestamp(obj.Etag);
				}
				index++;
			}
			return result;
		}

		internal static async Task<ResultSegment<TElement>> TableQueryPostProcessGenericAsync<TElement, TQueryType>(Stream responseStream, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, TElement> resolver, HttpResponseMessage resp, TableRequestOptions options, OperationContext ctx, CancellationToken cancellationToken)
		{
			ResultSegment<TElement> retSeg = new ResultSegment<TElement>(new List<TElement>())
			{
				ContinuationToken = ContinuationFromResponse(resp)
			};
			MediaTypeHeaderValue contentType = resp.Content.Headers.ContentType;
			if (contentType.MediaType.Equals("application/json") && contentType.Parameters.Contains(NameValueHeaderValue.Parse("odata=nometadata")))
			{
				await ReadQueryResponseUsingJsonParserAsync(retSeg, responseStream, resp.Headers.ETag?.Tag, resolver, options.PropertyResolver, typeof(TQueryType), null, options, cancellationToken);
			}
			else
			{
				foreach (KeyValuePair<string, Dictionary<string, object>> item in await ReadQueryResponseUsingJsonParserMetadataAsync(responseStream, cancellationToken))
				{
					retSeg.Results.Add(ReadAndResolve(item.Key, item.Value, resolver, options));
				}
			}
			Logger.LogInformational(ctx, "Retrieved '{0}' results with continuation token '{1}'.", retSeg.Results.Count, retSeg.ContinuationToken);
			return retSeg;
		}

		internal static async Task ReadQueryResponseUsingJsonParserAsync<TElement>(ResultSegment<TElement> retSeg, Stream responseStream, string etag, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, TElement> resolver, Func<string, string, string, string, EdmType> propertyResolver, Type type, OperationContext ctx, TableRequestOptions options, CancellationToken cancellationToken)
		{
			StreamReader reader2 = new StreamReader(responseStream);
			bool disablePropertyResolverCache = false;
			if (TableEntity.DisablePropertyResolverCache)
			{
				disablePropertyResolverCache = TableEntity.DisablePropertyResolverCache;
				Logger.LogVerbose(ctx, "Property resolver cache is disabled.");
			}
			using (JsonReader reader = new JsonTextReader(reader2))
			{
				reader.DateParseHandling = DateParseHandling.None;
				foreach (JToken item in (IEnumerable<JToken>)(await JObject.LoadAsync(reader, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))["value"])
				{
					string etag2;
					Dictionary<string, object> dictionary = ReadSingleItem(item, out etag2);
					Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
					foreach (string key in dictionary.Keys)
					{
						if (dictionary[key] == null)
						{
							dictionary2.Add(key, null);
						}
						else if (dictionary[key] is string)
						{
							dictionary2.Add(key, (string)dictionary[key]);
						}
						else if (dictionary[key] is DateTime)
						{
							dictionary2.Add(key, ((DateTime)dictionary[key]).ToUniversalTime().ToString("o"));
						}
						else
						{
							if (!(dictionary[key] is bool) && !(dictionary[key] is double) && !(dictionary[key] is int))
							{
								throw new StorageException(string.Format(CultureInfo.InvariantCulture, "Invalid type in JSON object. Detected type is {0}, which is not a valid JSON type.", dictionary[key].GetType().ToString()));
							}
							dictionary2.Add(key, dictionary[key].ToString());
						}
					}
					retSeg.Results.Add(ReadAndResolveWithEdmTypeResolver(dictionary2, resolver, propertyResolver, etag, type, ctx, disablePropertyResolverCache, options));
				}
				if (await reader.ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
				{
					throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "The JSON reader has not yet reached the completed state."));
				}
			}
		}

		public static async Task<List<KeyValuePair<string, Dictionary<string, object>>>> ReadQueryResponseUsingJsonParserMetadataAsync(Stream responseStream, CancellationToken cancellationToken)
		{
			List<KeyValuePair<string, Dictionary<string, object>>> results = new List<KeyValuePair<string, Dictionary<string, object>>>();
			StreamReader reader2 = new StreamReader(responseStream);
			using (JsonReader reader = new JsonTextReader(reader2))
			{
				reader.DateParseHandling = DateParseHandling.None;
				foreach (JToken item in (IEnumerable<JToken>)(await JObject.LoadAsync(reader, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))["value"])
				{
					string etag;
					Dictionary<string, object> value = ReadSingleItem(item, out etag);
					results.Add(new KeyValuePair<string, Dictionary<string, object>>(etag, value));
				}
				if (await reader.ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
				{
					throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "The JSON reader has not yet reached the completed state."));
				}
			}
			return results;
		}

		internal static TableContinuationToken ContinuationFromResponse(HttpResponseMessage response)
		{
			HttpResponseHeaders headers = response.Headers;
			IEnumerable<string> values = new List<string>();
			string text = null;
			if (headers.TryGetValues("x-ms-continuation-NextPartitionKey", out values))
			{
				text = values.First();
			}
			IEnumerable<string> values2 = new List<string>();
			string text2 = null;
			if (headers.TryGetValues("x-ms-continuation-NextRowKey", out values2))
			{
				text2 = values2.First();
			}
			IEnumerable<string> values3 = new List<string>();
			string text3 = null;
			if (headers.TryGetValues("x-ms-continuation-NextTableName", out values3))
			{
				text3 = values3.First();
			}
			text = (string.IsNullOrEmpty(text) ? null : text);
			text2 = (string.IsNullOrEmpty(text2) ? null : text2);
			text3 = (string.IsNullOrEmpty(text3) ? null : text3);
			if (text == null && text2 == null && text3 == null)
			{
				return null;
			}
			return new TableContinuationToken
			{
				NextPartitionKey = text,
				NextRowKey = text2,
				NextTableName = text3
			};
		}

		private static DateTimeOffset ParseETagForTimestamp(string etag)
		{
			if (etag.StartsWith("W/", StringComparison.Ordinal))
			{
				etag = etag.Substring(2);
			}
			etag = etag.Substring("\"datetime'".Length, etag.Length - 2 - "\"datetime'".Length);
			return DateTimeOffset.Parse(Uri.UnescapeDataString(etag), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
		}

		private static async Task ReadEntityUsingJsonParserAsync(TableResult result, TableOperation operation, Stream stream, OperationContext ctx, TableRequestOptions options, CancellationToken cancellationToken)
		{
			StreamReader reader2 = new StreamReader(stream);
			using (JsonReader reader = new JsonTextReader(reader2))
			{
				string etag;
				Dictionary<string, object> dictionary = ReadSingleItem(await JObject.LoadAsync(reader, cancellationToken).ConfigureAwait(continueOnCapturedContext: false), out etag);
				Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
				foreach (string key in dictionary.Keys)
				{
					if (dictionary[key] == null)
					{
						dictionary2.Add(key, null);
					}
					else
					{
						Type type = dictionary[key].GetType();
						if (type == typeof(string))
						{
							dictionary2.Add(key, (string)dictionary[key]);
						}
						else if (type == typeof(DateTime))
						{
							dictionary2.Add(key, ((DateTime)dictionary[key]).ToUniversalTime().ToString("o"));
						}
						else if (type == typeof(bool))
						{
							dictionary2.Add(key, ((bool)dictionary[key]).ToString());
						}
						else if (type == typeof(double))
						{
							dictionary2.Add(key, ((double)dictionary[key]).ToString());
						}
						else
						{
							if (!(type == typeof(int)))
							{
								throw new StorageException();
							}
							dictionary2.Add(key, ((int)dictionary[key]).ToString());
						}
					}
				}
				if (operation.OperationType == TableOperationType.Retrieve)
				{
					result.Result = ReadAndResolveWithEdmTypeResolver(dictionary2, operation.RetrieveResolver, options.PropertyResolver, result.Etag, operation.PropertyResolverType, ctx, TableEntity.DisablePropertyResolverCache, options);
				}
				else
				{
					ReadAndUpdateTableEntityWithEdmTypeResolver(operation.Entity, dictionary2, EntityReadFlags.Timestamp | EntityReadFlags.Etag, options.PropertyResolver, ctx);
				}
				if (reader.Read())
				{
					throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "The JSON reader has not yet reached the completed state."));
				}
			}
		}

		private static Dictionary<string, object> ReadSingleItem(JToken token, out string etag)
		{
			Dictionary<string, object> dictionary = token.ToObject<Dictionary<string, object>>();
			if (dictionary.ContainsKey("odata.etag"))
			{
				etag = (string)dictionary["odata.etag"];
			}
			else
			{
				etag = null;
			}
			string[] array = (from key in dictionary.Keys
			where key.StartsWith("odata.", StringComparison.Ordinal)
			select key).ToArray();
			foreach (string key2 in array)
			{
				dictionary.Remove(key2);
			}
			if (dictionary.ContainsKey("Timestamp") && dictionary["Timestamp"].GetType() == typeof(string))
			{
				dictionary["Timestamp"] = DateTime.Parse((string)dictionary["Timestamp"], CultureInfo.InvariantCulture);
			}
			if (dictionary.ContainsKey("Timestamp@odata.type"))
			{
				dictionary.Remove("Timestamp@odata.type");
			}
			KeyValuePair<string, object>[] array2 = dictionary.Where(delegate(KeyValuePair<string, object> kvp)
			{
				if (kvp.Value != null)
				{
					return kvp.Value.GetType() == typeof(long);
				}
				return false;
			}).ToArray();
			for (int i = 0; i < array2.Length; i++)
			{
				KeyValuePair<string, object> keyValuePair = array2[i];
				dictionary[keyValuePair.Key] = (int)(long)keyValuePair.Value;
			}
			array2 = (from kvp in dictionary
			where kvp.Key.EndsWith("@odata.type", StringComparison.Ordinal)
			select kvp).ToArray();
			for (int i = 0; i < array2.Length; i++)
			{
				KeyValuePair<string, object> keyValuePair2 = array2[i];
				dictionary.Remove(keyValuePair2.Key);
				string key3 = keyValuePair2.Key.Split(new char[1]
				{
					'@'
				}, StringSplitOptions.RemoveEmptyEntries)[0];
				switch ((string)keyValuePair2.Value)
				{
				case "Edm.DateTime":
					dictionary[key3] = DateTime.Parse((string)dictionary[key3], null, DateTimeStyles.AdjustToUniversal);
					break;
				case "Edm.Binary":
					dictionary[key3] = Convert.FromBase64String((string)dictionary[key3]);
					break;
				case "Edm.Guid":
					dictionary[key3] = Guid.Parse((string)dictionary[key3]);
					break;
				case "Edm.Int64":
					dictionary[key3] = long.Parse((string)dictionary[key3], CultureInfo.InvariantCulture);
					break;
				default:
					throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unexpected EDM type from the Table Service: {0}.", keyValuePair2.Value));
				}
			}
			return dictionary;
		}

		private static T ReadAndResolveWithEdmTypeResolver<T>(Dictionary<string, string> entityAttributes, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, T> resolver, Func<string, string, string, string, EdmType> propertyResolver, string etag, Type type, OperationContext ctx, bool disablePropertyResolverCache, TableRequestOptions options)
		{
			string arg = null;
			string arg2 = null;
			DateTimeOffset arg3 = default(DateTimeOffset);
			Dictionary<string, EntityProperty> dictionary = new Dictionary<string, EntityProperty>();
			Dictionary<string, EdmType> dictionary2 = null;
			HashSet<string> encryptedPropertyDetailsSet = null;
			if (type != null)
			{
				dictionary2 = (disablePropertyResolverCache ? CreatePropertyResolverDictionary(type) : TableEntity.PropertyResolverCache.GetOrAdd(type, CreatePropertyResolverDictionary));
			}
			foreach (KeyValuePair<string, string> entityAttribute in entityAttributes)
			{
				if (entityAttribute.Key == "PartitionKey")
				{
					arg = entityAttribute.Value;
				}
				else if (entityAttribute.Key == "RowKey")
				{
					arg2 = entityAttribute.Value;
				}
				else if (entityAttribute.Key == "Timestamp")
				{
					arg3 = DateTimeOffset.Parse(entityAttribute.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
					if (etag == null)
					{
						etag = GetETagFromTimestamp(entityAttribute.Value);
					}
				}
				else if (propertyResolver != null)
				{
					Logger.LogVerbose(ctx, "Using the property resolver provided via TableRequestOptions to deserialize the entity.");
					try
					{
						EdmType edmType = propertyResolver(arg, arg2, entityAttribute.Key, entityAttribute.Value);
						Logger.LogVerbose(ctx, "Attempting to deserialize '{0}' as type '{1}'", entityAttribute.Key, edmType);
						try
						{
							CreateEntityPropertyFromObject(dictionary, encryptedPropertyDetailsSet, entityAttribute, edmType);
						}
						catch (FormatException innerException)
						{
							throw new StorageException(string.Format(CultureInfo.InvariantCulture, "Failed to parse property '{0}' with value '{1}' as type '{2}'", entityAttribute.Key, entityAttribute.Value, edmType), innerException)
							{
								IsRetryable = false
							};
						}
					}
					catch (StorageException)
					{
						throw;
					}
					catch (Exception innerException2)
					{
						throw new StorageException("The custom property resolver delegate threw an exception. Check the inner exception for more details.", innerException2)
						{
							IsRetryable = false
						};
					}
				}
				else if (type != null)
				{
					Logger.LogVerbose(ctx, "Using the default property resolver to deserialize the entity.");
					if (dictionary2 != null)
					{
						dictionary2.TryGetValue(entityAttribute.Key, out EdmType value);
						Logger.LogVerbose(ctx, "Attempting to deserialize '{0}' as type '{1}'", entityAttribute.Key, value);
						CreateEntityPropertyFromObject(dictionary, encryptedPropertyDetailsSet, entityAttribute, value);
					}
				}
				else
				{
					Logger.LogVerbose(ctx, "No property resolver available. Deserializing the entity properties as strings.");
					CreateEntityPropertyFromObject(dictionary, encryptedPropertyDetailsSet, entityAttribute, EdmType.String);
				}
			}
			return resolver(arg, arg2, arg3, dictionary, etag);
		}

		private static Dictionary<string, EdmType> CreatePropertyResolverDictionary(Type type)
		{
			Dictionary<string, EdmType> dictionary = new Dictionary<string, EdmType>();
			foreach (PropertyInfo item in (IEnumerable<PropertyInfo>)type.GetProperties())
			{
				if (item.PropertyType == typeof(byte[]))
				{
					dictionary.Add(item.Name, EdmType.Binary);
				}
				else if (item.PropertyType == typeof(bool) || item.PropertyType == typeof(bool?))
				{
					dictionary.Add(item.Name, EdmType.Boolean);
				}
				else if (item.PropertyType == typeof(DateTime) || item.PropertyType == typeof(DateTime?) || item.PropertyType == typeof(DateTimeOffset) || item.PropertyType == typeof(DateTimeOffset?))
				{
					dictionary.Add(item.Name, EdmType.DateTime);
				}
				else if (item.PropertyType == typeof(double) || item.PropertyType == typeof(double?))
				{
					dictionary.Add(item.Name, EdmType.Double);
				}
				else if (item.PropertyType == typeof(Guid) || item.PropertyType == typeof(Guid?))
				{
					dictionary.Add(item.Name, EdmType.Guid);
				}
				else if (item.PropertyType == typeof(int) || item.PropertyType == typeof(int?))
				{
					dictionary.Add(item.Name, EdmType.Int32);
				}
				else if (item.PropertyType == typeof(long) || item.PropertyType == typeof(long?))
				{
					dictionary.Add(item.Name, EdmType.Int64);
				}
				else
				{
					dictionary.Add(item.Name, EdmType.String);
				}
			}
			return dictionary;
		}

		private static string GetETagFromTimestamp(string timeStampString)
		{
			timeStampString = Uri.EscapeDataString(timeStampString);
			return "W/\"datetime'" + timeStampString + "'\"";
		}

		private static void CreateEntityPropertyFromObject(Dictionary<string, EntityProperty> properties, HashSet<string> encryptedPropertyDetailsSet, KeyValuePair<string, string> prop, EdmType edmType)
		{
			if (encryptedPropertyDetailsSet != null && encryptedPropertyDetailsSet.Contains(prop.Key))
			{
				properties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value, EdmType.Binary));
			}
			else
			{
				properties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value, edmType));
			}
		}

		private static async Task ReadOdataEntityAsync(TableResult result, TableOperation operation, Stream responseStream, OperationContext ctx, string accountName, TableRequestOptions options, CancellationToken cancellationToken)
		{
			KeyValuePair<string, Dictionary<string, object>> keyValuePair = await ReadSingleItemResponseUsingJsonParserMetadataAsync(responseStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (operation.OperationType == TableOperationType.Retrieve)
			{
				result.Result = ReadAndResolve(keyValuePair.Key, keyValuePair.Value, operation.RetrieveResolver, options);
				result.Etag = keyValuePair.Key;
			}
			else
			{
				result.Etag = ReadAndUpdateTableEntity(operation.Entity, keyValuePair.Key, keyValuePair.Value, EntityReadFlags.Timestamp | EntityReadFlags.Etag, ctx);
			}
		}

		public static async Task<KeyValuePair<string, Dictionary<string, object>>> ReadSingleItemResponseUsingJsonParserMetadataAsync(Stream responseStream, CancellationToken cancellationToken)
		{
			StreamReader reader2 = new StreamReader(responseStream);
			using (JsonReader reader = new JsonTextReader(reader2))
			{
				reader.DateParseHandling = DateParseHandling.None;
				string etag;
				Dictionary<string, object> properties = ReadSingleItem(await JObject.LoadAsync(reader, cancellationToken).ConfigureAwait(continueOnCapturedContext: false), out etag);
				if (await reader.ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
				{
					throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "The JSON reader has not yet reached the completed state."));
				}
				return new KeyValuePair<string, Dictionary<string, object>>(etag, properties);
			}
		}

		private static T ReadAndResolve<T>(string etag, Dictionary<string, object> props, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, T> resolver, TableRequestOptions options)
		{
			string arg = null;
			string arg2 = null;
			DateTimeOffset arg3 = default(DateTimeOffset);
			Dictionary<string, EntityProperty> dictionary = new Dictionary<string, EntityProperty>();
			foreach (KeyValuePair<string, object> prop in props)
			{
				string key = prop.Key;
				if (key == "PartitionKey")
				{
					arg = (string)prop.Value;
				}
				else if (key == "RowKey")
				{
					arg2 = (string)prop.Value;
				}
				else if (key == "Timestamp")
				{
					arg3 = new DateTimeOffset((DateTime)prop.Value);
				}
				else
				{
					dictionary.Add(key, EntityProperty.CreateEntityPropertyFromObject(prop.Value));
				}
			}
			return resolver(arg, arg2, arg3, dictionary, etag);
		}

		internal static string ReadAndUpdateTableEntity(ITableEntity entity, string etag, Dictionary<string, object> props, EntityReadFlags flags, OperationContext ctx)
		{
			if ((flags & EntityReadFlags.Etag) > (EntityReadFlags)0)
			{
				entity.ETag = etag;
			}
			Dictionary<string, EntityProperty> dictionary = ((flags & EntityReadFlags.Properties) > (EntityReadFlags)0) ? new Dictionary<string, EntityProperty>() : null;
			if (flags > (EntityReadFlags)0)
			{
				foreach (KeyValuePair<string, object> prop in props)
				{
					if (prop.Key == "PartitionKey")
					{
						if ((flags & EntityReadFlags.PartitionKey) != 0)
						{
							entity.PartitionKey = (string)prop.Value;
						}
					}
					else if (prop.Key == "RowKey")
					{
						if ((flags & EntityReadFlags.RowKey) != 0)
						{
							entity.RowKey = (string)prop.Value;
						}
					}
					else if (prop.Key == "Timestamp")
					{
						if ((flags & EntityReadFlags.Timestamp) != 0)
						{
							entity.Timestamp = (DateTime)prop.Value;
						}
					}
					else if ((flags & EntityReadFlags.Properties) > (EntityReadFlags)0)
					{
						dictionary.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value));
					}
				}
				if ((flags & EntityReadFlags.Properties) > (EntityReadFlags)0)
				{
					entity.ReadEntity(dictionary, ctx);
				}
			}
			return etag;
		}

		internal static void ReadAndUpdateTableEntityWithEdmTypeResolver(ITableEntity entity, Dictionary<string, string> entityAttributes, EntityReadFlags flags, Func<string, string, string, string, EdmType> propertyResolver, OperationContext ctx)
		{
			Dictionary<string, EntityProperty> dictionary = ((flags & EntityReadFlags.Properties) > (EntityReadFlags)0) ? new Dictionary<string, EntityProperty>() : null;
			Dictionary<string, EdmType> dictionary2 = null;
			if (entity.GetType() != typeof(DynamicTableEntity))
			{
				if (!TableEntity.DisablePropertyResolverCache)
				{
					dictionary2 = TableEntity.PropertyResolverCache.GetOrAdd(entity.GetType(), CreatePropertyResolverDictionary);
				}
				else
				{
					Logger.LogVerbose(ctx, "Property resolver cache is disabled.");
					dictionary2 = CreatePropertyResolverDictionary(entity.GetType());
				}
			}
			if (flags > (EntityReadFlags)0)
			{
				foreach (KeyValuePair<string, string> entityAttribute in entityAttributes)
				{
					if (entityAttribute.Key == "PartitionKey")
					{
						entity.PartitionKey = entityAttribute.Value;
					}
					else if (entityAttribute.Key == "RowKey")
					{
						entity.RowKey = entityAttribute.Value;
					}
					else if (entityAttribute.Key == "Timestamp")
					{
						if ((flags & EntityReadFlags.Timestamp) != 0)
						{
							entity.Timestamp = DateTime.Parse(entityAttribute.Value, CultureInfo.InvariantCulture);
						}
					}
					else if ((flags & EntityReadFlags.Properties) > (EntityReadFlags)0)
					{
						if (propertyResolver != null)
						{
							Logger.LogVerbose(ctx, "Using the property resolver provided via TableRequestOptions to deserialize the entity.");
							try
							{
								EdmType edmType = propertyResolver(entity.PartitionKey, entity.RowKey, entityAttribute.Key, entityAttribute.Value);
								Logger.LogVerbose(ctx, "Attempting to deserialize '{0}' as type '{1}'", entityAttribute.Key, edmType.GetType().ToString());
								try
								{
									dictionary.Add(entityAttribute.Key, EntityProperty.CreateEntityPropertyFromObject(entityAttribute.Value, edmType.GetType()));
								}
								catch (FormatException innerException)
								{
									throw new StorageException(string.Format(CultureInfo.InvariantCulture, "Failed to parse property '{0}' with value '{1}' as type '{2}'", entityAttribute.Key, entityAttribute.Value, edmType.ToString()), innerException)
									{
										IsRetryable = false
									};
								}
							}
							catch (StorageException)
							{
								throw;
							}
							catch (Exception innerException2)
							{
								throw new StorageException("The custom property resolver delegate threw an exception. Check the inner exception for more details.", innerException2)
								{
									IsRetryable = false
								};
							}
						}
						else if (entity.GetType() != typeof(DynamicTableEntity))
						{
							Logger.LogVerbose(ctx, "Using the default property resolver to deserialize the entity.");
							if (dictionary2 != null)
							{
								dictionary2.TryGetValue(entityAttribute.Key, out EdmType value);
								Logger.LogVerbose(ctx, "Attempting to deserialize '{0}' as type '{1}'", entityAttribute.Key, value);
								dictionary.Add(entityAttribute.Key, EntityProperty.CreateEntityPropertyFromObject(entityAttribute.Value, value));
							}
						}
						else
						{
							Logger.LogVerbose(ctx, "No property resolver available. Deserializing the entity properties as strings.");
							dictionary.Add(entityAttribute.Key, EntityProperty.CreateEntityPropertyFromObject(entityAttribute.Value, typeof(string)));
						}
					}
				}
				if ((flags & EntityReadFlags.Properties) > (EntityReadFlags)0)
				{
					entity.ReadEntity(dictionary, ctx);
				}
			}
		}

		internal static string ExtractEntityIndexFromExtendedErrorInformation(RequestResult result)
		{
			if (result != null && result.ExtendedErrorInformation != null && !string.IsNullOrEmpty(result.ExtendedErrorInformation.ErrorMessage))
			{
				int num = result.ExtendedErrorInformation.ErrorMessage.IndexOf(":");
				if (num > 0 && num < 3)
				{
					return result.ExtendedErrorInformation.ErrorMessage.Substring(0, num);
				}
			}
			return null;
		}

		internal static Task<ServiceProperties> ReadServicePropertiesAsync(Stream inputStream)
		{
			using (XmlReader reader = XmlReader.Create(inputStream))
			{
				return ServiceProperties.FromServiceXmlAsync(XDocument.Load(reader));
			}
		}

		internal static Task<ServiceStats> ReadServiceStatsAsync(Stream inputStream)
		{
			using (XmlReader reader = XmlReader.Create(inputStream))
			{
				return ServiceStats.FromServiceXmlAsync(XDocument.Load(reader));
			}
		}

		internal static Task<TablePermissions> ParseGetAclAsync(RESTCommand<TablePermissions> cmd, HttpResponseMessage resp, OperationContext ctx)
		{
			TablePermissions tablePermissions = new TablePermissions();
			CommonUtility.AssertNotNull("permissions", tablePermissions);
			SharedAccessTablePolicies sharedAccessPolicies = tablePermissions.SharedAccessPolicies;
			foreach (KeyValuePair<string, SharedAccessTablePolicy> accessIdentifier in new TableAccessPolicyResponse(cmd.ResponseStream).AccessIdentifiers)
			{
				sharedAccessPolicies.Add(accessIdentifier.Key, accessIdentifier.Value);
			}
			return Task.FromResult(tablePermissions);
		}

		internal static DateTime ToUTCTime(this string str)
		{
			return DateTime.Parse(str, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AdjustToUniversal);
		}
	}
}
