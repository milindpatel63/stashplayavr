using Microsoft.AspNetCore.Mvc;
using PlayaApiV2.Filters;
using PlayaApiV2.Model;
using PlayaApiV2.Services;
using System.Linq;
using System.Threading.Tasks;

namespace PlayaApiV2.Controllers
{
    [Route("api/playa/v2/user")]
    [ApiController]
    [TypeFilter(typeof(ExceptionFilter))]
    public class UserController : ControllerBase
    {
        private readonly IAuthenticationService _authService;

        public UserController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        [HttpGet("profile")]
        public async Task<Rsp<UserProfile>> GetProfile()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                return new Rsp<UserProfile>(ApiStatus.From(ApiStatusCodes.UNAUTHORIZED, "Authorization header missing"), null);
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            return await _authService.GetProfileAsync(token);
        }

        [HttpGet("scripts-info")]
        public async Task<Rsp<ScriptsInfo>> GetScriptsInfo()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                return new Rsp<ScriptsInfo>(ApiStatus.From(ApiStatusCodes.UNAUTHORIZED, "Authorization header missing"), null);
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            return await _authService.GetScriptsInfoAsync(token);
        }
    }
}
