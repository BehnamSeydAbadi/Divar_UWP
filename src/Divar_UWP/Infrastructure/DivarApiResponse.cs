using System.Net;

namespace Divar_UWP.Infrastructure
{
    public sealed class DivarApiResponse
    {
        private DivarApiResponse(bool isSuccess, HttpStatusCode? statusCode, string content, string errorMessage)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            Content = content ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public bool IsSuccess { get; private set; }

        public HttpStatusCode? StatusCode { get; private set; }

        public string Content { get; private set; }

        public string ErrorMessage { get; private set; }

        public static DivarApiResponse Success(HttpStatusCode statusCode, string content)
        {
            return new DivarApiResponse(true, statusCode, content, string.Empty);
        }

        public static DivarApiResponse Failure(HttpStatusCode? statusCode, string content, string errorMessage)
        {
            return new DivarApiResponse(false, statusCode, content, errorMessage);
        }
    }
}
