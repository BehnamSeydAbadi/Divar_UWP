using System;

namespace Divar_UWP.Infrastructure
{
    public static class DivarApiOptions
    {
        public static readonly Uri ApiBaseUri = new Uri("https://api.divar.ir/");

        public const string JsonMediaType = "application/json";
        public const string PersianLanguage = "fa-IR";
        public const string UserAgentProduct = "Divar_UWP";
        public const string UserAgentVersion = "0.1";
    }
}
