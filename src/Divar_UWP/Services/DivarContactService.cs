using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Windows.Data.Json;

namespace Divar_UWP.Services
{
    public sealed class DivarContactService : IDivarContactService
    {
        private const string ContactPath = "v8/postcontact/web/contact_info_v2/";
        private readonly IDivarApiClient _apiClient;

        public DivarContactService(IDivarApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<ServiceResult<DivarContactResult>> GetPhoneAsync(string postToken, string contactUuid, string trackerSessionId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(postToken) || string.IsNullOrWhiteSpace(contactUuid)) return ServiceResult<DivarContactResult>.Failure("اطلاعات تماس این آگهی در دسترس نیست.");
            try
            {
                var request = new JsonObject();
                request["contact_uuid"] = JsonValue.CreateStringValue(contactUuid);
                if (!string.IsNullOrWhiteSpace(trackerSessionId)) request["tracker_session_id"] = JsonValue.CreateStringValue(trackerSessionId);
                var response = await _apiClient.PostJsonAsync(ContactPath + Uri.EscapeDataString(postToken), request.Stringify(), cancellationToken, true);
                cancellationToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized || string.Equals(response.ErrorMessage, "Authentication is required.", StringComparison.Ordinal)) return ServiceResult<DivarContactResult>.Success(new DivarContactResult { LoginRequired = true, Message = "برای دریافت اطلاعات تماس، وارد حساب کاربری خود شوید." });
                if (response.StatusCode == (HttpStatusCode)429) return ServiceResult<DivarContactResult>.Success(new DivarContactResult { RateLimited = true, Message = GetErrorMessage(response.Content, "تعداد درخواست‌ها زیاد است. کمی بعد دوباره تلاش کنید.") });
                if (!response.IsSuccess) return ServiceResult<DivarContactResult>.Failure(GetErrorMessage(response.Content, "دریافت اطلاعات تماس ممکن نشد."));

                JsonObject root;
                if (!JsonObject.TryParse(response.Content, out root)) return ServiceResult<DivarContactResult>.Failure("پاسخ اطلاعات تماس قابل خواندن نیست.");
                var phoneUri = FindTelephoneUri(root);
                if (!string.IsNullOrWhiteSpace(phoneUri)) return ServiceResult<DivarContactResult>.Success(new DivarContactResult { PhoneNumber = phoneUri.Substring(4) });
                var phoneNumber = FindIranianMobileNumber(root);
                if (!string.IsNullOrWhiteSpace(phoneNumber)) return ServiceResult<DivarContactResult>.Success(new DivarContactResult { PhoneNumber = phoneNumber });

                JsonObject hipAction;
                if (TryGetObject(root, "hip_action", out hipAction)) return ServiceResult<DivarContactResult>.Success(new DivarContactResult { ChallengeRequired = true, Message = "دیوار برای نمایش شماره، تکمیل بررسی امنیتی را لازم دانسته است." });
                return ServiceResult<DivarContactResult>.Failure("شماره‌ای برای این آگهی برنگشت.");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { return ServiceResult<DivarContactResult>.Failure("خطایی هنگام دریافت اطلاعات تماس رخ داد."); }
        }

        private static string FindTelephoneUri(IJsonValue value)
        {
            if (value == null) return string.Empty;
            if (value.ValueType == JsonValueType.String)
            {
                var text = value.GetString();
                return text.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ? text : string.Empty;
            }
            if (value.ValueType == JsonValueType.Object)
                foreach (var item in value.GetObject()) { var found = FindTelephoneUri(item.Value); if (!string.IsNullOrWhiteSpace(found)) return found; }
            else if (value.ValueType == JsonValueType.Array)
                foreach (var item in value.GetArray()) { var found = FindTelephoneUri(item); if (!string.IsNullOrWhiteSpace(found)) return found; }
            return string.Empty;
        }

        private static string FindIranianMobileNumber(IJsonValue value)
        {
            if (value == null) return string.Empty;
            if (value.ValueType == JsonValueType.String) return NormalizeIranianMobile(value.GetString());
            if (value.ValueType == JsonValueType.Object)
                foreach (var item in value.GetObject()) { var found = FindIranianMobileNumber(item.Value); if (!string.IsNullOrWhiteSpace(found)) return found; }
            else if (value.ValueType == JsonValueType.Array)
                foreach (var item in value.GetArray()) { var found = FindIranianMobileNumber(item); if (!string.IsNullOrWhiteSpace(found)) return found; }
            return string.Empty;
        }

        private static string NormalizeIranianMobile(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var digits = value.Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4').Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9');
            for (var index = 0; index <= digits.Length - 11; index++)
            {
                var candidate = digits.Substring(index, 11);
                if (candidate.StartsWith("09", StringComparison.Ordinal) && candidate.All(char.IsDigit)) return candidate;
            }
            return string.Empty;
        }

        private static string GetErrorMessage(string content, string fallback)
        {
            JsonObject root, message;
            if (!JsonObject.TryParse(content, out root)) return fallback;
            if (TryGetObject(root, "message", out message))
            {
                IJsonValue text;
                if (message.TryGetValue("message", out text) && text.ValueType == JsonValueType.String) return text.GetString();
            }
            IJsonValue direct;
            return root.TryGetValue("message", out direct) && direct.ValueType == JsonValueType.String ? direct.GetString() : fallback;
        }

        private static bool TryGetObject(JsonObject value, string key, out JsonObject result)
        {
            result = null; IJsonValue item;
            if (!value.TryGetValue(key, out item) || item.ValueType != JsonValueType.Object) return false;
            result = item.GetObject(); return true;
        }
    }
}
