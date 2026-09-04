using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace Divar_UWP.Infrastructure
{
    public sealed class PasswordVaultDivarCredentialProvider : IDivarCredentialProvider
    {
        private const string Resource = "Divar_UWP";
        private const string UserName = "front-token";
        private readonly PasswordVault _vault = new PasswordVault();

        public bool HasFrontToken { get { return !string.IsNullOrWhiteSpace(ReadToken()); } }

        public Task<string> GetFrontTokenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ReadToken());
        }

        public Task SaveFrontTokenAsync(string token, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Clear();
            if (!string.IsNullOrWhiteSpace(token)) _vault.Add(new PasswordCredential(Resource, UserName, token));
            return Task.CompletedTask;
        }

        public Task ClearFrontTokenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Clear();
            return Task.CompletedTask;
        }

        private string ReadToken()
        {
            try
            {
                var credential = _vault.RetrieveAll().FirstOrDefault(item => item.Resource == Resource && item.UserName == UserName);
                if (credential == null) return string.Empty;
                credential.RetrievePassword();
                return credential.Password ?? string.Empty;
            }
            catch (Exception) { return string.Empty; }
        }

        private void Clear()
        {
            try
            {
                foreach (var credential in _vault.RetrieveAll().Where(item => item.Resource == Resource && item.UserName == UserName).ToList()) _vault.Remove(credential);
            }
            catch (Exception) { }
        }
    }
}
