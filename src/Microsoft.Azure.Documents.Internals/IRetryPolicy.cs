using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Internals
{
	internal interface IRetryPolicy
	{
		/// <summary>
		/// Method that is called to determine from the policy that needs to retry on the exception
		/// </summary>
		/// <param name="exception">Exception during the callback method invocation</param>
		/// <param name="cancellationToken"></param>
		/// <returns>If the retry needs to be attempted or not</returns>
		Task<ShouldRetryResult> ShouldRetryAsync(Exception exception, CancellationToken cancellationToken);
	}
	internal interface IRetryPolicy<TPolicyArg1>
	{
		/// <summary>
		/// Initial value of the template argument
		/// </summary>
		TPolicyArg1 InitialArgumentValue
		{
			get;
		}

		/// <summary>
		/// Method that is called to determine from the policy that needs to retry on the exception
		/// </summary>
		/// <param name="exception">Exception during the callback method invocation</param>
		/// <param name="cancellationToken"></param>
		/// <returns>If the retry needs to be attempted or not</returns>
		Task<ShouldRetryResult<TPolicyArg1>> ShouldRetryAsync(Exception exception, CancellationToken cancellationToken);
	}
}
