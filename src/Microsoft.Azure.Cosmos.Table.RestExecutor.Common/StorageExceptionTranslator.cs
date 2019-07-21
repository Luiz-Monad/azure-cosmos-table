using Microsoft.Azure.Cosmos.Table.RestExecutor.Utils;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common
{
	internal static class StorageExceptionTranslator
	{
		public static async Task<StorageException> TranslateExceptionAsync(Exception ex, RequestResult reqResult, Func<Stream, CancellationToken, Task<StorageExtendedErrorInformation>> parseErrorAsync, CancellationToken cancellationToken, HttpResponseMessage response)
		{
			try
			{
				if (parseErrorAsync == null)
				{
					parseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadFromStreamAsync;
				}
				StorageException result;
				if ((result = CoreTranslateAsync(ex, reqResult, cancellationToken)) != null)
				{
					return result;
				}
				if (response != null)
				{
					PopulateRequestResult(reqResult, response);
					Func<Stream, CancellationToken, Task<StorageExtendedErrorInformation>> func = parseErrorAsync;
					Func<Stream, CancellationToken, Task<StorageExtendedErrorInformation>> func2 = func;
					reqResult.ExtendedErrorInformation = await func2(await response.Content.ReadAsStreamAsync().ConfigureAwait(continueOnCapturedContext: false), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (Exception)
			{
			}
			return new StorageException(reqResult, ex.Message, ex);
		}

		internal static async Task<StorageException> TranslateExceptionWithPreBufferedStreamAsync(Exception ex, RequestResult reqResult, Stream responseStream, HttpResponseMessage response, Func<Stream, HttpResponseMessage, string, CancellationToken, Task<StorageExtendedErrorInformation>> parseErrorAsync)
		{
			try
			{
				StorageException result;
				if ((result = CoreTranslateAsync(ex, reqResult, CancellationToken.None)) != null)
				{
					return result;
				}
				if (response != null)
				{
					PopulateRequestResult(reqResult, response);
					if (parseErrorAsync == null)
					{
						reqResult.ExtendedErrorInformation = await StorageExtendedErrorInformationRestHelper.ReadFromStreamAsync(responseStream, CancellationToken.None);
					}
					else
					{
						reqResult.ExtendedErrorInformation = parseErrorAsync(responseStream, response, response.Content.Headers.ContentType.ToString(), CancellationToken.None).Result;
					}
				}
			}
			catch (Exception)
			{
			}
			return new StorageException(reqResult, ex.Message, ex);
		}

		private static StorageException CoreTranslateAsync(Exception ex, RequestResult reqResult, CancellationToken token)
		{
			CommonUtility.AssertNotNull("reqResult", reqResult);
			CommonUtility.AssertNotNull("ex", ex);
			if (ex is StorageException)
			{
				return (StorageException)ex;
			}
			if (ex is TimeoutException)
			{
				reqResult.HttpStatusMessage = null;
				reqResult.HttpStatusCode = 408;
				reqResult.ExtendedErrorInformation = null;
				return new StorageException(reqResult, ex.Message, ex);
			}
			if (ex is ArgumentException)
			{
				reqResult.HttpStatusMessage = null;
				reqResult.HttpStatusCode = 306;
				reqResult.ExtendedErrorInformation = null;
				return new StorageException(reqResult, ex.Message, ex)
				{
					IsRetryable = false
				};
			}
			if (ex is OperationCanceledException)
			{
				reqResult.HttpStatusMessage = null;
				reqResult.HttpStatusCode = 306;
				reqResult.ExtendedErrorInformation = null;
				return new StorageException(reqResult, ex.Message, ex);
			}
			return null;
		}

		private static void PopulateRequestResult(RequestResult reqResult, HttpResponseMessage response)
		{
			reqResult.HttpStatusMessage = response.ReasonPhrase;
			reqResult.HttpStatusCode = (int)response.StatusCode;
			if (response.Headers != null)
			{
				reqResult.ServiceRequestID = response.Headers.GetHeaderSingleValueOrDefault("x-ms-request-id");
				string text2 = reqResult.RequestDate = (response.Headers.Date.HasValue ? response.Headers.Date.Value.UtcDateTime.ToString("R", CultureInfo.InvariantCulture) : null);
				reqResult.Etag = response.Headers.ETag?.ToString();
				reqResult.ErrorCode = response.Headers.GetHeaderSingleValueOrDefault("x-ms-error-code");
			}
			if (response.Content != null && response.Content.Headers != null)
			{
				reqResult.ContentMd5 = ((response.Content.Headers.ContentMD5 != null) ? Convert.ToBase64String(response.Content.Headers.ContentMD5) : null);
			}
		}

		internal static async Task<StorageException> PopulateStorageExceptionFromHttpResponseMessage(HttpResponseMessage response, RequestResult currentResult, CancellationToken token, Func<Stream, HttpResponseMessage, string, CancellationToken, Task<StorageExtendedErrorInformation>> parseErrorAsync)
		{
			if (response.IsSuccessStatusCode)
			{
				return null;
			}
			try
			{
				currentResult.HttpStatusCode = (int)response.StatusCode;
				currentResult.HttpStatusMessage = (currentResult.HttpStatusCode.Equals(400) ? "The remote server returned an error: (400) Bad Request." : response.ReasonPhrase);
				currentResult.ServiceRequestID = response.Headers.GetHeaderSingleValueOrDefault("x-ms-request-id");
				string headerSingleValueOrDefault = response.Headers.GetHeaderSingleValueOrDefault("x-ms-date");
				currentResult.RequestDate = (string.IsNullOrEmpty(headerSingleValueOrDefault) ? DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture) : headerSingleValueOrDefault);
				if (response.Headers.ETag != null)
				{
					currentResult.Etag = response.Headers.ETag.ToString();
				}
				if (response.Content != null && response.Content.Headers.ContentMD5 != null)
				{
					currentResult.ContentMd5 = Convert.ToBase64String(response.Content.Headers.ContentMD5);
				}
				currentResult.ErrorCode = response.Headers.GetHeaderSingleValueOrDefault("x-ms-error-code");
			}
			catch (Exception)
			{
			}
			try
			{
				Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(continueOnCapturedContext: false);
				if (parseErrorAsync != null)
				{
					currentResult.ExtendedErrorInformation = await parseErrorAsync(stream, response, response.Content.Headers.ContentType.ToString(), token).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					currentResult.ExtendedErrorInformation = await StorageExtendedErrorInformationRestHelper.ReadFromStreamAsync(stream, token).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (Exception)
			{
			}
			return new StorageException(currentResult, currentResult.HttpStatusMessage, null);
		}

		internal static StorageException GenerateTimeoutException(RequestResult res, Exception inner)
		{
			if (res != null)
			{
				res.HttpStatusCode = 408;
			}
			TimeoutException ex = new TimeoutException("The client could not finish the operation within specified timeout.", inner);
			return new StorageException(res, ex.Message, ex)
			{
				IsRetryable = false
			};
		}
	}
}
