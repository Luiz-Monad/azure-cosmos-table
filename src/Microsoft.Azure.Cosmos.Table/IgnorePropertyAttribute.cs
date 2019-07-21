using System;

namespace Microsoft.Azure.Cosmos.Table
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public sealed class IgnorePropertyAttribute : Attribute
	{
	}
}
