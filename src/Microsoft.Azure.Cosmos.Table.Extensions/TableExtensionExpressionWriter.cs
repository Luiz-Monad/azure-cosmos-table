using Microsoft.Azure.Cosmos.Table.Queryable;
using Microsoft.Azure.Cosmos.Tables.SharedFiles;
using Microsoft.Azure.Documents.Interop.Common.Schema;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Extensions
{
	internal sealed class TableExtensionExpressionWriter : ExpressionWriter
	{
		internal new static string ExpressionToString(Expression e)
		{
			return new TableExtensionExpressionWriter().ConvertExpressionToString(e);
		}

		protected override string TranslateMemberName(string memberName)
		{
			return EntityTranslator.GetPropertyName(memberName);
		}

		internal override Expression VisitConstant(ConstantExpression c)
		{
			object value = SchemaUtil.ConvertEdmType(c.Value);
			builder.Append(value);
			return c;
		}

		protected override string TranslateOperator(ExpressionType type)
		{
			switch (type)
			{
			case ExpressionType.Equal:
				return "=";
			case ExpressionType.NotEqual:
				return "!=";
			case ExpressionType.LessThan:
				return "<";
			case ExpressionType.LessThanOrEqual:
				return "<=";
			case ExpressionType.GreaterThan:
				return ">";
			case ExpressionType.GreaterThanOrEqual:
				return ">=";
			case ExpressionType.Add:
			case ExpressionType.AddChecked:
				return "+";
			case ExpressionType.Subtract:
			case ExpressionType.SubtractChecked:
				return "-";
			case ExpressionType.Multiply:
			case ExpressionType.MultiplyChecked:
				return "*";
			case ExpressionType.Divide:
				return "/";
			case ExpressionType.Modulo:
				return "%";
			case ExpressionType.Negate:
			case ExpressionType.NegateChecked:
				return "!";
			default:
				return base.TranslateOperator(type);
			}
		}
	}
}
