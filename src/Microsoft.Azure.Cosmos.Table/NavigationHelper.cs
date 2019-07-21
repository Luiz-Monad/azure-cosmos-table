using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Cosmos.Table
{
	internal static class NavigationHelper
	{
		public static readonly char[] SlashAsSplitOptions = "/".ToCharArray();

		public const string Slash = "/";

		internal static StorageUri AppendPathToUri(StorageUri uriList, string relativeUri)
		{
			return AppendPathToUri(uriList, relativeUri, "/");
		}

		internal static StorageUri AppendPathToUri(StorageUri uriList, string relativeUri, string sep)
		{
			return new StorageUri(AppendPathToSingleUri(uriList.PrimaryUri, relativeUri, sep), AppendPathToSingleUri(uriList.SecondaryUri, relativeUri, sep));
		}

		internal static Uri AppendPathToSingleUri(Uri uri, string relativeUri)
		{
			return AppendPathToSingleUri(uri, relativeUri, "/");
		}

		internal static Uri AppendPathToSingleUri(Uri uri, string relativeUri, string sep)
		{
			if (uri == null || relativeUri.Length == 0)
			{
				return uri;
			}
			sep = Uri.EscapeUriString(sep);
			relativeUri = Uri.EscapeUriString(relativeUri);
			UriBuilder uriBuilder = new UriBuilder(uri);
			string text = null;
			text = ((!uriBuilder.Path.EndsWith(sep, StringComparison.Ordinal)) ? (sep + relativeUri) : relativeUri);
			uriBuilder.Path += text;
			return uriBuilder.Uri;
		}

		internal static StorageUri ParseTableQueryAndVerify(StorageUri address, out StorageCredentials parsedCredentials)
		{
			StorageCredentials parsedCredentials2;
			return new StorageUri(ParseTableQueryAndVerify(address.PrimaryUri, out parsedCredentials), ParseTableQueryAndVerify(address.SecondaryUri, out parsedCredentials2));
		}

		private static Uri ParseTableQueryAndVerify(Uri address, out StorageCredentials parsedCredentials)
		{
			parsedCredentials = null;
			if (address == null)
			{
				return null;
			}
			if (!address.IsAbsoluteUri)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Address '{0}' is a relative address. Only absolute addresses are permitted.", new object[1]
				{
					address.ToString()
				}), "address");
			}
			IDictionary<string, string> queryParameters = HttpWebUtility.ParseQueryString(address.Query);
			parsedCredentials = SharedAccessSignatureHelper.ParseQuery(queryParameters);
			return new Uri(address.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.UriEscaped));
		}

		internal static StorageUri GetServiceClientBaseAddress(StorageUri addressUri, bool? usePathStyleUris)
		{
			return new StorageUri(GetServiceClientBaseAddress(addressUri.PrimaryUri, usePathStyleUris), GetServiceClientBaseAddress(addressUri.SecondaryUri, usePathStyleUris));
		}

		internal static Uri GetServiceClientBaseAddress(Uri addressUri, bool? usePathStyleUris)
		{
			if (addressUri == null)
			{
				return null;
			}
			if (!usePathStyleUris.HasValue)
			{
				usePathStyleUris = CommonUtility.UsePathStyleAddressing(addressUri);
			}
			Uri uri = new Uri(addressUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped));
			if (!usePathStyleUris.Value)
			{
				return uri;
			}
			string[] segments = addressUri.Segments;
			if (segments.Length < 2)
			{
				throw new ArgumentException("address", string.Format(CultureInfo.CurrentCulture, "Missing account name information inside path style uri. Path style uris should be of the form http://<IPAddressPlusPort>/<accountName>"));
			}
			return new Uri(uri, segments[1]);
		}

		internal static string GetTableNameFromUri(Uri uri, bool? usePathStyleUris)
		{
			return GetContainerNameFromContainerAddress(uri, usePathStyleUris);
		}

		internal static string GetContainerNameFromContainerAddress(Uri uri, bool? usePathStyleUris)
		{
			if (!usePathStyleUris.HasValue)
			{
				usePathStyleUris = CommonUtility.UsePathStyleAddressing(uri);
			}
			if (!usePathStyleUris.Value)
			{
				return uri.AbsolutePath.Substring(1);
			}
			string[] array = uri.AbsolutePath.Split(SlashAsSplitOptions);
			if (array.Length < 3)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Cannot find account information inside Uri '{0}'", new object[1]
				{
					uri.AbsoluteUri
				}));
			}
			return array[2];
		}

		internal static string GetAccountNameFromUri(Uri clientUri, bool? usePathStyleUris)
		{
			if (!usePathStyleUris.HasValue)
			{
				usePathStyleUris = CommonUtility.UsePathStyleAddressing(clientUri);
			}
			string[] array = clientUri.AbsoluteUri.Split(SlashAsSplitOptions, StringSplitOptions.RemoveEmptyEntries);
			if (usePathStyleUris.Value)
			{
				if (array.Length < 3)
				{
					throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Cannot find account information inside Uri '{0}'", new object[1]
					{
						clientUri.AbsoluteUri
					}));
				}
				return array[2];
			}
			if (array.Length < 2)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Cannot find account information inside Uri '{0}'", new object[1]
				{
					clientUri.AbsoluteUri
				}));
			}
			int length = array[1].IndexOf(".", StringComparison.Ordinal);
			return array[1].Substring(0, length);
		}
	}
}
