using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table
{
	internal sealed class NullType
	{
		internal static readonly NullType Value = new NullType();

		internal static readonly Task<NullType> ValueTask = Task.FromResult(Value);

		private NullType()
		{
		}
	}
}
