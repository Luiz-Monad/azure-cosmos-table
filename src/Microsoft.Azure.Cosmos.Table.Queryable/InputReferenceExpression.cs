using System.Diagnostics;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	[DebuggerDisplay("InputReferenceExpression -> {Type}")]
	internal sealed class InputReferenceExpression : Expression
	{
		private ResourceExpression target;

		internal ResourceExpression Target => target;

        #pragma warning disable 612, 618
		internal InputReferenceExpression(ResourceExpression target)
			: base((ExpressionType)10007, target.ResourceType)
		{
			this.target = target;
		}
        #pragma warning restore 612, 618

		internal void OverrideTarget(ResourceSetExpression newTarget)
		{
			target = newTarget;
		}
	}
}
