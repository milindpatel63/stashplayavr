using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PlayaApiV2.Model;
using PlayaApiV2.Services;
using System;
using System.Linq;

namespace PlayaApiV2.Filters
{
    public class AuthenticationFilter : IActionFilter
    {
        private readonly IAuthenticationService _authService;

        public AuthenticationFilter(IAuthenticationService authService)
        {
            _authService = authService;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Skip authentication for auth endpoints and version/config endpoints
            var controllerName = context.Controller.GetType().Name;
            var actionName = context.ActionDescriptor.DisplayName;

            if (ShouldSkipAuthentication(controllerName, actionName))
            {
                return;
            }

            // For streaming endpoints, check session-based authentication
            if (IsStreamingEndpoint(context))
            {
                if (!IsUserAuthenticated(context))
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }
                return; // Allow streaming for authenticated users
            }

            // Check if this endpoint allows guest access
            if (ShouldAllowGuestAccess(controllerName, actionName))
            {
                
                // Check for Authorization header (optional for guest endpoints)
                var authHeaderCheck = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (authHeaderCheck != null && authHeaderCheck.StartsWith("Bearer "))
                {
                    var token = authHeaderCheck.Substring("Bearer ".Length).Trim();
                    if (_authService.ValidateToken(token))
                    {
                        // Store user info in context for use in controllers
                        var userId = _authService.GetUserIdFromToken(token);
                        context.HttpContext.Items["UserId"] = userId;
                        context.HttpContext.Items["IsAuthenticated"] = true;
                    }
                    else
                    {
                        context.HttpContext.Items["IsAuthenticated"] = false;
                    }
                }
                else
                {
                    // Check for cookie-based authentication
                    var authCookieCheck = context.HttpContext.Request.Cookies["auth_token"];
                    if (!string.IsNullOrEmpty(authCookieCheck) && _authService.ValidateToken(authCookieCheck))
                    {
                        var userId = _authService.GetUserIdFromToken(authCookieCheck);
                        context.HttpContext.Items["UserId"] = userId;
                        context.HttpContext.Items["IsAuthenticated"] = true;
                        if (actionName.Contains("GetCategories"))
                        {
                        }
                    }
                    else
                    {
                        // Check for query parameter authentication
                        var authParamCheck = context.HttpContext.Request.Query["auth_token"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(authParamCheck) && _authService.ValidateToken(authParamCheck))
                        {
                            var userId = _authService.GetUserIdFromToken(authParamCheck);
                            context.HttpContext.Items["UserId"] = userId;
                            context.HttpContext.Items["IsAuthenticated"] = true;
                            if (actionName.Contains("GetCategories"))
                            {
                            }
                        }
                        else
                        {
                            context.HttpContext.Items["IsAuthenticated"] = false;
                            if (actionName.Contains("GetCategories"))
                            {
                            }
                        }
                    }
                }
                return; // Allow guest access
            }

            // For protected API endpoints, require Authorization header
            var authHeaderProtected = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeaderProtected == null || !authHeaderProtected.StartsWith("Bearer "))
            {
                context.Result = new JsonResult(new Rsp(ApiStatus.From(ApiStatusCodes.UNAUTHORIZED, "Authorization header missing")))
                {
                    StatusCode = 200 // Use 200 OK with API status code as per docs
                };
                return;
            }

            var tokenProtected = authHeaderProtected.Substring("Bearer ".Length).Trim();
            if (!_authService.ValidateToken(tokenProtected))
            {
                context.Result = new JsonResult(new Rsp(ApiStatus.From(ApiStatusCodes.UNAUTHORIZED, "Invalid token")))
                {
                    StatusCode = 200 // Use 200 OK with API status code as per docs
                };
                return;
            }

            // Store user info in context for use in controllers
            var userIdProtected = _authService.GetUserIdFromToken(tokenProtected);
            context.HttpContext.Items["UserId"] = userIdProtected;
            context.HttpContext.Items["IsAuthenticated"] = true;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed after execution
        }

        private bool ShouldSkipAuthentication(string controllerName, string actionName)
        {
            // Skip authentication for these endpoints
            var skipControllers = new[] { "AuthController" };
            var skipActions = new[] 
            { 
                "GetVersion", 
                "GetConfiguration",
                "GetHealth"
            };

            return skipControllers.Contains(controllerName) || 
                   skipActions.Any(action => actionName.Contains(action));
        }

        private bool ShouldAllowGuestAccess(string controllerName, string actionName)
        {
            // Allow guest access to these data endpoints (return limited data)
            var guestControllers = new[] { "VideosController", "ActorsController", "StudiosController", "StreamController" };
            var guestActions = new[] 
            { 
                "GetVideos", 
                "GetCategories",
                "GetActors",
                "GetStudios",
                "GetActor",
                "GetStudio",
                "ProxySpriteRequest"
            };

            return guestControllers.Contains(controllerName) && 
                   guestActions.Any(action => actionName.Contains(action));
        }

        private bool IsStreamingEndpoint(ActionExecutingContext context)
        {
            var path = context.HttpContext.Request.Path.Value?.ToLower();
            if (string.IsNullOrEmpty(path))
                return false;

            // Skip authentication for /stream endpoint entirely
            if (path.Contains("/stream"))
            {
                return false; // No authentication required for streaming/downloads
            }

            // Check if this is a streaming endpoint (excluding sprite-proxy which is guest accessible)
            return path.Contains("/download") || 
                   path.Contains("/preview") ||
                   path.Contains("/poster") ||
                   path.Contains("/image");
        }

        private bool IsUserAuthenticated(ActionExecutingContext context)
        {
            // Check for Authorization header first (if present)
            var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (_authService.ValidateToken(token))
                {
                    // Store user info in context
                    var userId = _authService.GetUserIdFromToken(token);
                    context.HttpContext.Items["UserId"] = userId;
                    return true;
                }
            }

            // Check for session-based authentication
            // Look for authentication cookie or session
            var authCookie = context.HttpContext.Request.Cookies["auth_token"];
            if (!string.IsNullOrEmpty(authCookie))
            {
                if (_authService.ValidateToken(authCookie))
                {
                    var userId = _authService.GetUserIdFromToken(authCookie);
                    context.HttpContext.Items["UserId"] = userId;
                    return true;
                }
            }

            // Check for query parameter authentication (for streaming URLs)
            var authParam = context.HttpContext.Request.Query["auth_token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authParam))
            {
                if (_authService.ValidateToken(authParam))
                {
                    var userId = _authService.GetUserIdFromToken(authParam);
                    context.HttpContext.Items["UserId"] = userId;
                    return true;
                }
            }

            return false;
        }
    }
}
