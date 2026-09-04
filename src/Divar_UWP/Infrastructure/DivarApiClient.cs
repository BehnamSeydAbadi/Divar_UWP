using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Divar_UWP.Infrastructure
{
    public sealed class DivarApiClient : IDivarApiClient
    {
        private static readonly HttpClient SharedHttpClient = CreateHttpClient();
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
            return SendAsync(HttpMethod.Get, relativePath, null, cancellationToken, authenticated);
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

        private async Task<DivarApiResponse> SendAsync(
            HttpMethod method,
            string relativePath,
            HttpContent content,
            CancellationToken cancellationToken,
            bool authenticated)
        {
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
                        return DivarApiResponse.Failure(null, string.Empty, "Authentication is required.");
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

                        if (response.IsSuccessStatusCode)
                        {
                            return DivarApiResponse.Success(response.StatusCode, body);
                        }

                        return DivarApiResponse.Failure(
                            response.StatusCode,
                            body,
                            "Divar returned HTTP " + (int)response.StatusCode + ".");
                    }
                }
                catch (OperationCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    return DivarApiResponse.Failure(null, string.Empty, "The request timed out.");
                }
                catch (HttpRequestException)
                {
                    return DivarApiResponse.Failure(null, string.Empty, "The network request failed.");
                }
                catch (Exception)
                {
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

        private static string DecodeUtf8(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }
    }
}
