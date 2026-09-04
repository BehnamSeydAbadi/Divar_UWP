using System;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Windows.Data.Json;

namespace Divar_UWP.Services
{
    public sealed class DivarAuthService : IDivarAuthService
    {
        private const string AuthenticatePath = "v5/auth/authenticate";
        private const string ConfirmPath = "v5/auth/confirm";
        private const string LogoutPath = "v8/auth/logout";
        private readonly IDivarApiClient _apiClient;
        private readonly IDivarCredentialProvider _credentials;

        public DivarAuthService(IDivarApiClient apiClient, IDivarCredentialProvider credentials)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        }

        public bool IsAuthenticated { get { return _credentials.HasFrontToken; } }

        public async Task<ServiceResult<DivarAuthChallenge>> SendCodeAsync(string phoneNumber, CancellationToken cancellationToken)
        {
            var phone = NormalizeIranianPhone(phoneNumber);
            if (string.IsNullOrWhiteSpace(phone)) return ServiceResult<DivarAuthChallenge>.Failure("شمارهٔ موبایل را به‌درستی وارد کنید.");
            try
            {
                var request = new JsonObject();
                request["phone"] = JsonValue.CreateStringValue(phone);
                var response = await _apiClient.PostJsonAsync(AuthenticatePath, request.Stringify(), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!response.IsSuccess) return ServiceResult<DivarAuthChallenge>.Failure(ReadError(response.Content, "ارسال کد ورود ممکن نشد."));
                return ServiceResult<DivarAuthChallenge>.Success(new DivarAuthChallenge { PhoneNumber = phone });
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { return ServiceResult<DivarAuthChallenge>.Failure("خطایی هنگام ارسال کد ورود رخ داد."); }
        }

        public async Task<ServiceResult<bool>> ConfirmCodeAsync(DivarAuthChallenge challenge, string code, CancellationToken cancellationToken)
        {
            if (challenge == null || string.IsNullOrWhiteSpace(challenge.PhoneNumber)) return ServiceResult<bool>.Failure("ابتدا شمارهٔ موبایل را وارد کنید.");
            var normalizedCode = ToEnglishDigits(code).Trim();
            if (string.IsNullOrWhiteSpace(normalizedCode)) return ServiceResult<bool>.Failure("کد ورود را وارد کنید.");
            try
            {
                var request = new JsonObject();
                request["phone"] = JsonValue.CreateStringValue(challenge.PhoneNumber);
                request["code"] = JsonValue.CreateStringValue(normalizedCode);
                var response = await _apiClient.PostJsonAsync(ConfirmPath, request.Stringify(), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!response.IsSuccess) return ServiceResult<bool>.Failure(ReadError(response.Content, "کد ورود معتبر نیست یا منقضی شده است."));
                JsonObject root; IJsonValue token;
                if (!JsonObject.TryParse(response.Content, out root) || !root.TryGetValue("token", out token) || token.ValueType != JsonValueType.String || string.IsNullOrWhiteSpace(token.GetString())) return ServiceResult<bool>.Failure("پاسخ ورود از دیوار معتبر نیست.");
                await _credentials.SaveFrontTokenAsync(token.GetString(), cancellationToken);
                return ServiceResult<bool>.Success(true);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { return ServiceResult<bool>.Failure("خطایی هنگام تأیید کد ورود رخ داد."); }
        }

        public async Task<ServiceResult<bool>> SignOutAsync(CancellationToken cancellationToken)
        {
            var token = await _credentials.GetFrontTokenAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrWhiteSpace(token))
                {
                    var request = new JsonObject();
                    request["token"] = JsonValue.CreateStringValue(token);
                    await _apiClient.PostJsonAsync(LogoutPath, request.Stringify(), cancellationToken);
                }
                await _credentials.ClearFrontTokenAsync(cancellationToken);
                return ServiceResult<bool>.Success(true);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { await _credentials.ClearFrontTokenAsync(CancellationToken.None); return ServiceResult<bool>.Success(true); }
        }

        private static string NormalizeIranianPhone(string value)
        {
            var phone = ToEnglishDigits(value).Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
            if (phone.Length == 10 && phone.StartsWith("9", StringComparison.Ordinal)) phone = "0" + phone;
            else if (phone.StartsWith("+980", StringComparison.Ordinal)) phone = phone.Substring(3);
            else if (phone.StartsWith("+98", StringComparison.Ordinal)) phone = "0" + phone.Substring(3);
            return phone.Length == 11 && phone.StartsWith("09", StringComparison.Ordinal) ? phone : string.Empty;
        }

        private static string ToEnglishDigits(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4').Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9').Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4').Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
        }

        private static string ReadError(string content, string fallback)
        {
            JsonObject root, message; IJsonValue text;
            if (!JsonObject.TryParse(content, out root)) return fallback;
            if (root.TryGetValue("message", out text) && text.ValueType == JsonValueType.String) return text.GetString();
            if (root.TryGetValue("message", out text) && text.ValueType == JsonValueType.Object && (message = text.GetObject()).TryGetValue("message", out text) && text.ValueType == JsonValueType.String) return text.GetString();
            return fallback;
        }
    }
}
