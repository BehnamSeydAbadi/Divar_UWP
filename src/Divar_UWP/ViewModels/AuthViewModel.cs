using System;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Divar_UWP.Services;

namespace Divar_UWP.ViewModels
{
    public sealed class AuthViewModel : ObservableObject
    {
        private readonly IDivarAuthService _authService;
        private DivarAuthChallenge _challenge;
        private string _phoneNumber;
        private string _code;
        private string _message;
        private bool _isBusy;
        private bool _isAuthenticated;

        public AuthViewModel(IDivarAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            RefreshState();
        }

        public string PhoneNumber { get { return _phoneNumber; } set { SetProperty(ref _phoneNumber, value); } }
        public string Code { get { return _code; } set { SetProperty(ref _code, value); } }
        public string Message { get { return _message; } private set { if (SetProperty(ref _message, value)) OnPropertyChanged("HasMessage"); } }
        public bool HasMessage { get { return !string.IsNullOrWhiteSpace(Message); } }
        public bool IsBusy { get { return _isBusy; } private set { SetProperty(ref _isBusy, value); } }
        public bool IsAuthenticated { get { return _isAuthenticated; } private set { if (SetProperty(ref _isAuthenticated, value)) { OnPropertyChanged("NeedsCode"); OnPropertyChanged("NeedsPhone"); } } }
        public bool NeedsPhone { get { return !IsAuthenticated && _challenge == null; } }
        public bool NeedsCode { get { return !IsAuthenticated && _challenge != null; } }

        public void RefreshState() { IsAuthenticated = _authService.IsAuthenticated; }

        public async Task SendCodeAsync(CancellationToken cancellationToken)
        {
            IsBusy = true; Message = string.Empty;
            try { var result = await _authService.SendCodeAsync(PhoneNumber, cancellationToken); if (!result.IsSuccess) { Message = result.ErrorMessage; return; } _challenge = result.Value; OnPropertyChanged("NeedsPhone"); OnPropertyChanged("NeedsCode"); Message = "کد ورود ارسال شد."; }
            finally { IsBusy = false; }
        }

        public async Task ConfirmCodeAsync(CancellationToken cancellationToken)
        {
            IsBusy = true; Message = string.Empty;
            try { var result = await _authService.ConfirmCodeAsync(_challenge, Code, cancellationToken); if (!result.IsSuccess) { Message = result.ErrorMessage; return; } IsAuthenticated = true; _challenge = null; OnPropertyChanged("NeedsPhone"); OnPropertyChanged("NeedsCode"); Message = "با موفقیت وارد شدید."; }
            finally { IsBusy = false; }
        }

        public async Task LogoutAsync(CancellationToken cancellationToken)
        {
            IsBusy = true;
            try { await _authService.SignOutAsync(cancellationToken); IsAuthenticated = false; _challenge = null; Code = string.Empty; Message = "از حساب کاربری خارج شدید."; OnPropertyChanged("NeedsPhone"); OnPropertyChanged("NeedsCode"); }
            finally { IsBusy = false; }
        }
    }
}
