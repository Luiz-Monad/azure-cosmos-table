using Microsoft.Azure.Cosmos.Table.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Cosmos.Table
{
	internal class TableEntityValidationHelper
	{
		private const string ValidPropertyNameFirstCharacterRegex = "_|[\\p{Lu}\\p{Ll}\\p{Lt}\\p{Lm}\\p{Lo}\\p{Nl}]";

		private const string ValidPropertyNameRemainingCharacterRegex = "[\\p{Lu}\\p{Ll}\\p{Lt}\\p{Lm}\\p{Lo}\\p{Nl}\\p{Mn}\\p{Mc}\\p{Nd}\\p{Pc}\\p{Cf}-]";

		private static Regex ValidPropertyNameRegex = new Regex("^(_|[\\p{Lu}\\p{Ll}\\p{Lt}\\p{Lm}\\p{Lo}\\p{Nl}])([\\p{Lu}\\p{Ll}\\p{Lt}\\p{Lm}\\p{Lo}\\p{Nl}\\p{Mn}\\p{Mc}\\p{Nd}\\p{Pc}\\p{Cf}-])*$", RegexOptions.Compiled);

		private static readonly List<string> UnsupportedPropertyNames = new List<string>
		{
			"id",
			"ttl"
		};

		private static Regex PartitionKeyRegex = new Regex("^[^\\\\/#?\\u0000-\\u001F\\u007F-\\u009F]+$", RegexOptions.Compiled);

		private static Regex RowKeyRegex = new Regex("^[^\\\\/#?\\u0000-\\u001F\\u007F-\\u009F]*[^ \\\\/#?\\u0000-\\u001F\\u007F-\\u009F]+$", RegexOptions.Compiled);

		public static bool IsValidPropertyName(string propertyName)
		{
			return ValidPropertyNameRegex.IsMatch(propertyName);
		}

		public static bool IsUnsupportedPropertyName(string propertyName)
		{
			return UnsupportedPropertyNames.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
		}

		public static void ValidatePartitionKey(string partitionKey)
		{
			if (string.IsNullOrWhiteSpace(partitionKey))
			{
				throw new TableInvalidInputException(TableErrorCodeStrings.PropertiesNeedValue, TableResources.PartitionKeyOrRowKeyEmpty);
			}
			if (partitionKey.Length > 1024)
			{
				string errorMessage = string.Format(CultureInfo.InvariantCulture, TableResources.PartitionKeyTooLarge, 1024);
				throw new TableInvalidInputException(TableErrorCodeStrings.KeyValueTooLarge, errorMessage);
			}
			if (!PartitionKeyRegex.IsMatch(partitionKey))
			{
				string errorMessage2 = string.Format(CultureInfo.InvariantCulture, TableResources.OutOfRangeInput, "PartitionKey", partitionKey);
				throw new TableInvalidInputException(TableErrorCodeStrings.OutOfRangeInput, errorMessage2);
			}
		}

		public static void ValidateRowKey(string rowKey)
		{
			if (string.IsNullOrWhiteSpace(rowKey))
			{
				throw new TableInvalidInputException(TableErrorCodeStrings.PropertiesNeedValue, TableResources.PartitionKeyOrRowKeyEmpty);
			}
			if (rowKey.Length > 254)
			{
				string errorMessage = string.Format(CultureInfo.InvariantCulture, TableResources.RowKeyTooLarge, 254);
				throw new TableInvalidInputException(TableErrorCodeStrings.KeyValueTooLarge, errorMessage);
			}
			if (!RowKeyRegex.IsMatch(rowKey))
			{
				string errorMessage2 = string.Format(CultureInfo.InvariantCulture, TableResources.OutOfRangeInput, "RowKey", rowKey);
				throw new TableInvalidInputException(TableErrorCodeStrings.OutOfRangeInput, errorMessage2);
			}
		}
	}
}
