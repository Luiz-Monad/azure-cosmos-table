using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Azure.Cosmos.Table
{
	internal static class CryptoUtility
	{
		internal static string ComputeHmac256(string keyValue, string message)
		{
			using (HashAlgorithm hashAlgorithm = new HMACSHA256(Convert.FromBase64String(keyValue)))
			{
				byte[] bytes = Encoding.UTF8.GetBytes(message);
				return Convert.ToBase64String(hashAlgorithm.ComputeHash(bytes));
			}
		}
	}
}
