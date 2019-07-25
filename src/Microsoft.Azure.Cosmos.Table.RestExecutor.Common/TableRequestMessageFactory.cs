using Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth;
using Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand;
using Microsoft.Azure.Cosmos.Tables.SharedFiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common
{
	internal static class TableRequestMessageFactory
	{
		internal static StorageRequestMessage BuildStorageRequestMessageForTableOperation(Uri uri, TableOperation operation, ICanonicalizer canonicalizer, StorageCredentials cred, OperationContext ctx, TableRequestOptions options)
		{
			HttpMethod httpMethod = RESTCommandGeneratorUtils.ExtractHttpMethod(operation);
			StorageRequestMessage storageRequestMessage = new StorageRequestMessage(httpMethod, uri, canonicalizer, cred, cred.AccountName);
			storageRequestMessage.Headers.AcceptCharset.ParseAdd("UTF-8");
			storageRequestMessage.Headers.Add("MaxDataServiceVersion", "3.0;NetFx");
			TablePayloadFormat value = options.PayloadFormat.Value;
			Logger.LogInformational(ctx, "Setting payload format for the request to '{0}'.", value);
			SetAcceptHeaderValueForStorageRequestMessage(storageRequestMessage, value);
			storageRequestMessage.Headers.Add("DataServiceVersion", "3.0;");
			if (operation.OperationType == TableOperationType.InsertOrMerge || operation.OperationType == TableOperationType.Merge)
			{
				storageRequestMessage.Headers.Add("X-HTTP-Method", "MERGE");
			}
			if ((operation.OperationType == TableOperationType.Delete || operation.OperationType == TableOperationType.Replace || operation.OperationType == TableOperationType.Merge) && operation.ETag != null)
			{
				storageRequestMessage.Headers.TryAddWithoutValidation("If-Match", operation.ETag);
			}
			if (operation.OperationType == TableOperationType.Insert)
			{
				storageRequestMessage.Headers.Add("Prefer", operation.EchoContent ? "return-content" : "return-no-content");
			}
			if (operation.OperationType == TableOperationType.Insert || operation.OperationType == TableOperationType.Merge || operation.OperationType == TableOperationType.InsertOrMerge || operation.OperationType == TableOperationType.InsertOrReplace || operation.OperationType == TableOperationType.Replace)
			{
				MultiBufferMemoryStream multiBufferMemoryStream = new MultiBufferMemoryStream();
				using (JsonTextWriter jsonWriter = new JsonTextWriter(new StreamWriter(new NonCloseableStream(multiBufferMemoryStream))))
				{
					WriteEntityContent(operation, ctx, options, jsonWriter);
				}
				multiBufferMemoryStream.Seek(0L, SeekOrigin.Begin);
				storageRequestMessage.Content = new StreamContent(multiBufferMemoryStream);
			}
			if (httpMethod != HttpMethod.Head && httpMethod != HttpMethod.Get && storageRequestMessage.Content != null)
			{
				storageRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			}
			return storageRequestMessage;
		}

		internal static StorageRequestMessage BuildStorageRequestMessageForTableBatchOperation(Uri uri, TableBatchOperation batch, ICanonicalizer canonicalizer, string tableName, StorageCredentials cred, OperationContext ctx, TableRequestOptions options)
		{
			StorageRequestMessage storageRequestMessage = new StorageRequestMessage(HttpMethod.Post, NavigationHelper.AppendPathToSingleUri(uri, "$batch"), canonicalizer, cred, cred.AccountName);
			storageRequestMessage.Headers.AcceptCharset.ParseAdd("UTF-8");
			storageRequestMessage.Headers.Add("MaxDataServiceVersion", "3.0;NetFx");
			TablePayloadFormat value = options.PayloadFormat.Value;
			Logger.LogInformational(ctx, "Setting payload format for the request to '{0}'.", value);
			SetAcceptHeaderValueForStorageRequestMessage(storageRequestMessage, value);
			storageRequestMessage.Headers.Add("DataServiceVersion", "3.0;");
			MultiBufferMemoryStream multiBufferMemoryStream = new MultiBufferMemoryStream();
			string str = Guid.NewGuid().ToString();
			using (StreamWriter streamWriter = new StreamWriter(new NonCloseableStream(multiBufferMemoryStream)))
			{
				string str2 = Guid.NewGuid().ToString();
				string text = "--batch_" + str;
				string text2 = "--changeset_" + str2;
				string text3 = "Accept: ";
				switch (value)
				{
				case TablePayloadFormat.Json:
					text3 += "application/json;odata=minimalmetadata";
					break;
				case TablePayloadFormat.JsonFullMetadata:
					text3 += "application/json;odata=fullmetadata";
					break;
				case TablePayloadFormat.JsonNoMetadata:
					text3 += "application/json;odata=nometadata";
					break;
				}
				streamWriter.WriteLine(text);
				bool flag = batch.Count == 1 && batch[0].OperationType == TableOperationType.Retrieve;
				if (!flag)
				{
					streamWriter.WriteLine("Content-Type: multipart/mixed; boundary=changeset_" + str2);
					streamWriter.WriteLine();
				}
				foreach (TableOperation item in batch)
				{
					HttpMethod httpMethod = RESTCommandGeneratorUtils.ExtractHttpMethod(item);
					if (item.OperationType == TableOperationType.Merge || item.OperationType == TableOperationType.InsertOrMerge)
					{
						httpMethod = new HttpMethod("MERGE");
					}
					if (!flag)
					{
						streamWriter.WriteLine(text2);
					}
					streamWriter.WriteLine("Content-Type: application/http");
					streamWriter.WriteLine("Content-Transfer-Encoding: binary");
					streamWriter.WriteLine();
					string text4 = Uri.EscapeUriString(RESTCommandGeneratorUtils.GenerateRequestURI(item, uri, tableName).ToString());
					text4 = text4.Replace("%25", "%");
					streamWriter.WriteLine(httpMethod + " " + text4 + " HTTP/1.1");
					streamWriter.WriteLine(text3);
					streamWriter.WriteLine("Content-Type: application/json");
					if (item.OperationType == TableOperationType.Insert)
					{
						streamWriter.WriteLine("Prefer: " + (item.EchoContent ? "return-content" : "return-no-content"));
					}
					streamWriter.WriteLine("DataServiceVersion: 3.0;");
					if (item.OperationType == TableOperationType.Delete || item.OperationType == TableOperationType.Replace || item.OperationType == TableOperationType.Merge)
					{
						streamWriter.WriteLine("If-Match: " + item.ETag);
					}
					streamWriter.WriteLine();
					if (item.OperationType != TableOperationType.Delete && item.OperationType != TableOperationType.Retrieve)
					{
						using (JsonTextWriter jsonTextWriter = new JsonTextWriter(streamWriter))
						{
							jsonTextWriter.CloseOutput = false;
							WriteEntityContent(item, ctx, options, jsonTextWriter);
						}
						streamWriter.WriteLine();
					}
				}
				if (!flag)
				{
					streamWriter.WriteLine(text2 + "--");
				}
				streamWriter.WriteLine(text + "--");
			}
			multiBufferMemoryStream.Seek(0L, SeekOrigin.Begin);
			storageRequestMessage.Content = new StreamContent(multiBufferMemoryStream);
			storageRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("multipart/mixed");
			storageRequestMessage.Content.Headers.ContentType.Parameters.Add(NameValueHeaderValue.Parse("boundary=batch_" + str));
			return storageRequestMessage;
		}

		internal static StorageRequestMessage BuildStorageRequestMessageForTableQuery(Uri uri, UriQueryBuilder builder, ICanonicalizer canonicalizer, StorageCredentials cred, OperationContext ctx, TableRequestOptions options)
		{
			StorageRequestMessage storageRequestMessage = new StorageRequestMessage(HttpMethod.Get, builder.AddToUri(uri), canonicalizer, cred, cred.AccountName);
			storageRequestMessage.Headers.AcceptCharset.ParseAdd("UTF-8");
			storageRequestMessage.Headers.Add("MaxDataServiceVersion", "3.0;NetFx");
			TablePayloadFormat value = options.PayloadFormat.Value;
			Logger.LogInformational(ctx, "Setting payload format for the request to '{0}'.", value);
			SetAcceptHeaderValueForStorageRequestMessage(storageRequestMessage, value);
			storageRequestMessage.Headers.Add("DataServiceVersion", "3.0;");
			Logger.LogInformational(ctx, "Setting payload format for the request to '{0}'.", value);
			return storageRequestMessage;
		}

		private static void SetAcceptHeaderValueForStorageRequestMessage(StorageRequestMessage message, TablePayloadFormat payloadFormat)
		{
			switch (payloadFormat)
			{
			case TablePayloadFormat.JsonFullMetadata:
				message.Headers.Accept.ParseAdd("application/json;odata=fullmetadata");
				break;
			case TablePayloadFormat.Json:
				message.Headers.Accept.ParseAdd("application/json;odata=minimalmetadata");
				break;
			case TablePayloadFormat.JsonNoMetadata:
				message.Headers.Accept.ParseAdd("application/json;odata=nometadata");
				break;
			}
		}

		private static void WriteEntityContent(TableOperation operation, OperationContext ctx, TableRequestOptions options, JsonTextWriter jsonWriter)
		{
			ITableEntity entity = operation.Entity;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			foreach (KeyValuePair<string, object> propertiesWithKey in GetPropertiesWithKeys(entity, ctx, operation.OperationType, options))
			{
				if (propertiesWithKey.Value != null)
				{
					if (propertiesWithKey.Value.GetType() == typeof(DateTime))
					{
						dictionary[propertiesWithKey.Key] = ((DateTime)propertiesWithKey.Value).ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
						dictionary[propertiesWithKey.Key + "@odata.type"] = "Edm.DateTime";
					}
					else if (propertiesWithKey.Value.GetType() == typeof(byte[]))
					{
						dictionary[propertiesWithKey.Key] = Convert.ToBase64String((byte[])propertiesWithKey.Value);
						dictionary[propertiesWithKey.Key + "@odata.type"] = "Edm.Binary";
					}
					else if (propertiesWithKey.Value.GetType() == typeof(long))
					{
						dictionary[propertiesWithKey.Key] = propertiesWithKey.Value.ToString();
						dictionary[propertiesWithKey.Key + "@odata.type"] = "Edm.Int64";
					}
					else if (propertiesWithKey.Value.GetType() == typeof(Guid))
					{
						dictionary[propertiesWithKey.Key] = propertiesWithKey.Value.ToString();
						dictionary[propertiesWithKey.Key + "@odata.type"] = "Edm.Guid";
					}
					else
					{
						dictionary[propertiesWithKey.Key] = propertiesWithKey.Value;
					}
				}
			}
			JObject.FromObject(dictionary, EntityTranslator.JsonSerializer).WriteTo(jsonWriter);
		}

		private static IEnumerable<KeyValuePair<string, object>> GetPropertiesWithKeys(ITableEntity entity, OperationContext operationContext, TableOperationType operationType, TableRequestOptions options, bool ignoreEncryption = false)
		{
			if (operationType == TableOperationType.Insert)
			{
				if (entity.PartitionKey != null)
				{
					yield return new KeyValuePair<string, object>("PartitionKey", entity.PartitionKey);
				}
				if (entity.RowKey != null)
				{
					yield return new KeyValuePair<string, object>("RowKey", entity.RowKey);
				}
			}
			foreach (KeyValuePair<string, object> item in GetPropertiesFromDictionary(entity.WriteEntity(operationContext), options, entity.PartitionKey, entity.RowKey, ignoreEncryption))
			{
				yield return item;
			}
		}

		internal static IEnumerable<KeyValuePair<string, object>> GetPropertiesFromDictionary(IDictionary<string, EntityProperty> properties, TableRequestOptions options, string partitionKey, string rowKey, bool ignoreEncryption)
		{
			return from kvp in properties
			select new KeyValuePair<string, object>(kvp.Key, kvp.Value.PropertyAsObject);
		}

		internal static StorageRequestMessage BuildStorageRequestMessageForTableServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, HttpMethod httpMethod, ServiceProperties properties, ICanonicalizer canonicalizer, StorageCredentials cred, OperationContext ctx, TableRequestOptions options)
		{
			if (builder == null)
			{
				builder = new UriQueryBuilder();
			}
			builder.Add("comp", "properties");
			builder.Add("restype", "service");
			if (timeout.HasValue && timeout.Value > 0)
			{
				builder.Add("timeout", timeout.Value.ToString(CultureInfo.InvariantCulture));
			}
			StorageRequestMessage storageRequestMessage = new StorageRequestMessage(httpMethod, builder.AddToUri(uri), canonicalizer, cred, cred.AccountName);
			if (httpMethod.Equals(HttpMethod.Put))
			{
				MultiBufferMemoryStream multiBufferMemoryStream = new MultiBufferMemoryStream(1024);
				try
				{
					properties.WriteServiceProperties(multiBufferMemoryStream);
				}
				catch (InvalidOperationException ex)
				{
					multiBufferMemoryStream.Dispose();
					throw new ArgumentException(ex.Message, "properties");
				}
				multiBufferMemoryStream.Seek(0L, SeekOrigin.Begin);
				storageRequestMessage.Content = new StreamContent(multiBufferMemoryStream);
			}
			return storageRequestMessage;
		}

		internal static StorageRequestMessage BuildStorageRequestMessageForGetServiceStats(Uri uri, UriQueryBuilder builder, int? timeout, ICanonicalizer canonicalizer, StorageCredentials cred, OperationContext ctx, TableRequestOptions options)
		{
			if (builder == null)
			{
				builder = new UriQueryBuilder();
			}
			builder.Add("comp", "stats");
			builder.Add("restype", "service");
			if (timeout.HasValue && timeout.Value > 0)
			{
				builder.Add("timeout", timeout.Value.ToString(CultureInfo.InvariantCulture));
			}
			return new StorageRequestMessage(HttpMethod.Get, builder.AddToUri(uri), canonicalizer, cred, cred.AccountName);
		}

		internal static StorageRequestMessage BuildStorageRequestMessageForTablePermissions(Uri uri, UriQueryBuilder builder, int? timeout, HttpMethod httpMethod, TablePermissions permissions, ICanonicalizer canonicalizer, StorageCredentials cred, OperationContext ctx, TableRequestOptions options)
		{
			if (builder == null)
			{
				builder = new UriQueryBuilder();
			}
			builder.Add("comp", "acl");
			if (timeout.HasValue && timeout.Value > 0)
			{
				builder.Add("timeout", timeout.Value.ToString(CultureInfo.InvariantCulture));
			}
			StorageRequestMessage storageRequestMessage = new StorageRequestMessage(httpMethod, builder.AddToUri(uri), canonicalizer, cred, cred.AccountName);
			if (httpMethod.Equals(HttpMethod.Put))
			{
				MultiBufferMemoryStream multiBufferMemoryStream = new MultiBufferMemoryStream(1024);
				WriteSharedAccessIdentifiers(permissions.SharedAccessPolicies, multiBufferMemoryStream);
				multiBufferMemoryStream.Seek(0L, SeekOrigin.Begin);
				storageRequestMessage.Content = new StreamContent(multiBufferMemoryStream);
			}
			return storageRequestMessage;
		}

		private static void WriteSharedAccessIdentifiers(SharedAccessTablePolicies sharedAccessPolicies, Stream outputStream)
		{
			WriteSharedAccessIdentifiers(sharedAccessPolicies, outputStream, delegate(SharedAccessTablePolicy policy, XmlWriter writer)
			{
				writer.WriteElementString("Start", SharedAccessSignatureHelper.GetDateTimeOrEmpty(policy.SharedAccessStartTime));
				writer.WriteElementString("Expiry", SharedAccessSignatureHelper.GetDateTimeOrEmpty(policy.SharedAccessExpiryTime));
				writer.WriteElementString("Permission", SharedAccessTablePolicy.PermissionsToString(policy.Permissions));
			});
		}

		private static void WriteSharedAccessIdentifiers<T>(IDictionary<string, T> sharedAccessPolicies, Stream outputStream, Action<T, XmlWriter> writePolicyXml)
		{
			CommonUtility.AssertNotNull("sharedAccessPolicies", sharedAccessPolicies);
			CommonUtility.AssertNotNull("outputStream", outputStream);
			if (sharedAccessPolicies.Count > 5)
			{
				throw new ArgumentOutOfRangeException("sharedAccessPolicies", string.Format(CultureInfo.CurrentCulture, "Too many '{0}' shared access policy identifiers provided. Server does not support setting more than '{1}' on a single container, queue, table, or share.", new object[2]
				{
					sharedAccessPolicies.Count,
					5
				}));
			}
			using (XmlWriter xmlWriter = XmlWriter.Create(outputStream, new XmlWriterSettings
			{
				Encoding = Encoding.UTF8
			}))
			{
				xmlWriter.WriteStartElement("SignedIdentifiers");
				foreach (string key in sharedAccessPolicies.Keys)
				{
					xmlWriter.WriteStartElement("SignedIdentifier");
					xmlWriter.WriteElementString("Id", key);
					xmlWriter.WriteStartElement("AccessPolicy");
					T arg = sharedAccessPolicies[key];
					writePolicyXml(arg, xmlWriter);
					xmlWriter.WriteEndElement();
					xmlWriter.WriteEndElement();
				}
				xmlWriter.WriteEndDocument();
			}
		}
	}
}
