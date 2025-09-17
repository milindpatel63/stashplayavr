using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlayaApiV2.Model;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PlayaApiV2.Services
{
    public class StashAppService : IStashAppService
    {
        private readonly HttpClient _httpClient;
        private readonly StashAppOptions _options;
        private readonly AppOptions _appOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public StashAppService(HttpClient httpClient, IOptions<StashAppOptions> options, IOptions<AppOptions> appOptions, IHttpContextAccessor httpContextAccessor, IMemoryCache cache, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _appOptions = appOptions.Value;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
            _configuration = configuration;
            _jwtSecret = _configuration["JWT:Secret"] ?? "your-super-secret-jwt-key-that-should-be-at-least-32-characters-long";
            _jwtIssuer = _configuration["JWT:Issuer"] ?? "PlayaVR-API";
            _jwtAudience = _configuration["JWT:Audience"] ?? "PlayaVR-Client";
            
            // Configure HttpClient - DON'T set Content-Type here, it goes on HttpContent
            _httpClient.BaseAddress = new Uri(_options.GraphQLUrl);
            _httpClient.DefaultRequestHeaders.Add("ApiKey", _options.ApiKey);
        }

        public async Task<List<VideoView>> GetVideosAsync(int page = 1, int perPage = 50)
        {
            try
            {
                // Increase perPage to get more videos at once
                var actualPerPage = Math.Max(perPage, 100); // Minimum 100 videos per request
                
                var query = $@"
                    query {{
                        findScenes(scene_filter: {{}}, filter: {{ per_page: {actualPerPage}, page: {page} }}) {{
                            count
                            scenes {{
                                id
                                title
                                date
                                o_counter
                                rating100
                                studio {{
                                    id
                                    name
                                }}
                                tags {{
                                    id
                                    name
                                }}
                                files {{
                                    path
                                    size
                                    width
                                    height
                                    duration
                                }}
                                paths {{
                                    screenshot
                                    sprite
                                    preview
                                }}
                            }}
                        }}
                    }}";

                var response = await ExecuteGraphQLQueryAsync(query);
                var scenes = response?["findScenes"]?["scenes"] as JArray;
                var totalCount = response?["findScenes"]?["count"]?.ToObject<int>() ?? 0;

                if (scenes == null)
                {
                    Console.WriteLine("No scenes found in StashApp response");
                    return new List<VideoView>();
                }

                var videos = new List<VideoView>();
                foreach (var scene in scenes)
                {
                    try
                    {
                        var video = ConvertSceneToVideoView(scene);
                        if (video != null)
                        {
                            videos.Add(video);
                            if (videos.Count <= 5) // Log first 5 successful conversions
                            {
                            }
                        }
                        else
                        {
                            Console.WriteLine($"ConvertSceneToVideoView returned null for scene {scene["id"]}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error converting scene {scene["id"]}: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                }

                Console.WriteLine($"Retrieved {videos.Count} videos from StashApp (Total available: {totalCount})");
                return videos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching videos from StashApp: {ex.Message}");
                return new List<VideoView>();
            }
        }

        public async Task<List<VideoView>> GetVideosByActorAsync(string actorId, int page = 1, int perPage = 50)
        {
            try
            {
                var actualPerPage = Math.Max(perPage, 100);
                
                var query = $@"
                    query {{
                        findScenes(scene_filter: {{ performers: {{ value: ""{actorId}"", modifier: INCLUDES }} }}, filter: {{ per_page: {actualPerPage}, page: {page} }}) {{
                            count
                            scenes {{
                                id
                                title
                                details
                                date
                                created_at
                                updated_at
                                o_counter
                                o_history
                                files {{
                                    path
                                    size
                                    width
                                    height
                                    duration
                                    video_codec
                                    audio_codec
                                    frame_rate
                                    bit_rate
                                }}
                                tags {{
                                    id
                                    name
                                }}
                                studio {{
                                    id
                                    name
                                }}
                                performers {{
                                    id
                                    name
                                }}
                                paths {{
                                    screenshot
                                    sprite
                                    preview
                                }}
                            }}
                        }}
                    }}";

                var response = await ExecuteGraphQLQueryAsync(query);
                var scenes = response?["findScenes"]?["scenes"] as JArray;
                var totalCount = response?["findScenes"]?["count"]?.ToObject<int>() ?? 0;

                if (scenes == null)
                {
                    Console.WriteLine($"No scenes found for actor {actorId}");
                    return new List<VideoView>();
                }

                var videos = new List<VideoView>();
                foreach (var scene in scenes)
                {
                    try
                    {
                        var video = ConvertSceneToVideoView(scene);
                        if (video != null)
                        {
                            videos.Add(video);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error converting scene {scene["id"]}: {ex.Message}");
                    }
                }

                Console.WriteLine($"Retrieved {videos.Count} videos for actor {actorId} (Total available: {totalCount})");
                return videos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching videos for actor {actorId}: {ex.Message}");
                return new List<VideoView>();
            }
        }

        public async Task<VideoView> GetVideoAsync(string videoId)
        {
            try
            {
                // Check cache first for lazy loading
                var cacheKey = $"video_details_{videoId}";
                if (_cache.TryGetValue(cacheKey, out VideoView cachedVideo))
                {
                    return cachedVideo;
                }

                var query = $@"
                    query {{
                        findScene(id: {videoId}) {{
                            id
                            title
                            details
                            date
                            files {{
                                path
                                size
                                width
                                height
                                duration
                                video_codec
                                audio_codec
                                frame_rate
                            }}
                            tags {{
                                id
                                name
                            }}
                            studio {{
                                id
                                name
                            }}
                            performers {{
                                id
                                name
                            }}
                            paths {{
                                screenshot
                                sprite
                                preview
                            }}
                        }}
                    }}";

                var response = await ExecuteGraphQLQueryAsync(query);
                var scene = response?["findScene"];

                if (scene == null)
                {
                    Console.WriteLine($"Scene {videoId} not found in StashApp");
                    return null;
                }

                var video = ConvertSceneToVideoView(scene);
                
                // Cache the video details for 15 minutes
                if (video != null)
                {
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
                        SlidingExpiration = TimeSpan.FromMinutes(10)
                    };
                    _cache.Set(cacheKey, video, cacheOptions);
                }
                
                return video;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching video {videoId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<CategoryListView>> GetCategoriesAsync()
        {
            try
            {
                // First, get the total count of tags
                var countQuery = @"
                    query {
                        findTags(tag_filter: {}, filter: { per_page: 1, page: 1 }) {
                            count
                        }
                    }";

                var countResponse = await ExecuteGraphQLQueryAsync(countQuery);
                var totalCount = countResponse?["findTags"]?["count"]?.ToObject<int>() ?? 0;

                Console.WriteLine($"Total tags available in StashApp: {totalCount}");

                // Now fetch all tags with a large page size
                var query = $@"
                    query {{
                        findTags(tag_filter: {{}}, filter: {{ per_page: {Math.Max(totalCount, 5000)}, page: 1 }}) {{
                            count
                            tags {{
                                id
                                name
                            }}
                        }}
                    }}";

                var response = await ExecuteGraphQLQueryAsync(query);
                var tags = response?["findTags"]?["tags"] as JArray;

                if (tags == null)
                {
                    Console.WriteLine("No tags found in StashApp response");
                    return new List<CategoryListView>();
                }

                var categories = new List<CategoryListView>();
                foreach (var tag in tags)
                {
                    try
                    {
                        categories.Add(new CategoryListView
                        {
                            Id = tag["id"]?.ToString(),
                            Title = tag["name"]?.ToString(),
                            Preview = null
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error converting tag: {ex.Message}");
                    }
                }

                Console.WriteLine($"Retrieved {categories.Count} categories from StashApp (Total available: {totalCount})");
                return categories;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching categories from StashApp: {ex.Message}");
                return new List<CategoryListView>();
            }
        }

        public async Task<List<ActorListView>> GetActorsAsync()
        {
            try
            {
                // Build the performer filter based on ShowAllActors setting
                string performerFilter = _appOptions.ShowAllActors 
                    ? "" // No filter - get all performers
                    : "performer_filter: { scene_count: { value: 0, modifier: GREATER_THAN } }";

                // First, get the total count
                var countQuery = $@"
                    query {{
                        findPerformers({performerFilter}, filter: {{ per_page: 1, page: 1 }}) {{
                            count
                        }}
                    }}";

                var countResponse = await ExecuteGraphQLQueryAsync(countQuery);
                var totalCount = countResponse?["findPerformers"]?["count"]?.ToObject<int>() ?? 0;

                string filterDescription = _appOptions.ShowAllActors ? "all performers" : "performers with scenes";
                Console.WriteLine($"Total {filterDescription} available in StashApp: {totalCount}");

                // Now fetch all performers with a large page size
                var query = $@"
                    query {{
                        findPerformers({performerFilter}, filter: {{ per_page: {Math.Max(totalCount, 5000)}, page: 1 }}) {{
                            count
                            performers {{
                                id
                                name
                                image_path
                                rating100
                                scene_count
                            }}
                        }}
                    }}";

                var response = await ExecuteGraphQLQueryAsync(query);
                var performers = response?["findPerformers"]?["performers"] as JArray;

                if (performers == null)
                {
                    Console.WriteLine("No performers found in StashApp response");
                    return new List<ActorListView>();
                }

                var actors = new List<ActorListView>();
                foreach (var performer in performers)
                {
                    try
                    {
                        actors.Add(new ActorListView
                        {
                            Id = performer["id"]?.ToString(),
                            Title = performer["name"]?.ToString(),
                            Preview = GetActorImageProxyUrl(performer["id"]?.ToString()), // Use bridge proxy URL
                            Rating = performer["rating100"]?.ToObject<int?>()
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error converting performer: {ex.Message}");
                    }
                }

                Console.WriteLine($"Retrieved {actors.Count} actors from StashApp (Total available: {totalCount})");
                return actors;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching actors from StashApp: {ex.Message}");
                return new List<ActorListView>();
            }
        }

        public async Task<ActorView> GetActorAsync(string actorId)
        {
            try
            {
                // Use the CORRECT StashApp GraphQL query with findPerformer and all the proper fields
                var query = $@"
                    query {{
                        findPerformer(id: {actorId}) {{
                            id
                            name
                            image_path
                            gender
                            ethnicity
                            country
                            eye_color
                            hair_color
                            height_cm
                            weight
                            measurements
                            career_length
                            details
                            url
                            disambiguation
                            fake_tits
                            death_date
                            alias_list
                            rating100
                        }}
                    }}";

                var response = await ExecuteGraphQLQueryAsync(query);
                var performer = response?["findPerformer"];

                if (performer == null)
                {
                    Console.WriteLine($"Performer {actorId} not found in StashApp");
                    return null;
                }

                Console.WriteLine($"Found performer {actorId}: {performer["name"]?.ToString()}");
                
                // Get studios from scenes where this performer appears
                var studios = await GetStudiosForActor(actorId);
                
                return ConvertPerformerToActorView(performer, studios);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching actor {actorId}: {ex.Message}");
                return null;
            }
        }

        private async Task<List<ActorView.Studio>> GetStudiosForActor(string actorId)
        {
            try
            {
                var query = $@"
                    query {{
                        findScenes(scene_filter: {{ performers: {{ value: ""{actorId}"", modifier: INCLUDES }} }}, filter: {{ per_page: 100, page: 1 }}) {{
                            scenes {{
                                studio {{
                                    id
                                    name
                                }}
                            }}
                        }}
                    }}";

                var response = await ExecuteGraphQLQueryAsync(query);
                var scenes = response?["findScenes"]?["scenes"] as JArray;

                if (scenes == null)
                {
                    return new List<ActorView.Studio>();
                }

                var studios = new List<ActorView.Studio>();
                var studioIds = new HashSet<string>();

                foreach (var scene in scenes)
                {
                    var studio = scene["studio"];
                    if (studio != null && studio.Type == JTokenType.Object)
                    {
                        var studioId = studio["id"]?.ToString();
                        var studioName = studio["name"]?.ToString();
                        
                        if (!string.IsNullOrEmpty(studioId) && !string.IsNullOrEmpty(studioName) && !studioIds.Contains(studioId))
                        {
                            studios.Add(new ActorView.Studio
                            {
                                Id = studioId,
                                Title = studioName
                            });
                            studioIds.Add(studioId);
                        }
                    }
                }

                return studios;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching studios for actor {actorId}: {ex.Message}");
                return new List<ActorView.Studio>();
            }
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var query = "query { findScenes { count } }";
                await ExecuteGraphQLQueryAsync(query);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Health check failed: {ex.Message}");
                return false;
            }
        }

        private string GetActorImageProxyUrl(string actorId)
        {
            if (string.IsNullOrEmpty(actorId))
                return null;
            
            // Get the current request's host to make URLs accessible from external devices
            var request = _httpContextAccessor.HttpContext?.Request;
            var host = request?.Host.ToString() ?? "localhost:8890";
            var scheme = request?.Scheme ?? "http";
            
            // Get auth token from current request
            var authToken = GetCurrentAuthToken();
            var authParam = !string.IsNullOrEmpty(authToken) ? $"?auth_token={authToken}" : "";
            
            // Point to our bridge API for actor images
            return $"{scheme}://{host}/api/playa/v2/actor/{actorId}/image{authParam}";
        }

        private async Task<JObject> ExecuteGraphQLQueryAsync(string query)
        {
            try
            {
                // Create cache key based on query hash
                var cacheKey = $"graphql_{query.GetHashCode()}";
                
                // Try to get from cache first
                if (_cache.TryGetValue(cacheKey, out JObject cachedResult))
                {
                    return cachedResult;
                }
                
                var request = new
                {
                    query = query
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<JObject>(responseContent);

                if (responseObject["errors"] != null)
                {
                    var errors = responseObject["errors"];
                    Console.WriteLine($"GraphQL errors: {errors}");
                    throw new Exception($"GraphQL errors: {errors}");
                }

                var result = responseObject["data"] as JObject;
                
                // Cache the result for 10 minutes
                if (result != null)
                {
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                        SlidingExpiration = TimeSpan.FromMinutes(5)
                    };
                    _cache.Set(cacheKey, result, cacheOptions);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GraphQL query failed: {ex.Message}");
                throw;
            }
        }

        private VideoView ConvertSceneToVideoView(JToken scene)
        {
            try
            {
                var video = new VideoView
                {
                    Id = scene["id"]?.ToString(),
                    Title = scene["title"]?.ToString() ?? "Untitled",
                    Subtitle = GetStudioName(scene["studio"]),
                    Description = scene["details"]?.ToString(),
                    Preview = GetThumbnailUrl(scene["id"]?.ToString()),
                    ReleaseDate = ParseDate(scene["date"]?.ToString()),
                    CreatedAt = ParseDate(scene["created_at"]?.ToString()),
                    UpdatedAt = ParseDate(scene["updated_at"]?.ToString()),
                    OCount = scene["o_counter"]?.ToObject<long?>(),
                    OHistory = GetOHistory(scene["o_history"]),
                    Views = 0,
                    Rating = scene["rating100"]?.ToObject<int?>()
                };

                // Set categories from tags - handle both JArray and single values
                video.Categories = GetCategories(scene["tags"]);

                // Set actors from performers - handle both JArray and single values
                video.Actors = GetPerformers(scene["performers"]);

                // Set studio
                var studio = scene["studio"];
                if (studio != null && studio.Type == JTokenType.Object)
                {
                    video.Studio = new VideoView.StudioRef
                    {
                        Id = studio["id"]?.ToString(),
                        Title = studio["name"]?.ToString()
                    };
                }

                // Set video details with multiple quality options
                var files = scene["files"] as JArray;
                var details = new List<VideoView.VideoDetails>();
                
                if (files != null && files.Count > 0)
                {
                    // Sort files by preference: MP4 first, then WebM, then others
                    var sortedFiles = files.OrderBy(f => GetFilePriority(f["path"]?.ToString())).ToList();
                    
                    var videoDetails = new VideoView.VideoDetails
                        {
                            Type = "full",
                            DurationSeconds = GetDuration(sortedFiles),
                        BitRate = GetBitRate(sortedFiles),
                            Links = CreateVideoLinks(scene["id"]?.ToString(), sortedFiles)
                    };

                    // Add sprite/timeline atlas if available
                    var spriteUrl = scene["paths"]?["sprite"]?.ToString();
                    if (!string.IsNullOrEmpty(spriteUrl))
                    {
                        videoDetails.TimelineAtlas = CreateTimelineAtlas(scene["id"]?.ToString(), GetDuration(sortedFiles));
                    }

                    details.Add(videoDetails);
                }
                
                // Add trailer details if preview is available
                var previewUrl = scene["paths"]?["preview"]?.ToString();
                if (!string.IsNullOrEmpty(previewUrl))
                {
                    var trailerDetails = new VideoView.VideoDetails
                    {
                        Type = "trailer",
                        DurationSeconds = 10, // Default trailer duration
                        Links = new List<VideoLinkView>
                        {
                            new VideoLinkView
                            {
                                IsStream = true,
                                IsDownload = false,
                                Url = GetBridgePreviewUrl(scene["id"]?.ToString()),
                                QualityOrder = 5, // Lower quality for trailer
                                QualityName = "Trailer",
                                ProjectionString = "FLT",
                                StereoString = "MN"
                            }
                        }
                    };
                    
                    details.Add(trailerDetails);
                }
                
                video.Details = details;

                return video;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting scene to video view: {ex.Message}");
                return null;
            }
        }

        private int GetFilePriority(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return 999;
            
            var extension = filePath.Split('.').LastOrDefault()?.ToLower();
            return extension switch
            {
                "mp4" => 1,
                "webm" => 2,
                "mov" => 3,
                "avi" => 4,
                "mkv" => 5,
                _ => 6
            };
        }

        private long GetDuration(List<JToken> files)
        {
            foreach (var file in files)
            {
                var duration = file["duration"]?.ToObject<double?>();
                if (duration.HasValue && duration > 0)
                {
                    return (long)duration.Value;
                }
            }
            return 0;
        }

        private long GetBitRate(List<JToken> files)
        {
            foreach (var file in files)
            {
                var bitRate = file["bit_rate"]?.ToObject<long?>();
                if (bitRate.HasValue && bitRate > 0)
                {
                    return bitRate.Value;
                }
            }
            return 0;
        }

        private List<string> GetOHistory(JToken oHistory)
        {
            if (oHistory == null || oHistory.Type != JTokenType.Array)
                return new List<string>();

            var history = new List<string>();
            foreach (var item in oHistory)
            {
                if (item != null && item.Type != JTokenType.Null)
                {
                    history.Add(item.ToString());
                }
            }
            return history;
        }

        private List<VideoLinkView> CreateVideoLinks(string videoId, List<JToken> files)
        {
            var links = new List<VideoLinkView>();
            
            // Check if user is authenticated
            var isAuthenticated = IsUserAuthenticated();
            
            foreach (var file in files.Take(3)) // Limit to 3 quality options
            {
                var qualityName = GetQualityName(file);
                var qualityOrder = GetQualityOrder(file);
                var projection = GetProjection(file);
                var stereo = GetStereo(file);
                
                // Use the same URL for both streaming and downloading as per PlayaVR docs
                // The endpoint will detect the intent based on HTTP method
                links.Add(new VideoLinkView
                {
                    IsStream = true,
                    IsDownload = true,
                    Url = isAuthenticated ? GetBridgeStreamUrl(videoId) : null,
                    UnavailableReason = isAuthenticated ? null : "login",
                    QualityOrder = qualityOrder,
                    QualityName = qualityName,
                    ProjectionString = projection,
                    StereoString = stereo
                });
            }
            
            return links;
        }

        private string GetProjection(JToken file)
        {
            // Default to flat projection - you can enhance this to detect VR content
            return "FLT";
        }

        private string GetStereo(JToken file)
        {
            // Default to mono - you can enhance this to detect stereo content
            return "MN";
        }

        private int GetQualityOrder(JToken file)
        {
            var height = file["height"]?.ToObject<int>() ?? 0;
            return height switch
            {
                >= 2160 => 45, // 4K
                >= 1440 => 35, // 2K
                >= 1080 => 25, // 1080p
                >= 720 => 15,  // 720p
                >= 480 => 10,  // 480p
                _ => 5         // SD
            };
        }

        private string GetStudioName(JToken studio)
        {
            if (studio == null) return null;
            
            if (studio.Type == JTokenType.Object)
            {
                return studio["name"]?.ToString();
            }
            
            return studio.ToString();
        }

        private List<VideoView.CategoryRef> GetCategories(JToken tags)
        {
            var categories = new List<VideoView.CategoryRef>();
            
            if (tags == null) return categories;
            
            if (tags.Type == JTokenType.Array)
            {
                var tagArray = tags as JArray;
                foreach (var tag in tagArray)
                {
                    if (tag.Type == JTokenType.Object)
                    {
                        categories.Add(new VideoView.CategoryRef
                        {
                            Id = tag["id"]?.ToString(),
                            Title = tag["name"]?.ToString()
                        });
                    }
                }
            }
            
            return categories;
        }

        private List<VideoView.ActorRef> GetPerformers(JToken performers)
        {
            var actors = new List<VideoView.ActorRef>();
            
            if (performers == null) return actors;
            
            if (performers.Type == JTokenType.Array)
            {
                var performerArray = performers as JArray;
                foreach (var performer in performerArray)
                {
                    if (performer.Type == JTokenType.Object)
                    {
                        actors.Add(new VideoView.ActorRef
                        {
                            Id = performer["id"]?.ToString(),
                            Title = performer["name"]?.ToString()
                        });
                    }
                }
            }
            
            return actors;
        }

        private string GetThumbnailUrl(string videoId)
        {
            if (string.IsNullOrEmpty(videoId))
                return null;
            
            // Get the current request's host to make URLs accessible from external devices
            var request = _httpContextAccessor.HttpContext?.Request;
            var host = request?.Host.ToString() ?? "localhost:8890";
            var scheme = request?.Scheme ?? "http";
            
            // Get auth token from current request
            var authToken = GetCurrentAuthToken();
            var authParam = !string.IsNullOrEmpty(authToken) ? $"?auth_token={authToken}" : "";
            
            // Point to our bridge API for thumbnails
            return $"{scheme}://{host}/api/playa/v2/video/{videoId}/poster{authParam}";
        }

        private string GetBridgeStreamUrl(string videoId)
        {
            if (string.IsNullOrEmpty(videoId))
                return null;
            
            // Get the current request's host to make URLs accessible from external devices
            var request = _httpContextAccessor.HttpContext?.Request;
            var host = request?.Host.ToString() ?? "localhost:8890";
            var scheme = request?.Scheme ?? "http";
            
            // Get auth token from current request
            var authToken = GetCurrentAuthToken();
            var authParam = !string.IsNullOrEmpty(authToken) ? $"?auth_token={authToken}" : "";
            
            // Point to our bridge API for video streaming
            return $"{scheme}://{host}/api/playa/v2/video/{videoId}/stream{authParam}";
        }

        private string GetBridgeDownloadUrl(string videoId)
        {
            if (string.IsNullOrEmpty(videoId))
                return null;
            
            // Get the current request's host to make URLs accessible from external devices
            var request = _httpContextAccessor.HttpContext?.Request;
            var host = request?.Host.ToString() ?? "localhost:8890";
            var scheme = request?.Scheme ?? "http";
            
            // Point to our bridge API for video downloading
            return $"{scheme}://{host}/api/playa/v2/video/{videoId}/download";
        }

        private string GetBridgePreviewUrl(string videoId)
        {
            if (string.IsNullOrEmpty(videoId))
                return null;
            
            // Get the current request's host to make URLs accessible from external devices
            var request = _httpContextAccessor.HttpContext?.Request;
            var host = request?.Host.ToString() ?? "localhost:8890";
            var scheme = request?.Scheme ?? "http";
            
            // Point to our bridge API for video preview/trailer
            return $"{scheme}://{host}/api/playa/v2/video/{videoId}/preview";
        }

        private string ConvertSpriteUrlToProxy(string spriteUrl)
        {
            if (string.IsNullOrEmpty(spriteUrl))
                return null;
            
            // Get the current request's host to make URLs accessible from external devices
            var request = _httpContextAccessor.HttpContext?.Request;
            var host = request?.Host.ToString() ?? "localhost:8890";
            var scheme = request?.Scheme ?? "http";
            
            // Extract the path from the StashApp sprite URL and create a proxy URL
            try
            {
                var uri = new Uri(spriteUrl);
                var path = uri.AbsolutePath;
                
                // Create a proxy URL that points to our sprite-proxy endpoint
                return $"{scheme}://{host}/api/playa/v2/sprite-proxy{path}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting sprite URL to proxy: {ex.Message}");
                return spriteUrl; // Return original URL if conversion fails
            }
        }

        private string ConvertPreviewUrlToProxy(string previewUrl)
        {
            if (string.IsNullOrEmpty(previewUrl))
                return null;
            
            // Get the current request's host to make URLs accessible from external devices
            var request = _httpContextAccessor.HttpContext?.Request;
            var host = request?.Host.ToString() ?? "localhost:8890";
            var scheme = request?.Scheme ?? "http";
            
            // Extract the path from the StashApp preview URL and create a proxy URL
            try
            {
                var uri = new Uri(previewUrl);
                var path = uri.AbsolutePath;
                
                // Create a proxy URL that points to our preview-proxy endpoint
                return $"{scheme}://{host}/api/playa/v2/preview-proxy{path}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting preview URL to proxy: {ex.Message}");
                return previewUrl; // Return original URL if conversion fails
            }
        }

        private bool IsUserAuthenticated()
        {
            try
            {
                var request = _httpContextAccessor.HttpContext?.Request;
                if (request == null) return false;

                // Check for Authorization header
                var authHeader = request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != null && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    return ValidateJwtToken(token);
                }

                // Check for authentication cookie
                var authCookie = request.Cookies["auth_token"];
                if (!string.IsNullOrEmpty(authCookie))
                {
                    return ValidateJwtToken(authCookie);
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking authentication: {ex.Message}");
                return false;
            }
        }

        private bool ValidateJwtToken(string token)
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

        private string GetCurrentAuthToken()
        {
            try
            {
                var request = _httpContextAccessor.HttpContext?.Request;
                if (request == null) 
                {
                    return null;
                }

                // Check for Authorization header first
                var authHeader = request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != null && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    return token;
                }

                // Check for authentication cookie
                var authCookie = request.Cookies["auth_token"];
                if (!string.IsNullOrEmpty(authCookie))
                {
                    return authCookie;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public string GetPreviewUrl(string videoId)
        {
            try
            {
                // Get the preview URL from StashApp GraphQL response
                var query = $@"
                    query {{
                        findScene(id: {videoId}) {{
                            paths {{
                                preview
                            }}
                        }}
                    }}";

                var response = ExecuteGraphQLQueryAsync(query).Result;
                var previewUrl = response?["findScene"]?["paths"]?["preview"]?.ToString();
                
                if (string.IsNullOrEmpty(previewUrl))
                {
                    Console.WriteLine($"No preview URL found for video {videoId}");
                    return null;
                }
                
                // Convert to proxy URL
                return ConvertPreviewUrlToProxy(previewUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting preview URL for video {videoId}: {ex.Message}");
                return null;
            }
        }

        private ActorView ConvertPerformerToActorView(JToken performer, List<ActorView.Studio> studios)
        {
            try
            {
                var actor = new ActorView
                {
                    Id = performer["id"]?.ToString(),
                    Title = performer["name"]?.ToString(),
                    Preview = GetActorImageProxyUrl(performer["id"]?.ToString()), // Use bridge proxy URL
                    Views = 0, // StashApp doesn't track views for performers
                    Rating = performer["rating100"]?.ToObject<int?>(),
                    Studios = studios
                };

                // Set aliases from alias_list field (proper field from StashApp)
                var aliases = new List<string>();
                var aliasList = performer["alias_list"] as JArray;
                if (aliasList != null)
                {
                    foreach (var alias in aliasList)
                    {
                        var aliasValue = alias?.ToString();
                        if (!string.IsNullOrEmpty(aliasValue) && !aliasValue.Equals(performer["name"]?.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            aliases.Add(aliasValue);
                        }
                    }
                }
                actor.Aliases = aliases;

                // Set properties from performer data with DESCRIPTIVE labels
                var properties = new List<ActorView.Property>();
                
                if (!string.IsNullOrEmpty(performer["gender"]?.ToString()))
                    properties.Add(new ActorView.Property { Id = "gender", Value = $"Gender: {performer["gender"]}" });
                
                if (!string.IsNullOrEmpty(performer["ethnicity"]?.ToString()))
                    properties.Add(new ActorView.Property { Id = "ethnicity", Value = $"Ethnicity: {performer["ethnicity"]}" });
                
                if (!string.IsNullOrEmpty(performer["country"]?.ToString()))
                    properties.Add(new ActorView.Property { Id = "country", Value = $"Country: {performer["country"]}" });
                
                if (!string.IsNullOrEmpty(performer["eye_color"]?.ToString()))
                    properties.Add(new ActorView.Property { Id = "eye_color", Value = $"Eye Color: {performer["eye_color"]}" });
                
                if (!string.IsNullOrEmpty(performer["hair_color"]?.ToString()))
                    properties.Add(new ActorView.Property { Id = "hair_color", Value = $"Hair Color: {performer["hair_color"]}" });
                
                if (!string.IsNullOrEmpty(performer["height_cm"]?.ToString()))
                    properties.Add(new ActorView.Property { Id = "height", Value = $"Height: {performer["height_cm"]} cm" });
                
                if (!string.IsNullOrEmpty(performer["weight"]?.ToString()))
                    properties.Add(new ActorView.Property { Id = "weight", Value = $"Weight: {performer["weight"]}" });
                
                if (!string.IsNullOrEmpty(performer["measurements"]?.ToString()))
                    properties.Add(new ActorView.Property { Id = "measurements", Value = $"Measurements: {performer["measurements"]}" });
                
                if (!string.IsNullOrEmpty(performer["career_length"]?.ToString()))
                    properties.Add(new ActorView.Property { Id = "career_length", Value = $"Career Length: {performer["career_length"]}" });

                // Add age calculation from details (accounting for death_date)
                var age = CalculateAgeFromDetails(performer["details"]?.ToString(), performer["death_date"]?.ToString());
                if (age.HasValue)
                    properties.Add(new ActorView.Property { Id = "age", Value = $"Age: {age} years old" });

                // Add fake tits from proper StashApp field
                var fakeTits = performer["fake_tits"]?.ToString();
                if (!string.IsNullOrEmpty(fakeTits))
                {
                    var fakeTitsLabel = fakeTits.ToLower() switch
                    {
                        "fake" => "Fake Tits: Yes",
                        "natural" => "Fake Tits: No (Natural)",
                        _ => $"Fake Tits: {fakeTits}"
                    };
                    properties.Add(new ActorView.Property { Id = "fake_tits", Value = fakeTitsLabel });
                }

                // Add details/biography
                var details = performer["details"]?.ToString();
                if (!string.IsNullOrEmpty(details))
                    properties.Add(new ActorView.Property { Id = "details", Value = $"Biography: {details}" });

                actor.Properties = properties;

                return actor;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting performer to actor view: {ex.Message}");
                return null;
            }
        }

        private int? CalculateAgeFromDetails(string details, string deathDate)
        {
            if (string.IsNullOrEmpty(details))
                return null;

            try
            {
                // Look for birth date patterns like "born February 7, 1988" or "born 1988"
                var birthDatePattern = @"born\s+(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{1,2},?\s+(\d{4})";
                var match = Regex.Match(details, birthDatePattern, RegexOptions.IgnoreCase);
                
                if (!match.Success)
                {
                    // Try simpler pattern like "born 1988"
                    birthDatePattern = @"born\s+(\d{4})";
                    match = Regex.Match(details, birthDatePattern, RegexOptions.IgnoreCase);
                }

                if (match.Success && int.TryParse(match.Groups[1].Value, out int birthYear))
                {
                    // If death_date is provided, use that year; otherwise use current year
                    int endYear;
                    if (!string.IsNullOrEmpty(deathDate) && DateTime.TryParse(deathDate, out var deathDateTime))
                    {
                        endYear = deathDateTime.Year;
                    }
                    else
                    {
                        endYear = DateTime.Now.Year;
                    }
                    
                    var age = endYear - birthYear;
                    return age > 0 && age < 120 ? age : null; // Sanity check
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating age: {ex.Message}");
            }

            return null;
        }

        private string GetQualityName(JToken videoFile)
        {
            var width = videoFile["width"]?.ToObject<int>() ?? 0;
            var height = videoFile["height"]?.ToObject<int>() ?? 0;

            if (height >= 2160) return "4K";
            if (height >= 1440) return "2K";
            if (height >= 1080) return "1080p";
            if (height >= 720) return "720p";
            if (height >= 480) return "480p";
            
            return "SD";
        }

        private Timestamp? ParseDate(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return null;

            if (DateTime.TryParse(dateString, out var date))
                return Timestamp.From(date);

            return null;
        }

        public async Task<List<StudioListView>> GetStudiosAsync()
        {
            try
            {
                // First, get the total count of studios
                var countQuery = @"
                    query {
                        findStudios(studio_filter: {}, filter: { per_page: 1, page: 1 }) {
                            count
                        }
                    }";

                var countResponse = await ExecuteGraphQLQueryAsync(countQuery);
                var totalCount = countResponse?["findStudios"]?["count"]?.ToObject<int>() ?? 0;

                Console.WriteLine($"Total studios available in StashApp: {totalCount}");

                // Now fetch all studios with a large page size
                var query = $@"
                    query {{
                        findStudios(studio_filter: {{}}, filter: {{ per_page: {Math.Max(totalCount, 5000)}, page: 1 }}) {{
                            count
                            studios {{
                                id
                                name
                                image_path
                            }}
                        }}
                    }}";

                var response = await ExecuteGraphQLQueryAsync(query);
                var studios = response?["findStudios"]?["studios"] as JArray;

                if (studios == null)
                {
                    Console.WriteLine("No studios found in StashApp response");
                    return new List<StudioListView>();
                }

                var studioList = new List<StudioListView>();
                foreach (var studio in studios)
                {
                    try
                    {
                        studioList.Add(new StudioListView
                        {
                            Id = studio["id"]?.ToString(),
                            Title = studio["name"]?.ToString(),
                            Preview = GetStudioImageProxyUrl(studio["id"]?.ToString()) // Use bridge proxy URL
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error converting studio: {ex.Message}");
                    }
                }

                Console.WriteLine($"Retrieved {studioList.Count} studios from StashApp (Total available: {totalCount})");
                return studioList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching studios from StashApp: {ex.Message}");
                return new List<StudioListView>();
            }
        }

        public async Task<StudioView> GetStudioAsync(string studioId)
        {
            try
            {
                var query = $@"
                    query {{
                        findStudio(id: ""{studioId}"") {{
                            id
                            name
                            url
                            parent_studio {{
                              id
                              name
                            }}
                            child_studios {{
                              id
                              name
                            }}
                            details
                            aliases
                            image_path
                        }}
                    }}";

                var response = await ExecuteGraphQLQueryAsync(query);
                var studio = response?["findStudio"];

                if (studio == null)
                {
                    Console.WriteLine($"Studio {studioId} not found in StashApp");
                    return null;
                }

                Console.WriteLine($"Found studio {studioId}: {studio["name"]?.ToString()}");
                
                return ConvertStudioToStudioView(studio);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching studio {studioId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<VideoView>> GetVideosByStudioAsync(string studioId, int page = 1, int perPage = 50)
        {
            try
            {
                var actualPerPage = Math.Max(perPage, 100);
                
                var query = $@"
                    query {{
                        findScenes(scene_filter: {{ studios: {{ value: ""{studioId}"", modifier: INCLUDES }} }}, filter: {{ per_page: {actualPerPage}, page: {page} }}) {{
                            count
                            scenes {{
                                id
                                title
                                details
                                date
                                created_at
                                updated_at
                                o_counter
                                o_history
                                files {{
                                    path
                                    size
                                    width
                                    height
                                    duration
                                    video_codec
                                    audio_codec
                                    frame_rate
                                    bit_rate
                                }}
                                tags {{
                                    id
                                    name
                                }}
                                studio {{
                                    id
                                    name
                                }}
                                performers {{
                                    id
                                    name
                                }}
                                paths {{
                                    screenshot
                                    sprite
                                    preview
                                }}
                            }}
                        }}
                    }}";

                var response = await ExecuteGraphQLQueryAsync(query);
                var scenes = response?["findScenes"]?["scenes"] as JArray;
                var totalCount = response?["findScenes"]?["count"]?.ToObject<int>() ?? 0;

                if (scenes == null || scenes.Count == 0)
                {
                    Console.WriteLine($"No scenes found for studio {studioId} (studio information may not be populated in scenes)");
                    return new List<VideoView>();
                }

                var videos = new List<VideoView>();
                foreach (var scene in scenes)
                {
                    try
                    {
                        var video = ConvertSceneToVideoView(scene);
                        if (video != null)
                        {
                            videos.Add(video);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error converting scene {scene["id"]}: {ex.Message}");
                    }
                }

                Console.WriteLine($"Retrieved {videos.Count} videos for studio {studioId} (Total available: {totalCount})");
                return videos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching videos for studio {studioId}: {ex.Message}");
                return new List<VideoView>();
            }
        }

        private string GetStudioImageProxyUrl(string studioId)
        {
            if (string.IsNullOrEmpty(studioId))
                return null;
            
            // Get the current request's host to make URLs accessible from external devices
            var request = _httpContextAccessor.HttpContext?.Request;
            var host = request?.Host.ToString() ?? "localhost:8890";
            var scheme = request?.Scheme ?? "http";
            
            // Get auth token from current request
            var authToken = GetCurrentAuthToken();
            var authParam = !string.IsNullOrEmpty(authToken) ? $"?auth_token={authToken}" : "";
            
            // Point to our bridge API for studio images
            return $"{scheme}://{host}/api/playa/v2/studio/{studioId}/image{authParam}";
        }

        private StudioView ConvertStudioToStudioView(JToken studio)
        {
            try
            {
                var description = studio["details"]?.ToString();
                if (string.IsNullOrEmpty(description))
                {
                    description = "Studio information from StashApp.";
                }

                // Add parent/child studio information to description as text
                var parentStudio = studio["parent_studio"];
                if (parentStudio != null && parentStudio.Type != JTokenType.Null)
                {
                    var parentName = parentStudio["name"]?.ToString() ?? "Unknown Studio";
                    description += $"\n\nParent Studio: {parentName}";
                }

                var childStudios = studio["child_studios"];
                if (childStudios != null && childStudios.Type == JTokenType.Array)
                {
                    var childStudioNames = new List<string>();
                    foreach (var childStudio in childStudios)
                    {
                        if (childStudio != null && childStudio.Type != JTokenType.Null)
                        {
                            var childName = childStudio["name"]?.ToString() ?? "Unknown Studio";
                            childStudioNames.Add(childName);
                        }
                    }
                    
                    if (childStudioNames.Count > 0)
                    {
                        description += $"\n\nChild Studios:\n{string.Join("\n", childStudioNames)}";
                    }
                }

                var studioView = new StudioView
                {
                    Id = studio["id"]?.ToString(),
                    Title = studio["name"]?.ToString(),
                    Preview = GetStudioImageProxyUrl(studio["id"]?.ToString()), // Use bridge proxy URL
                    Description = description,
                    Views = 0 // StashApp doesn't track views for studios
                };

                // Log additional studio data for debugging
                Console.WriteLine($"Studio {studioView.Id} additional data:");
                Console.WriteLine($"  URL: {studio["url"]?.ToString()}");
                Console.WriteLine($"  Aliases: {studio["aliases"]?.ToString()}");
                Console.WriteLine($"  Description length: {description.Length} characters");

                return studioView;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting studio to studio view: {ex.Message}");
                return null;
            }
        }

        private TimelineAtlas CreateTimelineAtlas(string videoId, long durationSeconds)
        {
            try
            {
                // Get the actual sprite URL from StashApp GraphQL response
                var query = $@"
                    query {{
                        findScene(id: {videoId}) {{
                            paths {{
                                sprite
                            }}
                        }}
                    }}";

                var response = ExecuteGraphQLQueryAsync(query).Result;
                var spriteUrl = response?["findScene"]?["paths"]?["sprite"]?.ToString();
                
                if (string.IsNullOrEmpty(spriteUrl))
                {
                    Console.WriteLine($"No sprite URL found for video {videoId}");
                    return null;
                }
                
                // Convert StashApp sprite URL to use our proxy
                var proxyUrl = ConvertSpriteUrlToProxy(spriteUrl);
                
                return new TimelineAtlas
                {
                    Version = 1,
                    Url = proxyUrl,  // Use our proxy URL
                    FrameWidth = 160,  // Frame width (1440÷9)
                    FrameHeight = 90,  // Frame height (810÷9)
                    Columns = 9,      // 9 columns
                    Rows = 9,         // 9 rows
                    Frames = 81,      // 81 total frames (9x9)
                    IntervalMs = null // Distribute frames equally along video duration
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating timeline atlas for video {videoId}: {ex.Message}");
                return null;
            }
        }
    }
}
