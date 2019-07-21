using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal abstract class ResourceExpression : Expression
	{
		protected InputReferenceExpression inputRef;

		private List<string> expandPaths;

		private CountOption countOption;

		private Dictionary<ConstantExpression, ConstantExpression> customQueryOptions;

		private ProjectionQueryOptionExpression projection;

		internal abstract bool HasQueryOptions
		{
			get;
		}

		internal abstract Type ResourceType
		{
			get;
		}

		internal abstract bool IsSingleton
		{
			get;
		}

		internal virtual List<string> ExpandPaths
		{
			get
			{
				return expandPaths;
			}
			set
			{
				expandPaths = value;
			}
		}

		internal virtual CountOption CountOption
		{
			get
			{
				return countOption;
			}
			set
			{
				countOption = value;
			}
		}

		internal virtual Dictionary<ConstantExpression, ConstantExpression> CustomQueryOptions
		{
			get
			{
				return customQueryOptions;
			}
			set
			{
				customQueryOptions = value;
			}
		}

		internal ProjectionQueryOptionExpression Projection
		{
			get
			{
				return projection;
			}
			set
			{
				projection = value;
			}
		}

		internal Expression Source
		{
			get;
			private set;
		}

        #pragma warning disable 612, 618
		internal ResourceExpression(Expression source, ExpressionType nodeType, Type type, List<string> expandPaths, CountOption countOption, Dictionary<ConstantExpression, ConstantExpression> customQueryOptions, ProjectionQueryOptionExpression projection)
			: base(nodeType, type)
		{
			this.expandPaths = (expandPaths ?? new List<string>());
			this.countOption = countOption;
			this.customQueryOptions = (customQueryOptions ?? new Dictionary<ConstantExpression, ConstantExpression>(ReferenceEqualityComparer<ConstantExpression>.Instance));
			this.projection = projection;
			Source = source;
		}
        #pragma warning restore 612, 618

		internal abstract ResourceExpression CreateCloneWithNewType(Type type);

		internal InputReferenceExpression CreateReference()
		{
			if (inputRef == null)
			{
				inputRef = new InputReferenceExpression(this);
			}
			return inputRef;
		}
	}
}
