using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	[DebuggerDisplay("ResourceSetExpression {Source}.{MemberExpression}")]
	internal class ResourceSetExpression : ResourceExpression
	{
		[DebuggerDisplay("{ToString()}")]
		internal class TransparentAccessors
		{
			internal readonly string Accessor;

			internal readonly Dictionary<string, Expression> SourceAccessors;

			internal TransparentAccessors(string acc, Dictionary<string, Expression> sourceAccesors)
			{
				Accessor = acc;
				SourceAccessors = sourceAccesors;
			}

			public override string ToString()
			{
				return "SourceAccessors=[" + string.Join(",", SourceAccessors.Keys.ToArray()) + "] ->* Accessor=" + Accessor;
			}
		}

		private readonly Type resourceType;

		private readonly Expression member;

		private Dictionary<PropertyInfo, ConstantExpression> keyFilter;

		private List<QueryOptionExpression> sequenceQueryOptions;

		private TransparentAccessors transparentScope;

		internal Expression MemberExpression => member;

		internal override Type ResourceType => resourceType;

		internal bool HasTransparentScope => transparentScope != null;

		internal TransparentAccessors TransparentScope
		{
			get
			{
				return transparentScope;
			}
			set
			{
				transparentScope = value;
			}
		}

		internal bool HasKeyPredicate => keyFilter != null;

		internal Dictionary<PropertyInfo, ConstantExpression> KeyPredicate
		{
			get
			{
				return keyFilter;
			}
			set
			{
				keyFilter = value;
			}
		}

		internal override bool IsSingleton => HasKeyPredicate;

		internal override bool HasQueryOptions
		{
			get
			{
				if (sequenceQueryOptions.Count <= 0 && ExpandPaths.Count <= 0 && CountOption != CountOption.InlineAll && CustomQueryOptions.Count <= 0)
				{
					return base.Projection != null;
				}
				return true;
			}
		}

		internal FilterQueryOptionExpression Filter => sequenceQueryOptions.OfType<FilterQueryOptionExpression>().SingleOrDefault();

		internal RequestOptionsQueryOptionExpression RequestOptions => sequenceQueryOptions.OfType<RequestOptionsQueryOptionExpression>().SingleOrDefault();

		internal TakeQueryOptionExpression Take => sequenceQueryOptions.OfType<TakeQueryOptionExpression>().SingleOrDefault();

		internal IEnumerable<QueryOptionExpression> SequenceQueryOptions => sequenceQueryOptions.ToList();

		internal bool HasSequenceQueryOptions => sequenceQueryOptions.Count > 0;

		internal ResourceSetExpression(Type type, Expression source, Expression memberExpression, Type resourceType, List<string> expandPaths, CountOption countOption, Dictionary<ConstantExpression, ConstantExpression> customQueryOptions, ProjectionQueryOptionExpression projection)
			: base(source, (source != null) ? ((ExpressionType)10001) : ((ExpressionType)10000), type, expandPaths, countOption, customQueryOptions, projection)
		{
			member = memberExpression;
			this.resourceType = resourceType;
			sequenceQueryOptions = new List<QueryOptionExpression>();
		}

		internal override ResourceExpression CreateCloneWithNewType(Type type)
		{
			return new ResourceSetExpression(type, base.Source, MemberExpression, TypeSystem.GetElementType(type), ExpandPaths.ToList(), CountOption, CustomQueryOptions.ToDictionary((KeyValuePair<ConstantExpression, ConstantExpression> kvp) => kvp.Key, (KeyValuePair<ConstantExpression, ConstantExpression> kvp) => kvp.Value), base.Projection)
			{
				keyFilter = keyFilter,
				sequenceQueryOptions = sequenceQueryOptions,
				transparentScope = transparentScope
			};
		}

		internal void AddSequenceQueryOption(QueryOptionExpression qoe)
		{
			QueryOptionExpression queryOptionExpression = (from o in sequenceQueryOptions
			where o.GetType() == qoe.GetType()
			select o).FirstOrDefault();
			if (queryOptionExpression != null)
			{
				qoe = qoe.ComposeMultipleSpecification(queryOptionExpression);
				sequenceQueryOptions.Remove(queryOptionExpression);
			}
			sequenceQueryOptions.Add(qoe);
		}

		internal void OverrideInputReference(ResourceSetExpression newInput)
		{
			InputReferenceExpression inputRef = newInput.inputRef;
			if (inputRef != null)
			{
				base.inputRef = inputRef;
				inputRef.OverrideTarget(this);
			}
		}
	}
}
