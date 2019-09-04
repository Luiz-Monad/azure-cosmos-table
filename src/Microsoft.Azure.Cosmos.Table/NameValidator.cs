using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Cosmos.Table
{
	public static class NameValidator
	{
		private static readonly string[] ReservedFileNames = new string[25]
		{
			".",
			"..",
			"LPT1",
			"LPT2",
			"LPT3",
			"LPT4",
			"LPT5",
			"LPT6",
			"LPT7",
			"LPT8",
			"LPT9",
			"COM1",
			"COM2",
			"COM3",
			"COM4",
			"COM5",
			"COM6",
			"COM7",
			"COM8",
			"COM9",
			"PRN",
			"AUX",
			"NUL",
			"CON",
			"CLOCK$"
		};

		private static readonly RegexOptions RegexOptions = RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.CultureInvariant;

		private static readonly Regex TableRegex = new Regex("^[A-Za-z][A-Za-z0-9]*$", RegexOptions);

		private static readonly Regex MetricsTableRegex = new Regex("^\\$Metrics(HourPrimary|MinutePrimary|HourSecondary|MinuteSecondary)?(Transactions)(Blob|Queue|Table)$", RegexOptions);

		public static void ValidateTableName(string tableName)
		{
			if (string.IsNullOrWhiteSpace(tableName))
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid {0} name. The {0} name may not be null, empty, or whitespace only.", new object[1]
				{
					"table"
				}));
			}
			if (tableName.Length < 3 || tableName.Length > 63)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid {0} name length. The {0} name must be between {1} and {2} characters long.", "table", 3, 63));
			}
			if (!TableRegex.IsMatch(tableName) && !MetricsTableRegex.IsMatch(tableName) && !tableName.Equals("$MetricsCapacityBlob", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid {0} name. Check MSDN for more information about valid {0} naming.", new object[1]
				{
					"table"
				}));
			}
		}
	}
}
