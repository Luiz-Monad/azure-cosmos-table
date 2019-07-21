using System;
using Microsoft.Azure.Documents;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class TableRequestOptions
	{
		public static readonly TimeSpan MaxMaximumExecutionTime = TimeSpan.FromDays(24.0);

		private TablePayloadFormat? payloadFormat;

		private TimeSpan? maximumExecutionTime;

		internal static TableRequestOptions BaseDefaultRequestOptions = new TableRequestOptions
		{
			RetryPolicy = new NoRetry(),
			ServerTimeout = null,
			MaximumExecutionTime = null,
			PayloadFormat = TablePayloadFormat.Json,
			PropertyResolver = null,
			ProjectSystemProperties = true,
			TableQueryMaxItemCount = 1000,
			TableQueryMaxDegreeOfParallelism = -1,
			TableQueryEnableScan = false,
			TableQueryContinuationTokenLimitInKb = null
		};

		internal DateTime? OperationExpiryTime
		{
			get;
			set;
		}

		public IRetryPolicy RetryPolicy
		{
			get;
			set;
		}

		public bool? ProjectSystemProperties
		{
			get;
			set;
		}

		public LocationMode? LocationMode
		{
			get;
			set;
		}

		public TimeSpan? ServerTimeout
		{
			get;
			set;
		}

		public TimeSpan? MaximumExecutionTime
		{
			get
			{
				return maximumExecutionTime;
			}
			set
			{
				if (value.HasValue)
				{
					CommonUtility.AssertInBounds("MaximumExecutionTime", value.Value, TimeSpan.Zero, MaxMaximumExecutionTime);
				}
				maximumExecutionTime = value;
			}
		}

		public TablePayloadFormat? PayloadFormat
		{
			get
			{
				return payloadFormat;
			}
			set
			{
				if (value.HasValue)
				{
					payloadFormat = value.Value;
				}
			}
		}

		public Func<string, string, string, string, EdmType> PropertyResolver
		{
			get;
			set;
		}

		public string SessionToken
		{
			get;
			set;
		}

		public int? TableQueryMaxItemCount
		{
			get;
			set;
		}

		public bool? TableQueryEnableScan
		{
			get;
			set;
		}

		public int? TableQueryMaxDegreeOfParallelism
		{
			get;
			set;
		}

		public int? TableQueryContinuationTokenLimitInKb
		{
			get;
			set;
		}

		public ConsistencyLevel? ConsistencyLevel
		{
			get;
			set;
		}

		public TableRequestOptions()
		{
		}

		public TableRequestOptions(TableRequestOptions other)
		{
			if (other != null)
			{
				ServerTimeout = other.ServerTimeout;
				RetryPolicy = other.RetryPolicy;
				MaximumExecutionTime = other.MaximumExecutionTime;
				OperationExpiryTime = other.OperationExpiryTime;
				PayloadFormat = other.PayloadFormat;
				PropertyResolver = other.PropertyResolver;
				ProjectSystemProperties = other.ProjectSystemProperties;
				SessionToken = other.SessionToken;
				TableQueryMaxItemCount = other.TableQueryMaxItemCount;
				TableQueryEnableScan = other.TableQueryEnableScan;
				TableQueryMaxDegreeOfParallelism = other.TableQueryMaxDegreeOfParallelism;
				TableQueryContinuationTokenLimitInKb = other.TableQueryContinuationTokenLimitInKb;
				ConsistencyLevel = other.ConsistencyLevel;
			}
		}

		internal static TableRequestOptions ApplyDefaults(TableRequestOptions requestOptions, CloudTableClient serviceClient)
		{
			TableRequestOptions tableRequestOptions = new TableRequestOptions(requestOptions);
			if (serviceClient.IsPremiumEndpoint())
			{
				tableRequestOptions.TableQueryMaxItemCount = (tableRequestOptions.TableQueryMaxItemCount ?? serviceClient.DefaultRequestOptions.TableQueryMaxItemCount ?? BaseDefaultRequestOptions.TableQueryMaxItemCount);
				tableRequestOptions.TableQueryMaxDegreeOfParallelism = (tableRequestOptions.TableQueryMaxDegreeOfParallelism ?? serviceClient.DefaultRequestOptions.TableQueryMaxDegreeOfParallelism ?? BaseDefaultRequestOptions.TableQueryMaxDegreeOfParallelism);
				tableRequestOptions.TableQueryEnableScan = (tableRequestOptions.TableQueryEnableScan ?? serviceClient.DefaultRequestOptions.TableQueryEnableScan ?? BaseDefaultRequestOptions.TableQueryEnableScan);
				tableRequestOptions.TableQueryContinuationTokenLimitInKb = (tableRequestOptions.TableQueryContinuationTokenLimitInKb ?? serviceClient.DefaultRequestOptions.TableQueryContinuationTokenLimitInKb ?? BaseDefaultRequestOptions.TableQueryContinuationTokenLimitInKb);
				tableRequestOptions.ConsistencyLevel = (tableRequestOptions.ConsistencyLevel ?? serviceClient.DefaultRequestOptions.ConsistencyLevel ?? BaseDefaultRequestOptions.ConsistencyLevel);
			}
			else
			{
				tableRequestOptions.LocationMode = (tableRequestOptions.LocationMode ?? serviceClient.DefaultRequestOptions.LocationMode ?? BaseDefaultRequestOptions.LocationMode);
			}
			tableRequestOptions.RetryPolicy = (tableRequestOptions.RetryPolicy ?? serviceClient.DefaultRequestOptions.RetryPolicy ?? BaseDefaultRequestOptions.RetryPolicy);
			tableRequestOptions.ServerTimeout = (tableRequestOptions.ServerTimeout ?? serviceClient.DefaultRequestOptions.ServerTimeout ?? BaseDefaultRequestOptions.ServerTimeout);
			tableRequestOptions.MaximumExecutionTime = (tableRequestOptions.MaximumExecutionTime ?? serviceClient.DefaultRequestOptions.MaximumExecutionTime ?? BaseDefaultRequestOptions.MaximumExecutionTime);
			tableRequestOptions.PayloadFormat = (tableRequestOptions.PayloadFormat ?? serviceClient.DefaultRequestOptions.PayloadFormat ?? BaseDefaultRequestOptions.PayloadFormat);
			if (!tableRequestOptions.OperationExpiryTime.HasValue && tableRequestOptions.MaximumExecutionTime.HasValue)
			{
				tableRequestOptions.OperationExpiryTime = DateTime.Now + tableRequestOptions.MaximumExecutionTime.Value;
			}
			tableRequestOptions.PropertyResolver = (tableRequestOptions.PropertyResolver ?? serviceClient.DefaultRequestOptions.PropertyResolver ?? BaseDefaultRequestOptions.PropertyResolver);
			tableRequestOptions.ProjectSystemProperties = (tableRequestOptions.ProjectSystemProperties ?? serviceClient.DefaultRequestOptions.ProjectSystemProperties ?? BaseDefaultRequestOptions.ProjectSystemProperties);
			return tableRequestOptions;
		}
	}
}
