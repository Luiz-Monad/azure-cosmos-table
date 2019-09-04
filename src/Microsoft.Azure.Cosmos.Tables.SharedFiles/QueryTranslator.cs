using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Documents.Interop.Common.Schema.Edm;
using Microsoft.OData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Azure.Cosmos.Tables.SharedFiles
{
	internal static class QueryTranslator
	{
		private const string FromCollectionAlias = "entity";

		private const string UnconditionalQueryFormat = "select {0} from entity {1}";

		private const string ConditionalQueryFormat = "select {0} from entity where {1} {2}";

		private const string SelectAll = "*";

		internal static string GetSqlQuery(string selectList, string filterString, bool isLinqExpression, bool isTableQuery, IList<OrderByItem> orderByItems, string tombstoneKey = null, bool enableTimestampQuery = false)
		{
			string text = (tombstoneKey == null) ? null : ("not (is_defined(entity['" + tombstoneKey + "']['$v']))");
			if (string.IsNullOrEmpty(selectList) && string.IsNullOrEmpty(filterString))
			{
				throw new ArgumentException("All arguments cannot be null for query");
			}
			if (string.IsNullOrEmpty(selectList))
			{
				selectList = "*";
			}
			string text2 = OrderByClause(enableTimestampQuery, orderByItems);
			if (string.IsNullOrEmpty(filterString))
			{
				if (string.IsNullOrEmpty(tombstoneKey))
				{
					return string.Format(CultureInfo.InvariantCulture, "select {0} from entity {1}", selectList, text2);
				}
				return string.Format(CultureInfo.InvariantCulture, "select {0} from entity where {1} {2}", selectList, text, text2);
			}
			if (!isLinqExpression)
			{
				try
				{
					filterString = ODataFilterTranslator.ToSql(filterString, isTableQuery, enableTimestampQuery);
					if (!string.IsNullOrEmpty(tombstoneKey))
					{
						filterString = "(" + filterString + ") and " + text;
					}
				}
				catch (Exception ex)
				{
					if (ex is ODataException || ex is NotSupportedException || ex is NotImplementedException || ex is InvalidCastException || ex is ArgumentNullException)
					{
						throw new InvalidFilterException("Invalid filter", ex);
					}
					throw;
				}
			}
			return string.Format(CultureInfo.InvariantCulture, "select {0} from entity where {1} {2}", selectList, filterString, text2);
		}

		public static string GetSelectString(string selectList)
		{
			if (string.IsNullOrEmpty(selectList))
			{
				return selectList;
			}
			StringBuilder stringBuilder = new StringBuilder(selectList.Length);
			StringBuilder stringBuilder2 = new StringBuilder(selectList.Length);
			Dictionary<string, string> systemPropertiesMapping = EdmSchemaMapping.SystemPropertiesMapping;
			int i = 0;
			char c = ',';
			for (; i <= selectList.Length; i++)
			{
				if (i == selectList.Length || selectList[i] == c)
				{
					string text = stringBuilder2.ToString();
					stringBuilder2.Clear();
					stringBuilder.Append("entity").Append("[\"").Append(systemPropertiesMapping.ContainsKey(text) ? systemPropertiesMapping[text] : text)
						.Append("\"]");
					if (i != selectList.Length)
					{
						stringBuilder.Append(c);
					}
				}
				else
				{
					stringBuilder2.Append(selectList[i]);
				}
			}
			stringBuilder.Append(c).Append("entity").Append("[\"")
				.Append("_etag")
				.Append("\"]");
			return stringBuilder.ToString();
		}

		private static string OrderByClause(bool enableTimestampQuery, IList<OrderByItem> orderByItems = null)
		{
			if (orderByItems == null)
			{
				return "";
			}
			if (orderByItems.Count == 0)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder("order by");
			foreach (OrderByItem orderByItem in orderByItems)
			{
				stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, " {0} {1},", EntityTranslator.GetPropertyName(orderByItem.PropertyName, enableTimestampQuery), orderByItem.Order));
			}
			return stringBuilder.ToString(0, stringBuilder.Length - 1);
		}
	}
}
