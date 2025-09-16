using Microsoft.AspNetCore.Http;
using PlayaApiV2.Model;
using System.Threading.Tasks;

namespace PlayaApiV2.Services
{
    public interface IAuthenticationService
    {
        Task<Rsp<Token>> SignInPasswordAsync(SignInPasswordRequest request);
        Task<Rsp<Token>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<Rsp> SignOutAsync(SignOutRequest request);
        Task<Rsp<UserProfile>> GetProfileAsync(string accessToken);
        Task<Rsp<ScriptsInfo>> GetScriptsInfoAsync(string accessToken);
        bool ValidateToken(string token);
        string GetUserIdFromToken(string token);
        void SetAuthenticationCookie(HttpContext context, string token);
        void ClearAuthenticationCookie(HttpContext context);
    }
}
