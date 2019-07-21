using Microsoft.Azure.Documents.Interop.Common.Schema;
using Microsoft.Azure.Documents.Interop.Common.Schema.Edm;
using Microsoft.OData.UriParser;
using Microsoft.OData.UriParser.Aggregation;
using System;
using System.Globalization;
using System.Text;

namespace Microsoft.Azure.Cosmos.Tables.SharedFiles
{
	internal static class ODataFilterTranslator
	{
		private sealed class QueryTokenVisitor : ISyntacticTreeVisitor<bool>
		{
			private readonly StringBuilder stringBuilder;

			private readonly bool isTableQuery;

			private readonly bool enableTimestampQuery;

			private static readonly string tableEntityDotId = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", EdmSchemaMapping.EntityName, "id");

			public string SqlFilter => stringBuilder.ToString();

			public QueryTokenVisitor(bool isTableQuery, bool enableTimestampQuery)
			{
				stringBuilder = new StringBuilder();
				this.isTableQuery = isTableQuery;
				this.enableTimestampQuery = enableTimestampQuery;
			}

			public bool Visit(AllToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(AnyToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(BinaryOperatorToken tokenIn)
			{
				stringBuilder.Append('(');
				tokenIn.Left.Accept(this);
				stringBuilder.Append(' ');
				switch (tokenIn.OperatorKind)
				{
				case BinaryOperatorKind.Or:
					stringBuilder.Append("OR");
					break;
				case BinaryOperatorKind.And:
					stringBuilder.Append("AND");
					break;
				case BinaryOperatorKind.Equal:
					stringBuilder.Append("=");
					break;
				case BinaryOperatorKind.NotEqual:
					stringBuilder.Append("!=");
					break;
				case BinaryOperatorKind.GreaterThan:
					stringBuilder.Append(">");
					break;
				case BinaryOperatorKind.GreaterThanOrEqual:
					stringBuilder.Append(">=");
					break;
				case BinaryOperatorKind.LessThan:
					stringBuilder.Append("<");
					break;
				case BinaryOperatorKind.LessThanOrEqual:
					stringBuilder.Append("<=");
					break;
				case BinaryOperatorKind.Add:
					stringBuilder.Append("+");
					break;
				case BinaryOperatorKind.Subtract:
					stringBuilder.Append("-");
					break;
				case BinaryOperatorKind.Multiply:
					stringBuilder.Append("*");
					break;
				case BinaryOperatorKind.Divide:
					stringBuilder.Append("/");
					break;
				case BinaryOperatorKind.Modulo:
					stringBuilder.Append("%");
					break;
				default:
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.OperatorKind));
				}
				stringBuilder.Append(' ');
				EndPathToken endPathToken = tokenIn.Left as EndPathToken;
				if (endPathToken != null && endPathToken.Identifier.Equals("Timestamp") && enableTimestampQuery)
				{
					LiteralToken literalToken = tokenIn.Right as LiteralToken;
					stringBuilder.Append(SchemaUtil.GetTimestampEpochSecondString(literalToken.Value));
				}
				else
				{
					tokenIn.Right.Accept(this);
				}
				stringBuilder.Append(')');
				return true;
			}

			public bool Visit(InToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(DottedIdentifierToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(ExpandToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(ExpandTermToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(FunctionCallToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(LambdaToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(LiteralToken tokenIn)
			{
				stringBuilder.Append(SchemaUtil.ConvertEdmType(tokenIn.Value));
				return true;
			}

			public bool Visit(InnerPathToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(OrderByToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(EndPathToken tokenIn)
			{
				if (isTableQuery)
				{
					stringBuilder.Append(tableEntityDotId);
				}
				else
				{
					stringBuilder.Append(EntityTranslator.GetPropertyName(tokenIn.Identifier, enableTimestampQuery));
				}
				return true;
			}

			public bool Visit(CustomQueryOptionToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(RangeVariableToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(SelectToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(StarToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(UnaryOperatorToken tokenIn)
			{
				UnaryOperatorKind operatorKind = tokenIn.OperatorKind;
				if (operatorKind == UnaryOperatorKind.Not)
				{
					stringBuilder.Append("NOT");
					tokenIn.Operand.Accept(this);
					stringBuilder.Append(' ');
					return true;
				}
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.OperatorKind));
			}

			public bool Visit(FunctionParameterToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(AggregateToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(AggregateExpressionToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(EntitySetAggregateToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}

			public bool Visit(GroupByToken tokenIn)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "QueryToken of type '{0}' is not supported.", tokenIn.Kind));
			}
		}

		private const int ExpressionParserMaxDepth = 1000;

		public static string ToSql(string odataFilter, bool isTableQuery, bool enableTimestampQuery = false)
		{
			if (string.IsNullOrEmpty(odataFilter))
			{
				throw new ArgumentException("odataFilter");
			}
			QueryToken queryToken = new UriQueryExpressionParser(1000).ParseFilter(odataFilter);
			QueryTokenVisitor queryTokenVisitor = new QueryTokenVisitor(isTableQuery, enableTimestampQuery);
			queryToken.Accept(queryTokenVisitor);
			return queryTokenVisitor.SqlFilter;
		}
	}
}
