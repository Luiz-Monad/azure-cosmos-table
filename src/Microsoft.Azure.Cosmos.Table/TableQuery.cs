using Microsoft.Azure.Cosmos.Table.Queryable;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table
{
	public class TableQuery
	{
		private int? takeCount;

		public int? TakeCount
		{
			get
			{
				return takeCount;
			}
			set
			{
				if (value.HasValue && value.Value <= 0)
				{
					throw new ArgumentException("Take count must be positive and greater than 0.");
				}
				takeCount = value;
			}
		}

		public string FilterString
		{
			get;
			set;
		}

		public IList<string> SelectColumns
		{
			get;
			set;
		}

		internal List<OrderByItem> OrderByEntities
		{
			get;
			set;
		} = new List<OrderByItem>();


		public TableQuery OrderBy(string propertyName)
		{
			ValidateOrderBy();
			OrderByEntities.Add(new OrderByItem(propertyName));
			return this;
		}

		public TableQuery OrderByDesc(string propertyName)
		{
			ValidateOrderBy();
			OrderByEntities.Add(new OrderByItem(propertyName, "desc"));
			return this;
		}

		private void ValidateOrderBy()
		{
			if (OrderByEntities.Count >= 1)
			{
				throw new NotSupportedException("Only single order by is supported");
			}
		}

		public static T Project<T>(T entity, params string[] columns)
		{
			return entity;
		}

		internal IEnumerable<DynamicTableEntity> Execute(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			return CommonUtility.LazyEnumerable(delegate(TableContinuationToken continuationToken)
			{
				TableQuerySegment<DynamicTableEntity> tableQuerySegment = ExecuteQuerySegmented(continuationToken, client, table, modifiedOptions, operationContext);
				return new ResultSegment<DynamicTableEntity>(tableQuerySegment.Results)
				{
					ContinuationToken = tableQuerySegment.ContinuationToken
				};
			}, takeCount.HasValue ? takeCount.Value : long.MaxValue);
		}

		internal IEnumerable<TResult> Execute<TResult>(CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			return CommonUtility.LazyEnumerable(delegate(TableContinuationToken continuationToken)
			{
				TableQuerySegment<TResult> tableQuerySegment = ExecuteQuerySegmented(continuationToken, client, table, resolver, modifiedOptions, operationContext);
				return new ResultSegment<TResult>(tableQuerySegment.Results)
				{
					ContinuationToken = tableQuerySegment.ContinuationToken
				};
			}, takeCount.HasValue ? takeCount.Value : long.MaxValue);
		}

		internal TableQuerySegment<DynamicTableEntity> ExecuteQuerySegmented(TableContinuationToken token, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			return client.Executor.ExecuteQuerySegmented<DynamicTableEntity>(this, token, client, table, null, requestOptions2, operationContext);
		}

		internal TableQuerySegment<TResult> ExecuteQuerySegmented<TResult>(TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			CommonUtility.AssertNotNull("resolver", resolver);
			TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			return client.Executor.ExecuteQuerySegmented(this, token, client, table, resolver, requestOptions2, operationContext);
		}

		internal Task<TableQuerySegment<DynamicTableEntity>> ExecuteQuerySegmentedAsync(TableContinuationToken token, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			return client.Executor.ExecuteQuerySegmentedAsync<DynamicTableEntity>(this, token, client, table, null, requestOptions2, operationContext, cancellationToken);
		}

		internal Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			return client.Executor.ExecuteQuerySegmentedAsync(this, token, client, table, resolver, requestOptions2, operationContext, cancellationToken);
		}

		public static string GenerateFilterCondition(string propertyName, string operation, string givenValue)
		{
			givenValue = (givenValue ?? string.Empty);
			return GenerateFilterCondition(propertyName, operation, givenValue, EdmType.String);
		}

		public static string GenerateFilterConditionForBool(string propertyName, string operation, bool givenValue)
		{
			return GenerateFilterCondition(propertyName, operation, givenValue ? "true" : "false", EdmType.Boolean);
		}

		public static string GenerateFilterConditionForBinary(string propertyName, string operation, byte[] givenValue)
		{
			CommonUtility.AssertNotNull("value", givenValue);
			StringBuilder stringBuilder = new StringBuilder();
			foreach (byte b in givenValue)
			{
				stringBuilder.AppendFormat("{0:x2}", b);
			}
			return GenerateFilterCondition(propertyName, operation, stringBuilder.ToString(), EdmType.Binary);
		}

		public static string GenerateFilterConditionForDate(string propertyName, string operation, DateTimeOffset givenValue)
		{
			return GenerateFilterCondition(propertyName, operation, givenValue.UtcDateTime.ToString("o", CultureInfo.InvariantCulture), EdmType.DateTime);
		}

		public static string GenerateFilterConditionForDouble(string propertyName, string operation, double givenValue)
		{
			return GenerateFilterCondition(propertyName, operation, Convert.ToString(givenValue, CultureInfo.InvariantCulture), EdmType.Double);
		}

		public static string GenerateFilterConditionForInt(string propertyName, string operation, int givenValue)
		{
			return GenerateFilterCondition(propertyName, operation, Convert.ToString(givenValue, CultureInfo.InvariantCulture), EdmType.Int32);
		}

		public static string GenerateFilterConditionForLong(string propertyName, string operation, long givenValue)
		{
			return GenerateFilterCondition(propertyName, operation, Convert.ToString(givenValue, CultureInfo.InvariantCulture), EdmType.Int64);
		}

		public static string GenerateFilterConditionForGuid(string propertyName, string operation, Guid givenValue)
		{
			CommonUtility.AssertNotNull("value", givenValue);
			return GenerateFilterCondition(propertyName, operation, givenValue.ToString(), EdmType.Guid);
		}

		private static string GenerateFilterCondition(string propertyName, string operation, string givenValue, EdmType edmType)
		{
			string text = null;
			switch (edmType)
			{
			case EdmType.Boolean:
			case EdmType.Int32:
				text = givenValue;
				break;
			case EdmType.Double:
				text = (int.TryParse(givenValue, out int _) ? string.Format(CultureInfo.InvariantCulture, "{0}.0", givenValue) : givenValue);
				break;
			case EdmType.Int64:
				text = string.Format(CultureInfo.InvariantCulture, "{0}L", givenValue);
				break;
			case EdmType.DateTime:
				text = string.Format(CultureInfo.InvariantCulture, "datetime'{0}'", givenValue);
				break;
			case EdmType.Guid:
				text = string.Format(CultureInfo.InvariantCulture, "guid'{0}'", givenValue);
				break;
			case EdmType.Binary:
				text = string.Format(CultureInfo.InvariantCulture, "X'{0}'", givenValue);
				break;
			default:
				text = string.Format(CultureInfo.InvariantCulture, "'{0}'", givenValue.Replace("'", "''"));
				break;
			}
			return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", propertyName, operation, text);
		}

		public static string CombineFilters(string filterA, string operatorString, string filterB)
		{
			return string.Format(CultureInfo.InvariantCulture, "({0}) {1} ({2})", filterA, operatorString, filterB);
		}

		public TableQuery Select(IList<string> columns)
		{
			SelectColumns = columns;
			return this;
		}

		public TableQuery Take(int? take)
		{
			TakeCount = take;
			return this;
		}

		public TableQuery Where(string filter)
		{
			FilterString = filter;
			return this;
		}

		public TableQuery Copy()
		{
			return new TableQuery
			{
				TakeCount = TakeCount,
				FilterString = FilterString,
				SelectColumns = SelectColumns,
				OrderByEntities = OrderByEntities
			};
		}
	}
	public class TableQuery<TElement> : IQueryable<TElement>, IEnumerable<TElement>, IEnumerable, IQueryable
	{
		internal class ExecutionInfo
		{
			public OperationContext OperationContext
			{
				get;
				set;
			}

			public TableRequestOptions RequestOptions
			{
				get;
				set;
			}

			public EntityResolver<TElement> Resolver
			{
				get;
				set;
			}
		}

		private readonly Expression queryExpression;

		private readonly TableQueryProvider queryProvider;

		private int? takeCount;

		public int? TakeCount
		{
			get
			{
				return takeCount;
			}
			set
			{
				if (value.HasValue && value.Value <= 0)
				{
					throw new ArgumentException("Take count must be positive and greater than 0.");
				}
				takeCount = value;
			}
		}

		public string FilterString
		{
			get;
			set;
		}

		public IList<string> SelectColumns
		{
			get;
			set;
		}

		public Type ElementType => typeof(TElement);

		public Expression Expression => queryExpression;

		public IQueryProvider Provider => queryProvider;

		internal List<OrderByItem> OrderByEntities
		{
			get;
		} = new List<OrderByItem>();


		public TableQuery()
		{
			if (typeof(TElement).GetTypeInfo().GetInterface(typeof(ITableEntity).FullName, ignoreCase: false) == null)
			{
				throw new NotSupportedException("TableQuery Generic Type must implement the ITableEntity Interface");
			}
			if (typeof(TElement).GetTypeInfo().GetConstructor(Type.EmptyTypes) == null)
			{
				throw new NotSupportedException("TableQuery Generic Type must provide a default parameterless constructor.");
			}
		}

		internal TableQuery(CloudTable table)
		{
			queryProvider = new TableQueryProvider(table);
			queryExpression = new ResourceSetExpression(typeof(IOrderedQueryable<TElement>), null, Expression.Constant("0"), typeof(TElement), null, CountOption.None, null, null);
		}

		internal TableQuery(Expression queryExpression, TableQueryProvider queryProvider)
		{
			this.queryProvider = queryProvider;
			this.queryExpression = queryExpression;
		}

		public TableQuery<TElement> Select(IList<string> columns)
		{
			if (Expression != null)
			{
				throw new NotSupportedException(TableResources.TableQueryFluentMethodNotAllowed);
			}
			SelectColumns = columns;
			return this;
		}

		public TableQuery<TElement> Take(int? take)
		{
			if (Expression != null)
			{
				throw new NotSupportedException(TableResources.TableQueryFluentMethodNotAllowed);
			}
			TakeCount = take;
			return this;
		}

		public TableQuery<TElement> Where(string filter)
		{
			if (Expression != null)
			{
				throw new NotSupportedException(TableResources.TableQueryFluentMethodNotAllowed);
			}
			FilterString = filter;
			return this;
		}

		public TableQuery<TElement> OrderBy(string propertyName)
		{
			ValidateOrderBy();
			OrderByEntities.Add(new OrderByItem(propertyName));
			return this;
		}

		public TableQuery<TElement> OrderByDesc(string propertyName)
		{
			ValidateOrderBy();
			OrderByEntities.Add(new OrderByItem(propertyName, "desc"));
			return this;
		}

		private void ValidateOrderBy()
		{
			if (OrderByEntities.Count >= 1)
			{
				throw new NotSupportedException("Only single order by is supported");
			}
		}

		public TableQuery<TElement> Copy()
		{
			return new TableQuery<TElement>
			{
				TakeCount = TakeCount,
				FilterString = FilterString,
				SelectColumns = SelectColumns
			};
		}

		public virtual IEnumerable<TElement> Execute(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			if (queryProvider == null)
			{
				throw new InvalidOperationException("Unknown Table. The TableQuery does not have an associated CloudTable Reference. Please execute the query via the CloudTable ExecuteQuery APIs.");
			}
			ExecutionInfo executionInfo = Bind();
			executionInfo.RequestOptions = (requestOptions ?? executionInfo.RequestOptions);
			executionInfo.OperationContext = (operationContext ?? executionInfo.OperationContext);
			if (executionInfo.Resolver != null)
			{
				return ExecuteInternal(queryProvider.Table.ServiceClient, queryProvider.Table, executionInfo.Resolver, executionInfo.RequestOptions, executionInfo.OperationContext);
			}
			return ExecuteInternal(queryProvider.Table.ServiceClient, queryProvider.Table, executionInfo.RequestOptions, executionInfo.OperationContext);
		}

		public virtual Task<TableQuerySegment<TElement>> ExecuteSegmentedAsync(TableContinuationToken currentToken)
		{
			return ExecuteSegmentedAsync(currentToken, CancellationToken.None);
		}

		public virtual Task<TableQuerySegment<TElement>> ExecuteSegmentedAsync(TableContinuationToken currentToken, CancellationToken cancellationToken)
		{
			return ExecuteSegmentedAsync(currentToken, null, null, cancellationToken);
		}

		public virtual Task<TableQuerySegment<TElement>> ExecuteSegmentedAsync(TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return ExecuteSegmentedAsync(currentToken, requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task<TableQuerySegment<TElement>> ExecuteSegmentedAsync(TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			if (queryProvider == null)
			{
				throw new InvalidOperationException("Unknown Table. The TableQuery does not have an associated CloudTable Reference. Please execute the query via the CloudTable ExecuteQuery APIs.");
			}
			ExecutionInfo executionInfo = Bind();
			executionInfo.RequestOptions = ((requestOptions == null) ? executionInfo.RequestOptions : requestOptions);
			executionInfo.OperationContext = ((operationContext == null) ? executionInfo.OperationContext : operationContext);
			if (executionInfo.Resolver != null)
			{
				return ExecuteQuerySegmentedInternalAsync(currentToken, queryProvider.Table.ServiceClient, queryProvider.Table, executionInfo.Resolver, executionInfo.RequestOptions, executionInfo.OperationContext, cancellationToken);
			}
			return ExecuteQuerySegmentedInternalAsync(currentToken, queryProvider.Table.ServiceClient, queryProvider.Table, executionInfo.RequestOptions, executionInfo.OperationContext, cancellationToken);
		}

		public virtual TableQuerySegment<TElement> ExecuteSegmented(TableContinuationToken continuationToken, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			if (queryProvider == null)
			{
				throw new InvalidOperationException("Unknown Table. The TableQuery does not have an associated CloudTable Reference. Please execute the query via the CloudTable ExecuteQuery APIs.");
			}
			ExecutionInfo executionInfo = Bind();
			executionInfo.RequestOptions = ((requestOptions == null) ? executionInfo.RequestOptions : requestOptions);
			executionInfo.OperationContext = ((operationContext == null) ? executionInfo.OperationContext : operationContext);
			if (executionInfo.Resolver != null)
			{
				return ExecuteQuerySegmentedInternal(continuationToken, queryProvider.Table.ServiceClient, queryProvider.Table, executionInfo.Resolver, executionInfo.RequestOptions, executionInfo.OperationContext);
			}
			return ExecuteQuerySegmentedInternal(continuationToken, queryProvider.Table.ServiceClient, queryProvider.Table, executionInfo.RequestOptions, executionInfo.OperationContext);
		}

		public virtual IEnumerator<TElement> GetEnumerator()
		{
			if (Expression == null)
			{
				TableRequestOptions requestOptions = TableRequestOptions.ApplyDefaults(null, queryProvider.Table.ServiceClient);
				return ExecuteInternal(queryProvider.Table.ServiceClient, queryProvider.Table, requestOptions, null).GetEnumerator();
			}
			ExecutionInfo executionInfo = Bind();
			if (executionInfo.Resolver != null)
			{
				return ExecuteInternal(queryProvider.Table.ServiceClient, queryProvider.Table, executionInfo.Resolver, executionInfo.RequestOptions, executionInfo.OperationContext).GetEnumerator();
			}
			return ExecuteInternal(queryProvider.Table.ServiceClient, queryProvider.Table, executionInfo.RequestOptions, executionInfo.OperationContext).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		internal ExecutionInfo Bind()
		{
			ExecutionInfo executionInfo = new ExecutionInfo();
			if (Expression != null)
			{
				Dictionary<Expression, Expression> rewrites = new Dictionary<Expression, Expression>(ReferenceEqualityComparer<Expression>.Instance);
				Expression e = ResourceBinder.Bind(ExpressionNormalizer.Normalize(Evaluator.PartialEval(Expression), rewrites));
				ExpressionParser parser = queryProvider.Table.ServiceClient.GetExpressionParser();
				parser.Translate(e);
				TakeCount = parser.TakeCount;
				FilterString = parser.FilterString;
				SelectColumns = parser.SelectColumns;
				executionInfo.RequestOptions = parser.RequestOptions;
				executionInfo.OperationContext = parser.OperationContext;
				if (parser.Resolver == null)
				{
					if (parser.Projection != null && parser.Projection.Selector != ProjectionQueryOptionExpression.DefaultLambda)
					{
						Type intermediateType = parser.Projection.Selector.Parameters[0].Type;
						ParameterExpression parameterExpression = Expression.Parameter(typeof(object));
						Func<object, TElement> projectorFunc = Expression.Lambda<Func<object, TElement>>(Expression.Invoke(parser.Projection.Selector, Expression.Convert(parameterExpression, intermediateType)), new ParameterExpression[1]
						{
							parameterExpression
						}).Compile();
						executionInfo.Resolver = delegate(string pk, string rk, DateTimeOffset ts, IDictionary<string, EntityProperty> props, string etag)
						{
							ITableEntity tableEntity = (ITableEntity)EntityUtilities.InstantiateEntityFromType(intermediateType);
							tableEntity.PartitionKey = pk;
							tableEntity.RowKey = rk;
							tableEntity.Timestamp = ts;
							tableEntity.ReadEntity(props, parser.OperationContext);
							tableEntity.ETag = etag;
							return projectorFunc(tableEntity);
						};
					}
				}
				else
				{
					executionInfo.Resolver = (EntityResolver<TElement>)parser.Resolver.Value;
				}
			}
			executionInfo.RequestOptions = TableRequestOptions.ApplyDefaults(executionInfo.RequestOptions, queryProvider.Table.ServiceClient);
			executionInfo.OperationContext = (executionInfo.OperationContext ?? new OperationContext());
			return executionInfo;
		}

		internal IEnumerable<TElement> ExecuteInternal(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			return CommonUtility.LazyEnumerable(delegate(TableContinuationToken continuationToken)
			{
				try
				{
					TableQuerySegment<TElement> tableQuerySegment = ExecuteQuerySegmentedInternal(continuationToken, client, table, modifiedOptions, operationContext);
					return new ResultSegment<TElement>(tableQuerySegment.Results)
					{
						ContinuationToken = tableQuerySegment.ContinuationToken
					};
				}
				catch (StorageException ex)
				{
					if (ex == null || ex.RequestInformation?.HttpStatusCode != 404)
					{
						throw;
					}
					return new ResultSegment<TElement>(new TableQuerySegment<TElement>(new List<TElement>()).Results);
				}
			}, TakeCount.HasValue ? TakeCount.Value : long.MaxValue);
		}

		internal IEnumerable<TResult> ExecuteInternal<TResult>(CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			CommonUtility.AssertNotNull("resolver", resolver);
			TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			return CommonUtility.LazyEnumerable(delegate(TableContinuationToken continuationToken)
			{
				TableQuerySegment<TResult> tableQuerySegment = ExecuteQuerySegmentedInternal(continuationToken, client, table, resolver, modifiedOptions, operationContext);
				return new ResultSegment<TResult>(tableQuerySegment.Results)
				{
					ContinuationToken = tableQuerySegment.ContinuationToken
				};
			}, takeCount.HasValue ? takeCount.Value : long.MaxValue);
		}

		internal TableQuerySegment<TElement> ExecuteQuerySegmentedInternal(TableContinuationToken token, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			try
			{
				CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
				TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, client);
				operationContext = (operationContext ?? new OperationContext());
				return client.Executor.ExecuteQuerySegmented<TElement, TElement>(this, token, client, table, null, requestOptions2, operationContext);
			}
			catch (StorageException ex)
			{
				if (ex == null || ex.RequestInformation?.HttpStatusCode != 404)
				{
					throw;
				}
				return new TableQuerySegment<TElement>(new TableQuerySegment<TElement>(new List<TElement>()).Results);
			}
		}

		internal TableQuerySegment<TResult> ExecuteQuerySegmentedInternal<TResult>(TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			CommonUtility.AssertNotNull("resolver", resolver);
			TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			return client.Executor.ExecuteQuerySegmented(this, token, client, table, resolver, requestOptions2, operationContext);
		}

		internal Task<TableQuerySegment<TElement>> ExecuteQuerySegmentedInternalAsync(TableContinuationToken token, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			return client.Executor.ExecuteQuerySegmentedAsync<TElement, TElement>(this, token, client, table, null, requestOptions2, operationContext, cancellationToken);
		}

		internal Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedInternalAsync<TResult>(TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			CommonUtility.AssertNotNull("resolver", resolver);
			TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			return client.Executor.ExecuteQuerySegmentedAsync(this, token, client, table, resolver, requestOptions2, operationContext, cancellationToken);
		}
	}
}
