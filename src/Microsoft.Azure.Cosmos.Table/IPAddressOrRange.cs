using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Azure.Cosmos.Table
{
	public class IPAddressOrRange
	{
		public string Address
		{
			get;
			private set;
		}

		public string MinimumAddress
		{
			get;
			private set;
		}

		public string MaximumAddress
		{
			get;
			private set;
		}

		public bool IsSingleAddress
		{
			get;
			private set;
		}

		public IPAddressOrRange(string address)
		{
			CommonUtility.AssertNotNull("address", address);
			AssertIPv4(address);
			Address = address;
			IsSingleAddress = true;
		}

		public IPAddressOrRange(string minimum, string maximum)
		{
			CommonUtility.AssertNotNull("minimum", minimum);
			CommonUtility.AssertNotNull("maximum", maximum);
			AssertIPv4(minimum);
			AssertIPv4(maximum);
			MinimumAddress = minimum;
			MaximumAddress = maximum;
			IsSingleAddress = false;
		}

		public override string ToString()
		{
			if (IsSingleAddress)
			{
				return Address;
			}
			return MinimumAddress + "-" + MaximumAddress;
		}

		private static void AssertIPv4(string address)
		{
			if (!IPAddress.TryParse(address, out IPAddress address2))
			{
				throw new ArgumentException("Error when parsing IP address: IP address is invalid.");
			}
			if (address2.AddressFamily != AddressFamily.InterNetwork)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "When specifying an IP Address in a SAS token, it must be an IPv4 address. Input address was {0}.", new object[1]
				{
					address
				}));
			}
		}
	}
}
