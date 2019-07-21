using System;
using System.Globalization;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class StorageCredentials
	{
		private SasQueryBuilder queryBuilder;

		public string SASToken
		{
			get;
			private set;
		}

		public string AccountName
		{
			get;
			private set;
		}

		public string Key
		{
			get;
			private set;
		}

		public string KeyName
		{
			get;
			private set;
		}

		public bool IsSharedKey
		{
			get
			{
				if (SASToken == null)
				{
					return AccountName != null;
				}
				return false;
			}
		}

		public bool IsAnonymous
		{
			get
			{
				if (SASToken == null)
				{
					return AccountName == null;
				}
				return false;
			}
		}

		public bool IsSAS => SASToken != null;

		public string SASSignature
		{
			get
			{
				if (IsSAS)
				{
					return queryBuilder["sig"];
				}
				return null;
			}
		}

		public StorageCredentials()
		{
		}

		public StorageCredentials(string sasToken)
		{
			CommonUtility.AssertNotNullOrEmpty("sasToken", sasToken);
			SASToken = sasToken;
			UpdateQueryBuilder();
		}

		public StorageCredentials(string accountName, string keyValue)
			: this(accountName, keyValue, null)
		{
		}

		public StorageCredentials(string accountName, string keyValue, string keyName)
		{
			CommonUtility.AssertNotNullOrEmpty("accountName", accountName);
			AccountName = accountName;
			UpdateKey(keyValue, keyName);
		}

		public void UpdateKey(string keyValue)
		{
			if (!IsSharedKey)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Cannot update key unless Account Key credentials are used."));
			}
			CommonUtility.AssertNotNull("keyValue", keyValue);
			Key = keyValue;
		}

		public void UpdateKey(string keyValue, string keyName)
		{
			if (!IsSharedKey)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Cannot update key unless Account Key credentials are used."));
			}
			CommonUtility.AssertNotNull("keyValue", keyValue);
			KeyName = keyName;
			Key = keyValue;
		}

		public bool Equals(StorageCredentials other)
		{
			if (other == null)
			{
				return false;
			}
			if (string.Equals(AccountName, other.AccountName))
			{
				return string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		public Uri TransformUri(Uri resourceUri)
		{
			CommonUtility.AssertNotNull("resourceUri", resourceUri);
			if (IsSAS)
			{
				return queryBuilder.AddToUri(resourceUri);
			}
			return resourceUri;
		}

		public StorageUri TransformUri(StorageUri resourceUri)
		{
			CommonUtility.AssertNotNull("resourceUri", resourceUri);
			return new StorageUri(TransformUri(resourceUri.PrimaryUri), TransformUri(resourceUri.SecondaryUri));
		}

		public void UpdateSASToken(string sasToken)
		{
			if (!IsSAS)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Cannot update Shared Access Signature unless Sas credentials are used."));
			}
			CommonUtility.AssertNotNullOrEmpty("sasToken", sasToken);
			SASToken = sasToken;
			UpdateQueryBuilder();
		}

		private void UpdateQueryBuilder()
		{
			SasQueryBuilder sasQueryBuilder = new SasQueryBuilder(SASToken);
			if (sasQueryBuilder.ContainsQueryStringName("api-version"))
			{
				throw new ArgumentException($"The parameter `api-version` should not be included in the SAS token. Please allow the library to set the  `api-version` parameter.");
			}
			sasQueryBuilder.Add("api-version", "2018-03-28");
			queryBuilder = sasQueryBuilder;
		}
	}
}
