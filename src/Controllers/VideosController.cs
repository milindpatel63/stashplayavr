using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json.Linq;

using PlayaApiV2.Filters;
using PlayaApiV2.Model;
using PlayaApiV2.Model.Events;
using PlayaApiV2.Repositories;

using Semver;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlayaApiV2.Controllers
{
    [Route("api/playa/v2")]
    [ApiController]
    [TypeFilter(typeof(ExceptionFilter))]
    [TypeFilter(typeof(AuthenticationFilter))]
    public class VideosController : ControllerBase
    {
        private readonly VideosRepository _repository;

        public VideosController(VideosRepository videosRepository)
        {
            _repository = videosRepository;
        }

        [HttpGet("version")]
        public Rsp<SemVersion> GetVersion()
        {
            return new SemVersion(1, 3, 0);
        }

        [HttpGet("config")]
        public Rsp<Configuration> GetConfiguration()
        {
            return new Configuration
            {
                SiteName = "StashApp VR Bridge",
                SiteLogo = "https://raw.githubusercontent.com/stashapp/stash/develop/ui/v2.5/public/android-chrome-192x192.png",

                Actors = true,
                Categories = true,
                CategoriesGroups = false,
                Studios = true,
                Analytics = true,
                Scripts = true,
                Masks = false,
                Auth = true,
                NSFW = true,
                Deals = false,
                AuthByCode = false,

                Theme = 0,
            };
        }

        [HttpGet("videos")]
        public async Task<Rsp<Page<VideoListView>>> GetVideos(
            [FromQuery(Name = "page-index")] long pageIndex,
            [FromQuery(Name = "page-size")] long pageSize,
            [FromQuery(Name = "title")] string searchTitle,
            [FromQuery(Name = "studio")] string studioId,
            [FromQuery(Name = "actor")] string actorId,
            [FromQuery(Name = "included-categories")] string includedCategories,
            [FromQuery(Name = "excluded-categories")] string excludedCategories,
            [FromQuery(Name = "order")] string sortOrder,
            [FromQuery(Name = "direction")] string sortDirection
            )
        {
            // Check if user is authenticated
            var isAuthenticated = HttpContext.Items["IsAuthenticated"] as bool? ?? false;
            
            if (!isAuthenticated)
            {
                return new Rsp<Page<VideoListView>>(ApiStatus.From(ApiStatusCodes.OK), new Page<VideoListView>
                {
                    Content = new List<VideoListView>(),
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    ItemTotal = 0,
                    PageTotal = 0
                });
            }
            
            var query = new VideosQuery()
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                SearchTitle = searchTitle,
                StudioId = studioId,
                ActorId = actorId,
                SortDirection = sortDirection,
                SortOrder = sortOrder,
            };

            query.AddIncludedCategories(includedCategories?.Split(',', StringSplitOptions.RemoveEmptyEntries));
            query.AddExcludedCategories(excludedCategories?.Split(',', StringSplitOptions.RemoveEmptyEntries));

            var videos = await _repository.GetVideosAsync(query);
            
            // Add caching headers for better performance
            Response.Headers["Cache-Control"] = "public, max-age=300"; // Cache for 5 minutes
            Response.Headers["ETag"] = $"\"videos-{pageIndex}-{pageSize}-{sortOrder}-{sortDirection}\"";
            Response.Headers["Last-Modified"] = DateTime.UtcNow.ToString("R");
            
            return videos;
        }

        [HttpGet("video/{videoId}")]
        [HttpPost("video/{videoId}")]
        public async Task<Rsp<VideoView>> GetVideoDetails([FromRoute(Name = "videoId")] string videoId)
        {
            var video = await _repository.GetVideoAsync(videoId);
            
            // Add caching headers for video details
            Response.Headers["Cache-Control"] = "public, max-age=600"; // Cache for 10 minutes
            Response.Headers["ETag"] = $"\"video-{videoId}\"";
            Response.Headers["Last-Modified"] = DateTime.UtcNow.ToString("R");
            
            return video;
        }

        [HttpGet("video/{videoId}/details")]
        public async Task<Rsp<VideoView>> GetVideoDetailsLazy([FromRoute(Name = "videoId")] string videoId)
        {
            // Lazy loading endpoint - only fetch when explicitly requested
            var video = await _repository.GetVideoAsync(videoId);
            
            // Add caching headers for video details
            Response.Headers["Cache-Control"] = "public, max-age=300"; // Cache for 5 minutes
            Response.Headers["ETag"] = $"\"video-details-{videoId}\"";
            Response.Headers["Last-Modified"] = DateTime.UtcNow.ToString("R");
            
            return video;
        }

        [HttpGet("categories")]
        public async Task<Rsp<List<CategoryListView>>> GetCategories()
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var authCookie = HttpContext.Request.Cookies["auth_token"];
            var authParam = HttpContext.Request.Query["auth_token"].ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            
            // Check if user is authenticated
            var isAuthenticated = HttpContext.Items["IsAuthenticated"] as bool? ?? false;
            
            // Special handling for PlayaVR Steam client
            // Check if PlayaVR client is sending any authentication
            if (userAgent == "PLAYAVR")
            {
                bool hasAnyAuth = !string.IsNullOrEmpty(authHeader) || authCookie != null || !string.IsNullOrEmpty(authParam);
                
                if (hasAnyAuth)
                {
                    var playaVrCategories = await _repository.GetCategoriesAsync();
                    return playaVrCategories;
                }
                else
                {
                    // Return dummy category for guest PlayaVR users
                    var dummyCategory = new List<CategoryListView>
                    {
                        new CategoryListView
                        {
                            Id = "free",
                            Title = "Free",
                            Preview = ""
                        }
                    };
                    return new Rsp<List<CategoryListView>>(ApiStatus.From(ApiStatusCodes.OK), dummyCategory);
                }
            }
            
            if (!isAuthenticated)
            {
                // Return a dummy "Free" category for guest users
                var dummyCategory = new List<CategoryListView>
                {
                    new CategoryListView
                    {
                        Id = "free",
                        Title = "Free",
                        Preview = ""
                    }
                };
                return new Rsp<List<CategoryListView>>(ApiStatus.From(ApiStatusCodes.OK), dummyCategory);
            }
            
            var categories = await _repository.GetCategoriesAsync();
            return categories;
        }

        [HttpPut("event")]
        public Rsp Event([FromBody] JObject parameters)
        {
            const string VIDEO_STREAM_END = "videoStreamEnd";
            const string VIDEO_DOWNLOADED = "videoDownloaded";

            var eventType = parameters.Value<string>("event_type");
            switch (eventType)
            {
                case VIDEO_STREAM_END:
                {
                    var eventData = parameters.ToObject<VideoStreamEnd>();
                    // Handle video stream end event
                    break;
                }
                case VIDEO_DOWNLOADED:
                {
                    var eventData = parameters.ToObject<VideoDownloaded>();
                    // Handle video downloaded event
                    break;
                }
            }
            return ApiStatus.OK;
        }
    }
}
