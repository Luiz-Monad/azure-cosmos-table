using Microsoft.Azure.Cosmos.Table.RestExecutor.Common;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal static class RESTCommandGeneratorUtils
	{
		internal static StorageUri GenerateRequestURI(TableOperation operation, StorageUri uriList, string tableName)
		{
			return new StorageUri(GenerateRequestURI(operation, uriList.PrimaryUri, tableName), GenerateRequestURI(operation, uriList.SecondaryUri, tableName));
		}

		internal static Uri GenerateRequestURI(TableOperation operation, Uri uri, string tableName)
		{
			if (uri == null)
			{
				return null;
			}
			if (operation.OperationType == TableOperationType.Insert)
			{
				return NavigationHelper.AppendPathToSingleUri(uri, tableName + "()");
			}
			return NavigationHelper.AppendPathToSingleUri(uri, string.Format(arg1: (!operation.IsTableEntity) ? string.Format(CultureInfo.InvariantCulture, "{0}='{1}',{2}='{3}'", "PartitionKey", operation.PartitionKey.Replace("'", "''"), "RowKey", operation.RowKey.Replace("'", "''")) : string.Format(CultureInfo.InvariantCulture, "'{0}'", operation.Entity.WriteEntity(null)["TableName"].StringValue), provider: CultureInfo.InvariantCulture, format: "{0}({1})", arg0: tableName));
		}

		internal static HttpMethod ExtractHttpMethod(TableOperation operation)
		{
			switch (operation.OperationType)
			{
			case TableOperationType.Insert:
				return HttpMethod.Post;
			case TableOperationType.Merge:
			case TableOperationType.InsertOrMerge:
				return HttpMethod.Post;
			case TableOperationType.Replace:
			case TableOperationType.InsertOrReplace:
				return HttpMethod.Put;
			case TableOperationType.Delete:
				return HttpMethod.Delete;
			case TableOperationType.Retrieve:
				return HttpMethod.Get;
			default:
				throw new NotSupportedException();
			}
		}

		internal static UriQueryBuilder GenerateQueryBuilder(TableQuery query, bool? projectSystemProperties)
		{
			UriQueryBuilder uriQueryBuilder = new UriQueryBuilder();
			if (!string.IsNullOrEmpty(query.FilterString))
			{
				uriQueryBuilder.Add("$filter", query.FilterString);
			}
			if (query.TakeCount.HasValue)
			{
				uriQueryBuilder.Add("$top", Convert.ToString(query.TakeCount.Value, CultureInfo.InvariantCulture));
			}
			if (query.SelectColumns != null && query.SelectColumns.Count > 0)
			{
				StringBuilder stringBuilder = new StringBuilder();
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				for (int i = 0; i < query.SelectColumns.Count; i++)
				{
					if (query.SelectColumns[i] == "PartitionKey")
					{
						flag2 = true;
					}
					else if (query.SelectColumns[i] == "RowKey")
					{
						flag = true;
					}
					else if (query.SelectColumns[i] == "Timestamp")
					{
						flag3 = true;
					}
					stringBuilder.Append(query.SelectColumns[i]);
					if (i < query.SelectColumns.Count - 1)
					{
						stringBuilder.Append(",");
					}
				}
				if (projectSystemProperties.Value)
				{
					if (!flag2)
					{
						stringBuilder.Append(",");
						stringBuilder.Append("PartitionKey");
					}
					if (!flag)
					{
						stringBuilder.Append(",");
						stringBuilder.Append("RowKey");
					}
					if (!flag3)
					{
						stringBuilder.Append(",");
						stringBuilder.Append("Timestamp");
					}
				}
				uriQueryBuilder.Add("$select", stringBuilder.ToString());
			}
			return uriQueryBuilder;
		}

		internal static UriQueryBuilder GenerateQueryBuilder(TableOperation operation, bool? projectSystemProperties)
		{
			UriQueryBuilder uriQueryBuilder = new UriQueryBuilder();
			if (operation.SelectColumns != null && operation.SelectColumns.Count > 0)
			{
				StringBuilder stringBuilder = new StringBuilder();
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				for (int i = 0; i < operation.SelectColumns.Count; i++)
				{
					if (operation.SelectColumns[i] == "PartitionKey")
					{
						flag2 = true;
					}
					else if (operation.SelectColumns[i] == "RowKey")
					{
						flag = true;
					}
					else if (operation.SelectColumns[i] == "Timestamp")
					{
						flag3 = true;
					}
					stringBuilder.Append(operation.SelectColumns[i]);
					if (i < operation.SelectColumns.Count - 1)
					{
						stringBuilder.Append(",");
					}
				}
				if (projectSystemProperties.Value)
				{
					if (!flag2)
					{
						stringBuilder.Append(",");
						stringBuilder.Append("PartitionKey");
					}
					if (!flag)
					{
						stringBuilder.Append(",");
						stringBuilder.Append("RowKey");
					}
					if (!flag3)
					{
						stringBuilder.Append(",");
						stringBuilder.Append("Timestamp");
					}
				}
				uriQueryBuilder.Add("$select", stringBuilder.ToString());
			}
			return uriQueryBuilder;
		}

		internal static UriQueryBuilder GenerateQueryBuilder<TInput>(TableQuery<TInput> query, bool? projectSystemProperties)
		{
			UriQueryBuilder uriQueryBuilder = new UriQueryBuilder();
			if (!string.IsNullOrEmpty(query.FilterString))
			{
				uriQueryBuilder.Add("$filter", query.FilterString);
			}
			if (query.TakeCount.HasValue)
			{
				uriQueryBuilder.Add("$top", Convert.ToString(query.TakeCount.Value, CultureInfo.InvariantCulture));
			}
			if (query.SelectColumns != null && query.SelectColumns.Count > 0)
			{
				StringBuilder stringBuilder = new StringBuilder();
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				for (int i = 0; i < query.SelectColumns.Count; i++)
				{
					if (query.SelectColumns[i] == "PartitionKey")
					{
						flag2 = true;
					}
					else if (query.SelectColumns[i] == "RowKey")
					{
						flag = true;
					}
					else if (query.SelectColumns[i] == "Timestamp")
					{
						flag3 = true;
					}
					stringBuilder.Append(query.SelectColumns[i]);
					if (i < query.SelectColumns.Count - 1)
					{
						stringBuilder.Append(",");
					}
				}
				if (projectSystemProperties.Value)
				{
					if (!flag2)
					{
						stringBuilder.Append(",");
						stringBuilder.Append("PartitionKey");
					}
					if (!flag)
					{
						stringBuilder.Append(",");
						stringBuilder.Append("RowKey");
					}
					if (!flag3)
					{
						stringBuilder.Append(",");
						stringBuilder.Append("Timestamp");
					}
				}
				uriQueryBuilder.Add("$select", stringBuilder.ToString());
			}
			return uriQueryBuilder;
		}

		internal static void ApplyToUriQueryBuilder(TableContinuationToken token, UriQueryBuilder builder)
		{
			if (!string.IsNullOrEmpty(token.NextPartitionKey))
			{
				builder.Add("NextPartitionKey", token.NextPartitionKey);
			}
			if (!string.IsNullOrEmpty(token.NextRowKey))
			{
				builder.Add("NextRowKey", token.NextRowKey);
			}
			if (!string.IsNullOrEmpty(token.NextTableName))
			{
				builder.Add("NextTableName", token.NextTableName);
			}
		}

		internal static CommandLocationMode GetListingLocationMode(TableContinuationToken token)
		{
			if (token != null && token.TargetLocation.HasValue)
			{
				switch (token.TargetLocation.Value)
				{
				case StorageLocation.Primary:
					return CommandLocationMode.PrimaryOnly;
				case StorageLocation.Secondary:
					return CommandLocationMode.SecondaryOnly;
				}
				CommonUtility.ArgumentOutOfRange("TargetLocation", token.TargetLocation.Value);
			}
			return CommandLocationMode.PrimaryOrSecondary;
		}

		internal static void ApplyTableRequestOptionsToStorageCommand<T>(TableRequestOptions options, RESTCommand<T> cmd)
		{
			if (options.LocationMode.HasValue)
			{
				cmd.LocationMode = options.LocationMode.Value;
			}
			if (options.ServerTimeout.HasValue)
			{
				cmd.ServerTimeoutInSeconds = (int)options.ServerTimeout.Value.TotalSeconds;
			}
			if (options.OperationExpiryTime.HasValue)
			{
				cmd.OperationExpiryTime = options.OperationExpiryTime;
			}
			else if (options.MaximumExecutionTime.HasValue)
			{
				cmd.OperationExpiryTime = DateTime.Now + options.MaximumExecutionTime.Value;
			}
		}
	}
}
