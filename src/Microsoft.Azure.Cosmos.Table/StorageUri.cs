using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class StorageUri : IEquatable<StorageUri>
	{
		private Uri primaryUri;

		private Uri secondaryUri;

		public Uri PrimaryUri
		{
			get
			{
				return primaryUri;
			}
			private set
			{
				AssertAbsoluteUri(value);
				primaryUri = value;
			}
		}

		public Uri SecondaryUri
		{
			get
			{
				return secondaryUri;
			}
			private set
			{
				AssertAbsoluteUri(value);
				secondaryUri = value;
			}
		}

		public StorageUri(Uri primaryUri)
			: this(primaryUri, null)
		{
		}

		public StorageUri(Uri primaryUri, Uri secondaryUri)
		{
			if (primaryUri != null && secondaryUri != null)
			{
				bool flag = CommonUtility.UsePathStyleAddressing(primaryUri);
				bool flag2 = CommonUtility.UsePathStyleAddressing(secondaryUri);
				if (!flag && !flag2)
				{
					if (primaryUri.PathAndQuery != secondaryUri.PathAndQuery)
					{
						throw new ArgumentException("Primary and secondary location URIs in a StorageUri must point to the same resource.", "secondaryUri");
					}
				}
				else
				{
					IEnumerable<string> first = primaryUri.Segments.Skip(flag ? 2 : 0);
					IEnumerable<string> second = secondaryUri.Segments.Skip(flag2 ? 2 : 0);
					if (!first.SequenceEqual(second) || primaryUri.Query != secondaryUri.Query)
					{
						throw new ArgumentException("Primary and secondary location URIs in a StorageUri must point to the same resource.", "secondaryUri");
					}
				}
			}
			PrimaryUri = primaryUri;
			SecondaryUri = secondaryUri;
		}

		public Uri GetUri(StorageLocation location)
		{
			switch (location)
			{
			case StorageLocation.Primary:
				return PrimaryUri;
			case StorageLocation.Secondary:
				return SecondaryUri;
			default:
				CommonUtility.ArgumentOutOfRange("location", location);
				return null;
			}
		}

		internal bool ValidateLocationMode(LocationMode mode)
		{
			switch (mode)
			{
			case LocationMode.PrimaryOnly:
				return PrimaryUri != null;
			case LocationMode.SecondaryOnly:
				return SecondaryUri != null;
			default:
				if (PrimaryUri != null)
				{
					return SecondaryUri != null;
				}
				return false;
			}
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "Primary = '{0}'; Secondary = '{1}'", PrimaryUri, SecondaryUri);
		}

		public override int GetHashCode()
		{
			int num = (PrimaryUri != null) ? PrimaryUri.GetHashCode() : 0;
			int num2 = (SecondaryUri != null) ? SecondaryUri.GetHashCode() : 0;
			return num ^ num2;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as StorageUri);
		}

		public bool Equals(StorageUri other)
		{
			if (other != null && PrimaryUri == other.PrimaryUri)
			{
				return SecondaryUri == other.SecondaryUri;
			}
			return false;
		}

		public static bool operator ==(StorageUri uri1, StorageUri uri2)
		{
			if ((object)uri1 == uri2)
			{
				return true;
			}
			return uri1?.Equals(uri2) ?? false;
		}

		public static bool operator !=(StorageUri uri1, StorageUri uri2)
		{
			return !(uri1 == uri2);
		}

		private static void AssertAbsoluteUri(Uri uri)
		{
			if (uri != null && !uri.IsAbsoluteUri)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Address '{0}' is a relative address. Only absolute addresses are permitted.", uri.ToString()), "uri");
			}
		}
	}
}
