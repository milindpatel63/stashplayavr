using Microsoft.AspNetCore.Mvc;
using PlayaApiV2.Filters;
using PlayaApiV2.Model;
using PlayaApiV2.Services;
using System.Threading.Tasks;

namespace PlayaApiV2.Controllers
{
    [Route("api/playa/v2/auth")]
    [ApiController]
    [TypeFilter(typeof(ExceptionFilter))]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;

        public AuthController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        [HttpPost("sign-in-password")]
        public async Task<Rsp<Token>> SignInPassword([FromBody] SignInPasswordRequest request)
        {
            var result = await _authService.SignInPasswordAsync(request);
            
            // Set authentication cookie for streaming endpoints
            if (result.Status.Code == ApiStatusCodes.OK && result.Data != null)
            {
                _authService.SetAuthenticationCookie(HttpContext, result.Data.Access);
            }
            
            return result;
        }


        [HttpPost("refresh")]
        public async Task<Rsp<Token>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request);
            
            // Update authentication cookie for streaming endpoints
            if (result.Status.Code == ApiStatusCodes.OK && result.Data != null)
            {
                _authService.SetAuthenticationCookie(HttpContext, result.Data.Access);
            }
            
            return result;
        }

        [HttpPost("sign-out")]
        public async Task<Rsp> SignOut([FromBody] SignOutRequest request)
        {
            // Clear authentication cookie
            _authService.ClearAuthenticationCookie(HttpContext);
            
            return await _authService.SignOutAsync(request);
        }
    }
}
