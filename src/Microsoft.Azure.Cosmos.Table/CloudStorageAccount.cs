using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class CloudStorageAccount
	{
		private static readonly KeyValuePair<string, Func<string, bool>> DefaultEndpointsProtocolSetting = Setting("DefaultEndpointsProtocol", "http", "https");

		private static readonly KeyValuePair<string, Func<string, bool>> AccountNameSetting = Setting("AccountName");

		private static readonly KeyValuePair<string, Func<string, bool>> AccountKeyNameSetting = Setting("AccountKeyName");

		private static readonly KeyValuePair<string, Func<string, bool>> AccountKeySetting = Setting("AccountKey", IsValidBase64String);

		private static readonly KeyValuePair<string, Func<string, bool>> BlobEndpointSetting = Setting("BlobEndpoint", IsValidUri);

		private static readonly KeyValuePair<string, Func<string, bool>> QueueEndpointSetting = Setting("QueueEndpoint", IsValidUri);

		private static readonly KeyValuePair<string, Func<string, bool>> TableEndpointSetting = Setting("TableEndpoint", IsValidUri);

		private static readonly KeyValuePair<string, Func<string, bool>> FileEndpointSetting = Setting("FileEndpoint", IsValidUri);

		private static readonly KeyValuePair<string, Func<string, bool>> BlobSecondaryEndpointSetting = Setting("BlobSecondaryEndpoint", IsValidUri);

		private static readonly KeyValuePair<string, Func<string, bool>> QueueSecondaryEndpointSetting = Setting("QueueSecondaryEndpoint", IsValidUri);

		private static readonly KeyValuePair<string, Func<string, bool>> TableSecondaryEndpointSetting = Setting("TableSecondaryEndpoint", IsValidUri);

		private static readonly KeyValuePair<string, Func<string, bool>> FileSecondaryEndpointSetting = Setting("FileSecondaryEndpoint", IsValidUri);

		private static readonly KeyValuePair<string, Func<string, bool>> EndpointSuffixSetting = Setting("EndpointSuffix", IsValidDomain);

		private static readonly KeyValuePair<string, Func<string, bool>> SharedAccessSignatureSetting = Setting("SharedAccessSignature");

		private static Func<IDictionary<string, string>, IDictionary<string, string>> ValidCredentials = MatchesOne(MatchesAll(AllRequired(AccountNameSetting, AccountKeySetting), Optional(AccountKeyNameSetting), None(SharedAccessSignatureSetting)), MatchesAll(AllRequired(SharedAccessSignatureSetting), Optional(AccountNameSetting), None(AccountKeySetting, AccountKeyNameSetting)), None(AccountNameSetting, AccountKeySetting, AccountKeyNameSetting, SharedAccessSignatureSetting));

		internal const string UseDevelopmentStorageSettingString = "UseDevelopmentStorage";

		internal const string DevelopmentStorageProxyUriSettingString = "DevelopmentStorageProxyUri";

		internal const string DefaultEndpointsProtocolSettingString = "DefaultEndpointsProtocol";

		internal const string AccountNameSettingString = "AccountName";

		internal const string AccountKeyNameSettingString = "AccountKeyName";

		internal const string AccountKeySettingString = "AccountKey";

		internal const string BlobEndpointSettingString = "BlobEndpoint";

		internal const string QueueEndpointSettingString = "QueueEndpoint";

		internal const string TableEndpointSettingString = "TableEndpoint";

		internal const string FileEndpointSettingString = "FileEndpoint";

		internal const string BlobSecondaryEndpointSettingString = "BlobSecondaryEndpoint";

		internal const string QueueSecondaryEndpointSettingString = "QueueSecondaryEndpoint";

		internal const string TableSecondaryEndpointSettingString = "TableSecondaryEndpoint";

		internal const string FileSecondaryEndpointSettingString = "FileSecondaryEndpoint";

		internal const string EndpointSuffixSettingString = "EndpointSuffix";

		internal const string SharedAccessSignatureSettingString = "SharedAccessSignature";

		private const string DevstoreAccountName = "devstoreaccount1";

		private const string DevstoreAccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

		internal const string SecondaryLocationAccountSuffix = "-secondary";

		private const string DefaultEndpointSuffix = "core.windows.net";

		private const string DefaultTableHostnamePrefix = "table";

		private static readonly KeyValuePair<string, Func<string, bool>> UseDevelopmentStorageSetting = Setting("UseDevelopmentStorage", "true");

		private static readonly KeyValuePair<string, Func<string, bool>> DevelopmentStorageProxyUriSetting = Setting("DevelopmentStorageProxyUri", IsValidUri);

		private string accountName;

		private static CloudStorageAccount devStoreAccount;

		public static CloudStorageAccount DevelopmentStorageAccount
		{
			get
			{
				if (devStoreAccount == null)
				{
					devStoreAccount = GetDevelopmentStorageAccount(null);
				}
				return devStoreAccount;
			}
		}

		private bool IsDevStoreAccount
		{
			get;
			set;
		}

		private string EndpointSuffix
		{
			get;
			set;
		}

		private IDictionary<string, string> Settings
		{
			get;
			set;
		}

		private bool DefaultEndpoints
		{
			get;
			set;
		}

		public Uri TableEndpoint
		{
			get
			{
				if (TableStorageUri == null)
				{
					return null;
				}
				return TableStorageUri.PrimaryUri;
			}
		}

		public StorageUri TableStorageUri
		{
			get;
			private set;
		}

		public StorageCredentials Credentials
		{
			get;
			private set;
		}

		public CloudStorageAccount(StorageCredentials storageCredentials, Uri tableEndpoint)
			: this(storageCredentials, new StorageUri(tableEndpoint))
		{
		}

		public CloudStorageAccount(StorageCredentials storageCredentials, StorageUri tableStorageUri)
		{
			Credentials = storageCredentials;
			TableStorageUri = tableStorageUri;
			DefaultEndpoints = false;
		}

		public CloudStorageAccount(StorageCredentials storageCredentials, bool useHttps)
			: this(storageCredentials, null, useHttps)
		{
		}

		public CloudStorageAccount(StorageCredentials storageCredentials, string endpointSuffix, bool useHttps)
			: this(storageCredentials, storageCredentials?.AccountName, endpointSuffix, useHttps)
		{
		}

		public CloudStorageAccount(StorageCredentials storageCredentials, string accountName, string endpointSuffix, bool useHttps)
		{
			CommonUtility.AssertNotNull("storageCredentials", storageCredentials);
			if (!string.IsNullOrEmpty(storageCredentials.AccountName))
			{
				if (string.IsNullOrEmpty(accountName))
				{
					accountName = storageCredentials.AccountName;
				}
				else if (string.Compare(storageCredentials.AccountName, accountName, StringComparison.Ordinal) != 0)
				{
					throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Account names do not match.  First account name is {0}, second is {1}.", new object[2]
					{
						storageCredentials.AccountName,
						accountName
					}));
				}
			}
			CommonUtility.AssertNotNull("AccountName", accountName);
			string scheme = useHttps ? "https" : "http";
			TableStorageUri = ConstructTableEndpoint(scheme, accountName, endpointSuffix);
			Credentials = storageCredentials;
			EndpointSuffix = endpointSuffix;
			DefaultEndpoints = true;
		}

		public static CloudStorageAccount Parse(string connectionString)
		{
			if (string.IsNullOrEmpty(connectionString))
			{
				throw new ArgumentNullException("connectionString");
			}
			CloudStorageAccount accountInformation = default(CloudStorageAccount);
			if (ParseImpl(connectionString, out accountInformation, delegate(string err)
			{
				throw new FormatException(err);
			}))
			{
				return accountInformation;
			}
			throw new ArgumentException("Error parsing value");
		}

		public static bool TryParse(string connectionString, out CloudStorageAccount account)
		{
			if (string.IsNullOrEmpty(connectionString))
			{
				account = null;
				return false;
			}
			try
			{
				return ParseImpl(connectionString, out account, delegate
				{
				});
			}
			catch (Exception)
			{
				account = null;
				return false;
			}
		}

		public string GetSharedAccessSignature(SharedAccessAccountPolicy policy)
		{
			if (!Credentials.IsSharedKey)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Cannot create Shared Access Signature unless Account Key credentials are used."));
			}
			string hash = SharedAccessSignatureHelper.GetHash(policy, Credentials.AccountName, "2018-03-28", Credentials.Key);
			return SharedAccessSignatureHelper.GetSignature(policy, hash, Credentials.KeyName, "2018-03-28").ToString();
		}

		public override string ToString()
		{
			if (Settings == null)
			{
				Settings = new Dictionary<string, string>();
				if (DefaultEndpoints)
				{
					if (EndpointSuffix != null)
					{
						Settings.Add("EndpointSuffix", EndpointSuffix);
					}
				}
				else if (TableEndpoint != null)
				{
					Settings.Add("TableEndpoint", TableEndpoint.ToString());
				}
			}
			List<string> list = (from pair in Settings
			select string.Format(CultureInfo.InvariantCulture, "{0}={1}", new object[2]
			{
				pair.Key,
				pair.Value
			})).ToList();
			if (!string.IsNullOrWhiteSpace(accountName) && (Credentials == null || (string.IsNullOrWhiteSpace(Credentials.AccountName) ? true : false)))
			{
				list.Add(string.Format(CultureInfo.InvariantCulture, "{0}={1}", new object[2]
				{
					"AccountName",
					accountName
				}));
			}
			return string.Join(";", list);
		}

		private static CloudStorageAccount GetDevelopmentStorageAccount(Uri proxyUri)
		{
			UriBuilder obj = (proxyUri != null) ? new UriBuilder(proxyUri.Scheme, proxyUri.Host) : new UriBuilder("http", "127.0.0.1");
			obj.Path = "devstoreaccount1";
			obj.Port = 10002;
			Uri uri = obj.Uri;
			obj.Path = "devstoreaccount1-secondary";
			obj.Port = 10002;
			Uri uri2 = obj.Uri;
			CloudStorageAccount cloudStorageAccount = new CloudStorageAccount(new StorageCredentials("devstoreaccount1", "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="), new StorageUri(uri, uri2));
			cloudStorageAccount.Settings = new Dictionary<string, string>();
			cloudStorageAccount.Settings.Add("UseDevelopmentStorage", "true");
			if (proxyUri != null)
			{
				cloudStorageAccount.Settings.Add("DevelopmentStorageProxyUri", proxyUri.ToString());
			}
			cloudStorageAccount.IsDevStoreAccount = true;
			return cloudStorageAccount;
		}

		internal static bool ParseImpl(string connectionString, out CloudStorageAccount accountInformation, Action<string> error)
		{
			IDictionary<string, string> settings = ParseStringIntoSettings(connectionString, error);
			if (settings == null)
			{
				accountInformation = null;
				return false;
			}
			Func<string, string> func = delegate(string key)
			{
				string value2 = null;
				settings.TryGetValue(key, out value2);
				return value2;
			};
			if (MatchesSpecification(settings, AllRequired(UseDevelopmentStorageSetting), Optional(DevelopmentStorageProxyUriSetting)))
			{
				if (settings.TryGetValue("DevelopmentStorageProxyUri", out string value))
				{
					accountInformation = GetDevelopmentStorageAccount(new Uri(value));
				}
				else
				{
					accountInformation = DevelopmentStorageAccount;
				}
				accountInformation.Settings = ValidCredentials(settings);
				return true;
			}
			Func<IDictionary<string, string>, IDictionary<string, string>> func2 = Optional(BlobEndpointSetting, BlobSecondaryEndpointSetting, QueueEndpointSetting, QueueSecondaryEndpointSetting, TableEndpointSetting, TableSecondaryEndpointSetting, FileEndpointSetting, FileSecondaryEndpointSetting);
			Func<IDictionary<string, string>, IDictionary<string, string>> func3 = AtLeastOne(BlobEndpointSetting, QueueEndpointSetting, TableEndpointSetting, FileEndpointSetting);
			Func<IDictionary<string, string>, IDictionary<string, string>> func4 = Optional(BlobSecondaryEndpointSetting, QueueSecondaryEndpointSetting, TableSecondaryEndpointSetting, FileSecondaryEndpointSetting);
			Func<IDictionary<string, string>, IDictionary<string, string>> func5 = MatchesExactly(MatchesAll(MatchesOne(MatchesAll(AllRequired(AccountKeySetting), Optional(AccountKeyNameSetting)), AllRequired(SharedAccessSignatureSetting)), AllRequired(AccountNameSetting), func2, Optional(DefaultEndpointsProtocolSetting, EndpointSuffixSetting)));
			Func<IDictionary<string, string>, IDictionary<string, string>> func6 = MatchesExactly(MatchesAll(ValidCredentials, func3, func4));
			bool matchesAutomaticEndpointsSpec = MatchesSpecification(settings, func5);
			bool flag = MatchesSpecification(settings, func6);
			if (matchesAutomaticEndpointsSpec | flag)
			{
				if (matchesAutomaticEndpointsSpec && !settings.ContainsKey("DefaultEndpointsProtocol"))
				{
					settings.Add("DefaultEndpointsProtocol", "https");
				}
				string text = func("BlobEndpoint");
				string text2 = func("QueueEndpoint");
				string text3 = func("TableEndpoint");
				string text4 = func("FileEndpoint");
				string arg = func("BlobSecondaryEndpoint");
				string arg2 = func("QueueSecondaryEndpoint");
				string arg3 = func("TableSecondaryEndpoint");
				string arg4 = func("FileSecondaryEndpoint");
				Func<string, string, bool> func7 = delegate(string primary, string secondary)
				{
					if (string.IsNullOrWhiteSpace(primary))
					{
						return string.IsNullOrWhiteSpace(secondary);
					}
					return true;
				};
				Func<string, string, Func<IDictionary<string, string>, StorageUri>, StorageUri> func8 = delegate(string primary, string secondary, Func<IDictionary<string, string>, StorageUri> factory)
				{
					if (string.IsNullOrWhiteSpace(secondary) || string.IsNullOrWhiteSpace(primary))
					{
						if (string.IsNullOrWhiteSpace(primary))
						{
							if (!matchesAutomaticEndpointsSpec || factory == null)
							{
								return new StorageUri(null);
							}
							return factory(settings);
						}
						return new StorageUri(new Uri(primary));
					}
					return new StorageUri(new Uri(primary), new Uri(secondary));
				};
				if (func7(text, arg) && func7(text2, arg2) && func7(text3, arg3) && func7(text4, arg4))
				{
					accountInformation = new CloudStorageAccount(GetCredentials(settings), func8(text3, arg3, ConstructTableEndpoint))
					{
						DefaultEndpoints = (text == null && text2 == null && text3 == null && text4 == null),
						EndpointSuffix = func("EndpointSuffix"),
						Settings = ValidCredentials(settings)
					};
					accountInformation.accountName = func("AccountName");
					return true;
				}
			}
			accountInformation = null;
			error("No valid combination of account information found.");
			return false;
		}

		private static IDictionary<string, string> ParseStringIntoSettings(string connectionString, Action<string> error)
		{
			IDictionary<string, string> dictionary = new Dictionary<string, string>();
			string[] array = connectionString.Split(new char[1]
			{
				';'
			}, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split(new char[1]
				{
					'='
				}, 2);
				if (array2.Length != 2)
				{
					error("Settings must be of the form \"name=value\".");
					return null;
				}
				if (dictionary.ContainsKey(array2[0]))
				{
					error(string.Format(CultureInfo.InvariantCulture, "Duplicate setting '{0}' found.", array2[0]));
					return null;
				}
				dictionary.Add(array2[0], array2[1]);
			}
			return dictionary;
		}

		private static KeyValuePair<string, Func<string, bool>> Setting(string name, params string[] validValues)
		{
			return new KeyValuePair<string, Func<string, bool>>(name, delegate(string settingValue)
			{
				if (validValues.Length == 0)
				{
					return true;
				}
				return validValues.Contains(settingValue);
			});
		}

		private static KeyValuePair<string, Func<string, bool>> Setting(string name, Func<string, bool> isValid)
		{
			return new KeyValuePair<string, Func<string, bool>>(name, isValid);
		}

		private static bool IsValidBase64String(string settingValue)
		{
			try
			{
				Convert.FromBase64String(settingValue);
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}

		private static bool IsValidUri(string settingValue)
		{
			return Uri.IsWellFormedUriString(settingValue, UriKind.Absolute);
		}

		private static bool IsValidDomain(string settingValue)
		{
			return Uri.CheckHostName(settingValue).Equals(UriHostNameType.Dns);
		}

		private static Func<IDictionary<string, string>, IDictionary<string, string>> AllRequired(params KeyValuePair<string, Func<string, bool>>[] requiredSettings)
		{
			return delegate(IDictionary<string, string> settings)
			{
				IDictionary<string, string> dictionary = new Dictionary<string, string>(settings);
				KeyValuePair<string, Func<string, bool>>[] array = requiredSettings;
				for (int i = 0; i < array.Length; i++)
				{
					KeyValuePair<string, Func<string, bool>> keyValuePair = array[i];
					if (!dictionary.TryGetValue(keyValuePair.Key, out string value) || !keyValuePair.Value(value))
					{
						return null;
					}
					dictionary.Remove(keyValuePair.Key);
				}
				return dictionary;
			};
		}

		private static Func<IDictionary<string, string>, IDictionary<string, string>> Optional(params KeyValuePair<string, Func<string, bool>>[] optionalSettings)
		{
			return delegate(IDictionary<string, string> settings)
			{
				IDictionary<string, string> dictionary = new Dictionary<string, string>(settings);
				KeyValuePair<string, Func<string, bool>>[] array = optionalSettings;
				for (int i = 0; i < array.Length; i++)
				{
					KeyValuePair<string, Func<string, bool>> keyValuePair = array[i];
					if (dictionary.TryGetValue(keyValuePair.Key, out string value) && keyValuePair.Value(value))
					{
						dictionary.Remove(keyValuePair.Key);
					}
				}
				return dictionary;
			};
		}

		private static Func<IDictionary<string, string>, IDictionary<string, string>> AtLeastOne(params KeyValuePair<string, Func<string, bool>>[] atLeastOneSettings)
		{
			return delegate(IDictionary<string, string> settings)
			{
				IDictionary<string, string> dictionary = new Dictionary<string, string>(settings);
				bool flag = false;
				KeyValuePair<string, Func<string, bool>>[] array = atLeastOneSettings;
				for (int i = 0; i < array.Length; i++)
				{
					KeyValuePair<string, Func<string, bool>> keyValuePair = array[i];
					if (dictionary.TryGetValue(keyValuePair.Key, out string value) && keyValuePair.Value(value))
					{
						dictionary.Remove(keyValuePair.Key);
						flag = true;
					}
				}
				if (!flag)
				{
					return null;
				}
				return dictionary;
			};
		}

		private static Func<IDictionary<string, string>, IDictionary<string, string>> None(params KeyValuePair<string, Func<string, bool>>[] atLeastOneSettings)
		{
			return delegate(IDictionary<string, string> settings)
			{
				IDictionary<string, string> dictionary = new Dictionary<string, string>(settings);
				bool flag = false;
				KeyValuePair<string, Func<string, bool>>[] array = atLeastOneSettings;
				for (int i = 0; i < array.Length; i++)
				{
					KeyValuePair<string, Func<string, bool>> keyValuePair = array[i];
					if (dictionary.TryGetValue(keyValuePair.Key, out string value) && keyValuePair.Value(value))
					{
						flag = true;
					}
				}
				if (!flag)
				{
					return dictionary;
				}
				return null;
			};
		}

		private static Func<IDictionary<string, string>, IDictionary<string, string>> MatchesAll(params Func<IDictionary<string, string>, IDictionary<string, string>>[] filters)
		{
			return delegate(IDictionary<string, string> settings)
			{
				IDictionary<string, string> dictionary = new Dictionary<string, string>(settings);
				Func<IDictionary<string, string>, IDictionary<string, string>>[] array = filters;
				foreach (Func<IDictionary<string, string>, IDictionary<string, string>> func in array)
				{
					if (dictionary == null)
					{
						break;
					}
					dictionary = func(dictionary);
				}
				return dictionary;
			};
		}

		private static Func<IDictionary<string, string>, IDictionary<string, string>> MatchesOne(params Func<IDictionary<string, string>, IDictionary<string, string>>[] filters)
		{
			return delegate(IDictionary<string, string> settings)
			{
				IDictionary<string, string>[] array = (from filter in filters
				select filter(new Dictionary<string, string>(settings)) into result
				where result != null
				select result).Take(2).ToArray();
				if (array.Length != 1)
				{
					return null;
				}
				return array.First();
			};
		}

		private static Func<IDictionary<string, string>, IDictionary<string, string>> MatchesExactly(Func<IDictionary<string, string>, IDictionary<string, string>> filter)
		{
			return delegate(IDictionary<string, string> settings)
			{
				IDictionary<string, string> dictionary = filter(settings);
				if (dictionary == null || dictionary.Any())
				{
					return null;
				}
				return dictionary;
			};
		}

		private static bool MatchesSpecification(IDictionary<string, string> settings, params Func<IDictionary<string, string>, IDictionary<string, string>>[] constraints)
		{
			for (int i = 0; i < constraints.Length; i++)
			{
				IDictionary<string, string> dictionary = constraints[i](settings);
				if (dictionary == null)
				{
					return false;
				}
				settings = dictionary;
			}
			if (settings.Count == 0)
			{
				return true;
			}
			return false;
		}

		private static StorageCredentials GetCredentials(IDictionary<string, string> settings)
		{
			settings.TryGetValue("AccountName", out string value);
			settings.TryGetValue("AccountKey", out string value2);
			settings.TryGetValue("AccountKeyName", out string value3);
			settings.TryGetValue("SharedAccessSignature", out string value4);
			if (value != null && value2 != null && value4 == null)
			{
				return new StorageCredentials(value, value2, value3);
			}
			if (value2 == null && value3 == null && value4 != null)
			{
				return new StorageCredentials(value4);
			}
			return null;
		}

		private static StorageUri ConstructTableEndpoint(IDictionary<string, string> settings)
		{
			return ConstructTableEndpoint(settings["DefaultEndpointsProtocol"], settings["AccountName"], settings.ContainsKey("EndpointSuffix") ? settings["EndpointSuffix"] : null);
		}

		private static StorageUri ConstructTableEndpoint(string scheme, string accountName, string endpointSuffix)
		{
			if (string.IsNullOrEmpty(scheme))
			{
				throw new ArgumentNullException("scheme");
			}
			if (string.IsNullOrEmpty(accountName))
			{
				throw new ArgumentNullException("accountName");
			}
			if (string.IsNullOrEmpty(endpointSuffix))
			{
				endpointSuffix = "core.windows.net";
			}
			string uriString = string.Format(CultureInfo.InvariantCulture, "{0}://{1}.{2}.{3}/", scheme, accountName, "table", endpointSuffix);
			string uriString2 = string.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}.{3}.{4}", scheme, accountName, "-secondary", "table", endpointSuffix);
			return new StorageUri(new Uri(uriString), new Uri(uriString2));
		}
	}
}
