using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PlayaApiV2.Model;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PlayaApiV2.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IConfiguration _configuration;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        // User store loaded from configuration
        private readonly Dictionary<string, string> _users;


        public AuthenticationService(IConfiguration configuration)
        {
            _configuration = configuration;
            _jwtSecret = _configuration["JWT:Secret"] ?? "your-super-secret-jwt-key-that-should-be-at-least-32-characters-long";
            _jwtIssuer = _configuration["JWT:Issuer"] ?? "PlayaVR-API";
            _jwtAudience = _configuration["JWT:Audience"] ?? "PlayaVR-Client";
            
            // Load users from configuration
            _users = new Dictionary<string, string>();
            var usersSection = _configuration.GetSection("Users");
            foreach (var user in usersSection.GetChildren())
            {
                var username = user.Key;
                var password = user.Value;
                _users[username] = password;
            }
        }

        public Task<Rsp<Token>> SignInPasswordAsync(SignInPasswordRequest request)
        {
            try
            {
                // Validate credentials
                if (string.IsNullOrEmpty(request.Login) || string.IsNullOrEmpty(request.Password))
                {
                    return Task.FromResult(new Rsp<Token>(ApiStatus.From(ApiStatusCodes.AUTHORIZATION_FAILED, "Login and password are required"), null));
                }

                if (!_users.TryGetValue(request.Login, out var storedPassword) || storedPassword != request.Password)
                {
                    return Task.FromResult(new Rsp<Token>(ApiStatus.From(ApiStatusCodes.AUTHORIZATION_FAILED, "Invalid credentials"), null));
                }

                // Generate tokens
                var accessToken = GenerateAccessToken(request.Login);
                var refreshToken = GenerateRefreshToken(request.Login);

                var token = new Token
                {
                    Access = accessToken,
                    Refresh = refreshToken
                };

                return Task.FromResult(new Rsp<Token>(ApiStatus.OK, token));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new Rsp<Token>(ApiStatus.From(ApiStatusCodes.ERROR, ex.Message), null));
            }
        }


        public Task<Rsp<Token>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return Task.FromResult(new Rsp<Token>(ApiStatus.From(ApiStatusCodes.UNAUTHORIZED, "Refresh token is required"), null));
                }

                // Validate refresh token
                if (!ValidateToken(request.RefreshToken))
                {
                    return Task.FromResult(new Rsp<Token>(ApiStatus.From(ApiStatusCodes.UNAUTHORIZED, "Invalid refresh token"), null));
                }

                var userId = GetUserIdFromToken(request.RefreshToken);
                var accessToken = GenerateAccessToken(userId);
                var refreshToken = GenerateRefreshToken(userId);

                var token = new Token
                {
                    Access = accessToken,
                    Refresh = refreshToken
                };

                return Task.FromResult(new Rsp<Token>(ApiStatus.OK, token));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new Rsp<Token>(ApiStatus.From(ApiStatusCodes.ERROR, ex.Message), null));
            }
        }

        public Task<Rsp> SignOutAsync(SignOutRequest request)
        {
            try
            {
                // In a real implementation, you'd invalidate the refresh token
                // For now, just return success
                return Task.FromResult(new Rsp(ApiStatus.OK));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new Rsp(ApiStatus.From(ApiStatusCodes.ERROR, ex.Message)));
            }
        }

        public Task<Rsp<UserProfile>> GetProfileAsync(string accessToken)
        {
            try
            {
                if (!ValidateToken(accessToken))
                {
                    return Task.FromResult(new Rsp<UserProfile>(ApiStatus.From(ApiStatusCodes.UNAUTHORIZED, "Invalid token"), null));
                }

                var userId = GetUserIdFromToken(accessToken);

                var profile = new UserProfile
                {
                    Name = userId,
                    Role = "user"
                };

                return Task.FromResult(new Rsp<UserProfile>(ApiStatus.OK, profile));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new Rsp<UserProfile>(ApiStatus.From(ApiStatusCodes.ERROR, ex.Message), null));
            }
        }

        public Task<Rsp<ScriptsInfo>> GetScriptsInfoAsync(string accessToken)
        {
            try
            {
                if (!ValidateToken(accessToken))
                {
                    return Task.FromResult(new Rsp<ScriptsInfo>(ApiStatus.From(ApiStatusCodes.UNAUTHORIZED, "Invalid token"), null));
                }

                var scriptsToken = GenerateAccessToken(GetUserIdFromToken(accessToken));

                var scriptsInfo = new ScriptsInfo
                {
                    Token = scriptsToken
                };

                return Task.FromResult(new Rsp<ScriptsInfo>(ApiStatus.OK, scriptsInfo));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new Rsp<ScriptsInfo>(ApiStatus.From(ApiStatusCodes.ERROR, ex.Message), null));
            }
        }

        public bool ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetUserIdFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private string GenerateAccessToken(string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", userId),
                    new Claim("type", "access")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken(string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", userId),
                    new Claim("type", "refresh")
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRandomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        public void SetAuthenticationCookie(HttpContext context, string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = false, // Allow JavaScript access for debugging
                Secure = false, // Set to true in production with HTTPS
                SameSite = SameSiteMode.Lax, // More permissive than Strict, less than None
                Expires = DateTime.UtcNow.AddDays(7), // Match refresh token expiration
                Path = "/", // Make cookie available for all paths
                Domain = null // Allow all domains
            };
            
            context.Response.Cookies.Append("auth_token", token, cookieOptions);
        }

        public void ClearAuthenticationCookie(HttpContext context)
        {
            context.Response.Cookies.Delete("auth_token");
        }
    }
}