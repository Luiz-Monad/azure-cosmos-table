using Microsoft.Azure.Cosmos.Table.RestExecutor.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal class RESTCommand<T>
	{
		public Stream ResponseStream
		{
			get;
			set;
		}

		public RequestResult CurrentResult
		{
			get;
			set;
		}

		public IList<RequestResult> RequestResults
		{
			get;
		}

		public LocationMode LocationMode
		{
			get;
			set;
		}

		public CommandLocationMode CommandLocationMode
		{
			get;
			set;
		}

		public StorageCredentials Credentials
		{
			get;
			set;
		}

		public StorageUri StorageUri
		{
			get;
			set;
		}

		public UriQueryBuilder Builder
		{
			get;
			set;
		}

		public int? ServerTimeoutInSeconds
		{
			get;
			set;
		}

		public DateTime? OperationExpiryTime
		{
			get;
			set;
		}

		public object OperationState
		{
			get;
			set;
		}

		public HttpClient HttpClient
		{
			get;
			set;
		}

		public Func<RESTCommand<T>, OperationContext, HttpContent> BuildContent
		{
			get;
			set;
		}

		public Func<RESTCommand<T>, Uri, UriQueryBuilder, HttpContent, int?, OperationContext, StorageRequestMessage> BuildRequest
		{
			get;
			set;
		}

		public Func<RESTCommand<T>, HttpResponseMessage, Exception, OperationContext, T> PreProcessResponse
		{
			get;
			set;
		}

		public Func<RESTCommand<T>, HttpResponseMessage, OperationContext, CancellationToken, Task<T>> PostProcessResponseAsync
		{
			get;
			set;
		}

		public Action<RESTCommand<T>, Exception, OperationContext> RecoveryAction
		{
			get;
			set;
		}

		public Func<Stream, HttpResponseMessage, string, CancellationToken, Task<StorageExtendedErrorInformation>> ParseErrorAsync
		{
			get;
			set;
		}

		public RESTCommand(StorageCredentials credentials, StorageUri storageUri)
			: this(credentials, storageUri, (UriQueryBuilder)null)
		{
		}

		public RESTCommand(StorageCredentials credentials, StorageUri storageUri, UriQueryBuilder builder)
		{
			Credentials = credentials;
			StorageUri = storageUri;
			Builder = builder;
			RequestResults = new List<RequestResult>();
		}
	}
}
