using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Divar_UWP.Infrastructure
{
    public sealed class DivarApiClient : IDivarApiClient
    {
        private static readonly HttpClient SharedHttpClient = CreateHttpClient();
        private static readonly BoundedMemoryCache<DivarApiResponse> PublicGetCache = new BoundedMemoryCache<DivarApiResponse>(24);
        private readonly IDivarCredentialProvider _credentialProvider;

        public DivarApiClient(IDivarCredentialProvider credentialProvider)
        {
            if (credentialProvider == null)
            {
                throw new ArgumentNullException(nameof(credentialProvider));
            }

            _credentialProvider = credentialProvider;
        }

        public Task<DivarApiResponse> GetAsync(
            string relativePath,
            CancellationToken cancellationToken,
            bool authenticated = false)
        {
            DivarApiResponse cached;
            if (!authenticated && IsCacheableGet(relativePath) && PublicGetCache.TryGet(relativePath, out cached))
            {
                DivarDiagnostics.Api("GET", relativePath, 0, cached.StatusCode.HasValue ? (int?)cached.StatusCode.Value : null, true);
                return Task.FromResult(cached);
            }
            return GetWithRetryAsync(relativePath, cancellationToken, authenticated);
        }

        private async Task<DivarApiResponse> GetWithRetryAsync(string relativePath, CancellationToken cancellationToken, bool authenticated)
        {
            var response = await SendAsync(HttpMethod.Get, relativePath, null, cancellationToken, authenticated);
            if (!IsTransient(response) || cancellationToken.IsCancellationRequested) return response;
            await Task.Delay(350, cancellationToken);
            return await SendAsync(HttpMethod.Get, relativePath, null, cancellationToken, authenticated);
        }

        public Task<DivarApiResponse> PostJsonAsync(
            string relativePath,
            string json,
            CancellationToken cancellationToken,
            bool authenticated = false)
        {
            var content = new StringContent(json ?? "{}", Encoding.UTF8, DivarApiOptions.JsonMediaType);
            return SendAsync(HttpMethod.Post, relativePath, content, cancellationToken, authenticated);
        }

        public Task<DivarApiResponse> DeleteAsync(
            string relativePath,
            CancellationToken cancellationToken,
            bool authenticated = false)
        {
            return SendAsync(HttpMethod.Delete, relativePath, null, cancellationToken, authenticated);
        }

        private async Task<DivarApiResponse> SendAsync(
            HttpMethod method,
            string relativePath,
            HttpContent content,
            CancellationToken cancellationToken,
            bool authenticated)
        {
            var stopwatch = Stopwatch.StartNew();
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return DivarApiResponse.Failure(null, string.Empty, "The API path is required.");
            }

            Uri requestUri;
            if (!Uri.TryCreate(relativePath, UriKind.Relative, out requestUri))
            {
                return DivarApiResponse.Failure(null, string.Empty, "Only relative Divar API paths are accepted.");
            }

            using (var request = new HttpRequestMessage(method, requestUri))
            {
                request.Content = content;

                if (authenticated)
                {
                    var frontToken = await _credentialProvider.GetFrontTokenAsync(cancellationToken);
                    if (string.IsNullOrWhiteSpace(frontToken))
                    {
                        var missing = DivarApiResponse.Failure(null, string.Empty, "Authentication is required.");
                        DivarDiagnostics.Api(method.Method, relativePath, stopwatch.ElapsedMilliseconds, null, false);
                        return missing;
                    }

                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", frontToken);
                }

                try
                {
                    using (var response = await SharedHttpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var bytes = await response.Content.ReadAsByteArrayAsync();
                        cancellationToken.ThrowIfCancellationRequested();
                        var body = DecodeUtf8(bytes);
                        var refreshedToken = GetHeader(response, "x-jwt-refresh");
                        var sessionRemoved = string.Equals(GetHeader(response, "front-token"), "remove", StringComparison.OrdinalIgnoreCase);
                        if (authenticated && !string.IsNullOrWhiteSpace(refreshedToken)) await _credentialProvider.SaveFrontTokenAsync(refreshedToken, cancellationToken);
                        if (authenticated && (sessionRemoved || response.StatusCode == System.Net.HttpStatusCode.Unauthorized))
                        {
                            await _credentialProvider.ClearFrontTokenAsync(cancellationToken);
                            DivarDiagnostics.AuthenticationExpired();
                        }

                        if (response.IsSuccessStatusCode)
                        {
                            var success = DivarApiResponse.Success(response.StatusCode, body);
                            if (!authenticated && method == HttpMethod.Get && IsCacheableGet(relativePath))
                                PublicGetCache.Set(relativePath, success, CacheLifetime(relativePath));
                            DivarDiagnostics.Api(method.Method, relativePath, stopwatch.ElapsedMilliseconds, (int)response.StatusCode, false);
                            return success;
                        }

                        var failure = DivarApiResponse.Failure(
                            response.StatusCode,
                            body,
                            "Divar returned HTTP " + (int)response.StatusCode + ".");
                        DivarDiagnostics.Api(method.Method, relativePath, stopwatch.ElapsedMilliseconds, (int)response.StatusCode, false);
                        return failure;
                    }
                }
                catch (OperationCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    DivarDiagnostics.Api(method.Method, relativePath, stopwatch.ElapsedMilliseconds, null, false);
                    return DivarApiResponse.Failure(null, string.Empty, "The request timed out.");
                }
                catch (HttpRequestException)
                {
                    DivarDiagnostics.Api(method.Method, relativePath, stopwatch.ElapsedMilliseconds, null, false);
                    return DivarApiResponse.Failure(null, string.Empty, "The network request failed.");
                }
                catch (Exception)
                {
                    DivarDiagnostics.Api(method.Method, relativePath, stopwatch.ElapsedMilliseconds, null, false);
                    return DivarApiResponse.Failure(null, string.Empty, "An unexpected API error occurred.");
                }
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                BaseAddress = DivarApiOptions.ApiBaseUri,
                Timeout = TimeSpan.FromSeconds(30)
            };

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(DivarApiOptions.JsonMediaType));
            client.DefaultRequestHeaders.AcceptCharset.Add(
                new StringWithQualityHeaderValue("utf-8"));
            client.DefaultRequestHeaders.AcceptLanguage.Add(
                new StringWithQualityHeaderValue(DivarApiOptions.PersianLanguage));
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(DivarApiOptions.UserAgentProduct, DivarApiOptions.UserAgentVersion));
            client.DefaultRequestHeaders.Add("X-Render-Type", "CSR");
            client.DefaultRequestHeaders.Add("X-Standard-Divar-Error", "true");

            return client;
        }

        public static void ClearPublicCache()
        {
            PublicGetCache.Clear();
        }

        private static bool IsCacheableGet(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            return path.StartsWith("v8/places/cities", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("v1/open-platform/assets/category", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("v8/posts-v2/web/", StringComparison.OrdinalIgnoreCase);
        }

        private static TimeSpan CacheLifetime(string path)
        {
            return path.StartsWith("v8/posts-v2/web/", StringComparison.OrdinalIgnoreCase)
                ? TimeSpan.FromMinutes(2)
                : TimeSpan.FromHours(6);
        }

        private static bool IsTransient(DivarApiResponse response)
        {
            return response != null && (!response.StatusCode.HasValue || (int)response.StatusCode.Value >= 500);
        }

        private static string DecodeUtf8(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        private static string GetHeader(HttpResponseMessage response, string name)
        {
            System.Collections.Generic.IEnumerable<string> values;
            return response.Headers.TryGetValues(name, out values) ? values.FirstOrDefault() ?? string.Empty : string.Empty;
        }
    }
}
