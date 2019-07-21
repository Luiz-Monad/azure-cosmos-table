using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Cosmos.Table
{
	internal static class SharedAccessSignatureHelper
	{
		internal static string GetDateTimeOrEmpty(DateTimeOffset? value)
		{
			return GetDateTimeOrNull(value) ?? string.Empty;
		}

		internal static string GetDateTimeOrNull(DateTimeOffset? value)
		{
			if (!value.HasValue)
			{
				return null;
			}
			return value.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
		}

		internal static string GetProtocolString(SharedAccessProtocol? protocols)
		{
			if (!protocols.HasValue)
			{
				return null;
			}
			if (protocols.Value != SharedAccessProtocol.HttpsOnly && protocols.Value != SharedAccessProtocol.HttpsOrHttp)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid value {0} for the SharedAccessProtocol parameter when creating a SharedAccessSignature.  Use 'null' if you do not wish to include a SharedAccessProtocol.", new object[1]
				{
					protocols.Value
				}));
			}
			if (protocols.Value == SharedAccessProtocol.HttpsOnly)
			{
				return "https";
			}
			return "https,http";
		}

		internal static void AddEscapedIfNotNull(UriQueryBuilder builder, string name, string value)
		{
			if (value != null)
			{
				builder.Add(name, value);
			}
		}

		internal static StorageCredentials ParseQuery(IDictionary<string, string> queryParameters)
		{
			bool flag = false;
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, string> queryParameter in queryParameters)
			{
				switch (queryParameter.Key.ToLower())
				{
				case "sig":
					flag = true;
					break;
				case "restype":
				case "comp":
				case "snapshot":
				case "api-version":
				case "sharesnapshot":
					list.Add(queryParameter.Key);
					break;
				}
			}
			foreach (string item in list)
			{
				queryParameters.Remove(item);
			}
			if (!flag)
			{
				return null;
			}
			UriQueryBuilder uriQueryBuilder = new UriQueryBuilder();
			foreach (KeyValuePair<string, string> queryParameter2 in queryParameters)
			{
				AddEscapedIfNotNull(uriQueryBuilder, queryParameter2.Key.ToLower(), queryParameter2.Value);
			}
			return new StorageCredentials(uriQueryBuilder.ToString());
		}

		internal static string GetHash(SharedAccessAccountPolicy policy, string accountName, string sasVersion, string keyValue)
		{
			string text = string.Format(CultureInfo.InvariantCulture, "{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}\n{8}\n{9}", accountName, SharedAccessAccountPolicy.PermissionsToString(policy.Permissions), SharedAccessAccountPolicy.ServicesToString(policy.Services), SharedAccessAccountPolicy.ResourceTypesToString(policy.ResourceTypes), GetDateTimeOrEmpty(policy.SharedAccessStartTime), GetDateTimeOrEmpty(policy.SharedAccessExpiryTime), (policy.IPAddressOrRange == null) ? string.Empty : policy.IPAddressOrRange.ToString(), GetProtocolString(policy.Protocols), sasVersion, string.Empty);
			Logger.LogVerbose(null, "StringToSign = {0}.", text);
			return CryptoUtility.ComputeHmac256(keyValue, text);
		}

		internal static UriQueryBuilder GetSignature(SharedAccessAccountPolicy policy, string signature, string accountKeyName, string sasVersion)
		{
			CommonUtility.AssertNotNull("signature", signature);
			CommonUtility.AssertNotNull("policy", policy);
			UriQueryBuilder uriQueryBuilder = new UriQueryBuilder();
			AddEscapedIfNotNull(uriQueryBuilder, "sv", sasVersion);
			AddEscapedIfNotNull(uriQueryBuilder, "sk", accountKeyName);
			AddEscapedIfNotNull(uriQueryBuilder, "sig", signature);
			AddEscapedIfNotNull(uriQueryBuilder, "spr", (!policy.Protocols.HasValue) ? null : GetProtocolString(policy.Protocols.Value));
			AddEscapedIfNotNull(uriQueryBuilder, "sip", (policy.IPAddressOrRange == null) ? null : policy.IPAddressOrRange.ToString());
			AddEscapedIfNotNull(uriQueryBuilder, "st", GetDateTimeOrNull(policy.SharedAccessStartTime));
			AddEscapedIfNotNull(uriQueryBuilder, "se", GetDateTimeOrNull(policy.SharedAccessExpiryTime));
			string value = SharedAccessAccountPolicy.ResourceTypesToString(policy.ResourceTypes);
			if (!string.IsNullOrEmpty(value))
			{
				AddEscapedIfNotNull(uriQueryBuilder, "srt", value);
			}
			string value2 = SharedAccessAccountPolicy.ServicesToString(policy.Services);
			if (!string.IsNullOrEmpty(value2))
			{
				AddEscapedIfNotNull(uriQueryBuilder, "ss", value2);
			}
			string value3 = SharedAccessAccountPolicy.PermissionsToString(policy.Permissions);
			if (!string.IsNullOrEmpty(value3))
			{
				AddEscapedIfNotNull(uriQueryBuilder, "sp", value3);
			}
			return uriQueryBuilder;
		}

		internal static UriQueryBuilder GetSignature(SharedAccessTablePolicy policy, string tableName, string accessPolicyIdentifier, string startPartitionKey, string startRowKey, string endPartitionKey, string endRowKey, string signature, string accountKeyName, string sasVersion, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
		{
			CommonUtility.AssertNotNull("signature", signature);
			UriQueryBuilder uriQueryBuilder = new UriQueryBuilder();
			AddEscapedIfNotNull(uriQueryBuilder, "sv", sasVersion);
			AddEscapedIfNotNull(uriQueryBuilder, "tn", tableName);
			AddEscapedIfNotNull(uriQueryBuilder, "spk", startPartitionKey);
			AddEscapedIfNotNull(uriQueryBuilder, "srk", startRowKey);
			AddEscapedIfNotNull(uriQueryBuilder, "epk", endPartitionKey);
			AddEscapedIfNotNull(uriQueryBuilder, "erk", endRowKey);
			AddEscapedIfNotNull(uriQueryBuilder, "si", accessPolicyIdentifier);
			AddEscapedIfNotNull(uriQueryBuilder, "sk", accountKeyName);
			AddEscapedIfNotNull(uriQueryBuilder, "sig", signature);
			AddEscapedIfNotNull(uriQueryBuilder, "spr", GetProtocolString(protocols));
			AddEscapedIfNotNull(uriQueryBuilder, "sip", ipAddressOrRange?.ToString());
			if (policy != null)
			{
				AddEscapedIfNotNull(uriQueryBuilder, "st", GetDateTimeOrNull(policy.SharedAccessStartTime));
				AddEscapedIfNotNull(uriQueryBuilder, "se", GetDateTimeOrNull(policy.SharedAccessExpiryTime));
				string value = SharedAccessTablePolicy.PermissionsToString(policy.Permissions);
				if (!string.IsNullOrEmpty(value))
				{
					AddEscapedIfNotNull(uriQueryBuilder, "sp", value);
				}
			}
			return uriQueryBuilder;
		}

		internal static string GetHash(SharedAccessTablePolicy policy, string accessPolicyIdentifier, string startPartitionKey, string startRowKey, string endPartitionKey, string endRowKey, string resourceName, string sasVersion, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange, string keyValue)
		{
			CommonUtility.AssertNotNullOrEmpty("resourceName", resourceName);
			CommonUtility.AssertNotNull("keyValue", keyValue);
			CommonUtility.AssertNotNullOrEmpty("sasVersion", sasVersion);
			string text = null;
			DateTimeOffset? value = null;
			DateTimeOffset? value2 = null;
			if (policy != null)
			{
				text = SharedAccessTablePolicy.PermissionsToString(policy.Permissions);
				value = policy.SharedAccessStartTime;
				value2 = policy.SharedAccessExpiryTime;
			}
			string text2 = string.Format(CultureInfo.InvariantCulture, "{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}\n{8}\n{9}\n{10}\n{11}", text, GetDateTimeOrEmpty(value), GetDateTimeOrEmpty(value2), resourceName, accessPolicyIdentifier, (ipAddressOrRange == null) ? string.Empty : ipAddressOrRange.ToString(), GetProtocolString(protocols), sasVersion, startPartitionKey, startRowKey, endPartitionKey, endRowKey);
			Logger.LogVerbose(null, "StringToSign = {0}.", text2);
			return CryptoUtility.ComputeHmac256(keyValue, text2);
		}
	}
}
