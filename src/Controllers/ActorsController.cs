using Microsoft.AspNetCore.Mvc;
using PlayaApiV2.Filters;
using PlayaApiV2.Model;
using PlayaApiV2.Repositories;
using PlayaApiV2.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PlayaApiV2.Controllers
{
    [Route("api/playa/v2")]
    [ApiController]
    [TypeFilter(typeof(ExceptionFilter))]
    public class ActorsController : ControllerBase
    {
        private readonly ActorsRepository _repository;
        private readonly IStashAppService _stashAppService;

        public ActorsController(ActorsRepository actorsRepository, IStashAppService stashAppService)
        {
            _repository = actorsRepository;
            _stashAppService = stashAppService;
        }

        [HttpGet("actors")]
        [TypeFilter(typeof(AuthenticationFilter))]
        public async Task<Rsp<Page<ActorListView>>> GetActors(
            [FromQuery(Name = "page-index")] long pageIndex,
            [FromQuery(Name = "page-size")] long pageSize,
            [FromQuery(Name = "title")] string searchTitle,
            [FromQuery(Name = "order")] string sortOrder,
            [FromQuery(Name = "direction")] string sortDirection
            )
        {
            // Check if user is authenticated
            var isAuthenticated = HttpContext.Items["IsAuthenticated"] as bool? ?? false;
            
            if (!isAuthenticated)
            {
                return new Rsp<Page<ActorListView>>(ApiStatus.From(ApiStatusCodes.OK), new Page<ActorListView>
                {
                    Content = new List<ActorListView>(),
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    ItemTotal = 0,
                    PageTotal = 0
                });
            }
            
            var query = new ActorsQuery()
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                SearchTitle = searchTitle,
                SortOrder = ParseSortOrder(sortOrder),
                SortDirection = ParseSortDirection(sortDirection)
            };

            var result = await _repository.GetActorsAsync(query);
            return result;
        }

        [HttpGet("actor/{id}")]
        [TypeFilter(typeof(AuthenticationFilter))]
        public async Task<Rsp<ActorView>> GetActor(string id)
        {
            var result = await _repository.GetActorAsync(id);
            return result;
        }

        [HttpGet("actor/{id}/image")]
        public async Task<IActionResult> GetActorImage(string id)
        {
            try
            {
                // Check authentication for this image request
                var authService = HttpContext.RequestServices.GetService(typeof(IAuthenticationService)) as IAuthenticationService;
                if (authService != null)
                {
                    // Check for Authorization header
                    var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                    {
                        var token = authHeader.Substring("Bearer ".Length).Trim();
                        if (!authService.ValidateToken(token))
                        {
                            return Unauthorized();
                        }
                    }
                    // Check for auth cookie
                    else if (HttpContext.Request.Cookies["auth_token"] != null)
                    {
                        var token = HttpContext.Request.Cookies["auth_token"];
                        if (!authService.ValidateToken(token))
                        {
                            return Unauthorized();
                        }
                    }
                    // Check for query parameter
                    else if (!string.IsNullOrEmpty(HttpContext.Request.Query["auth_token"]))
                    {
                        var token = HttpContext.Request.Query["auth_token"];
                        if (!authService.ValidateToken(token))
                        {
                            return Unauthorized();
                        }
                    }
                    else
                    {
                        return Unauthorized();
                    }
                }

                // Get the actor directly from StashApp to get the original image URL
                var actor = await _stashAppService.GetActorAsync(id);
                if (actor?.Preview == null)
                {
                    return NotFound();
                }

                // Extract the original StashApp image URL from the preview field
                // The preview field should contain the original StashApp URL
                var originalImageUrl = actor.Preview;
                
                // If it's already our proxy URL, we need to get the original URL
                // For now, let's construct the original StashApp URL
                var config = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Options.IOptions<PlayaApiV2.Model.StashAppOptions>)) as Microsoft.Extensions.Options.IOptions<PlayaApiV2.Model.StashAppOptions>;
                var stashAppImageUrl = $"{config.Value.Url}/performer/{id}/image";
                
                var httpClient = new System.Net.Http.HttpClient();
                
                // Add the API key header for StashApp authentication
                httpClient.DefaultRequestHeaders.Add("ApiKey", config.Value.ApiKey);
                
                var response = await httpClient.GetAsync(stashAppImageUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";

                // Process and resize the image to fix any corruption and optimize size
                var processedImageData = await ProcessImageAsync(imageBytes, contentType);

                return File(processedImageData, contentType);
            }
            catch
            {
                return NotFound();
            }
        }

        private SortOrder ParseSortOrder(string sortOrder)
        {
            return sortOrder?.ToLower() switch
            {
                "title" => SortOrders.Title,
                "popularity" => SortOrders.Popularity,
                _ => SortOrders.Title
            };
        }

        private SortDirection ParseSortDirection(string sortDirection)
        {
            return sortDirection?.ToLower() switch
            {
                "asc" => SortDirections.Ascending,
                "desc" => SortDirections.Descending,
                _ => SortDirections.Ascending
            };
        }

        private async Task<byte[]> ProcessImageAsync(byte[] imageData, string contentType)
        {
            try
            {
                // Check if this is an SVG image - convert to PNG for PlayaVR compatibility
                if (contentType?.ToLower().Contains("svg") == true || 
                    (imageData.Length > 0 && System.Text.Encoding.UTF8.GetString(imageData, 0, Math.Min(100, imageData.Length)).ToLower().Contains("<svg")))
                {
                    // Convert SVG to PNG using SkiaSharp (cross-platform)
                    var svgString = System.Text.Encoding.UTF8.GetString(imageData);
                    var svg = new SKSvg();
                    var picture = svg.FromSvg(svgString);
                    
                    if (picture == null)
                    {
                        // Return original data if SVG parsing fails
                        return imageData;
                    }
                    
                    // Set default size if not specified
                    var bounds = picture.CullRect;
                    var width = bounds.Width > 0 ? (int)bounds.Width : 800;
                    var height = bounds.Height > 0 ? (int)bounds.Height : 600;
                    
                    // Create bitmap from SVG
                    var info = new SKImageInfo(width, height);
                    using var surface = SKSurface.Create(info);
                    using var canvas = surface.Canvas;
                    canvas.Clear(SKColors.Transparent);
                    canvas.DrawPicture(picture);
                    
                    // Convert to PNG bytes
                    using var image = surface.Snapshot();
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    var pngBytes = data.ToArray();
                    
                    // Now process the PNG with SixLabors.ImageSharp
                    using var svgImage = SixLabors.ImageSharp.Image.Load(pngBytes);
                    
                    // Determine target size based on aspect ratio
                    int svgTargetWidth, svgTargetHeight;
                    if (svgImage.Width > svgImage.Height)
                    {
                        // Wide image (scenes) - resize to max 800px width
                        svgTargetWidth = Math.Min(800, svgImage.Width);
                        svgTargetHeight = (int)((double)svgImage.Height * svgTargetWidth / svgImage.Width);
                    }
                    else
                    {
                        // Tall image (actors) - resize to max 600px height
                        svgTargetHeight = Math.Min(600, svgImage.Height);
                        svgTargetWidth = (int)((double)svgImage.Width * svgTargetHeight / svgImage.Height);
                    }
                    
                    // Resize the image
                    svgImage.Mutate(x => x.Resize(svgTargetWidth, svgTargetHeight, KnownResamplers.Lanczos3));
                    
                    using var outputStream = new MemoryStream();
                    
                    // Save as JPEG with high quality
                    var jpegEncoder = new JpegEncoder
                    {
                        Quality = 90
                    };
                    
                    await svgImage.SaveAsync(outputStream, jpegEncoder);
                    
                    return outputStream.ToArray();
                }
                
                using var rasterImage = SixLabors.ImageSharp.Image.Load(imageData);
                
                // Determine target size based on aspect ratio
                int targetWidth, targetHeight;
                if (rasterImage.Width > rasterImage.Height)
                {
                    // Wide image (scenes) - resize to max 800px width
                    targetWidth = Math.Min(800, rasterImage.Width);
                    targetHeight = (int)((double)rasterImage.Height * targetWidth / rasterImage.Width);
                }
                else
                {
                    // Tall image (actors) - resize to max 600px height
                    targetHeight = Math.Min(600, rasterImage.Height);
                    targetWidth = (int)((double)rasterImage.Width * targetHeight / rasterImage.Height);
                }
                
                // Only resize if the image is larger than target
                if (rasterImage.Width <= targetWidth && rasterImage.Height <= targetHeight)
                {
                    return imageData; // Return original if already small enough
                }
                
                // Resize the image
                rasterImage.Mutate(x => x.Resize(targetWidth, targetHeight, KnownResamplers.Lanczos3));
                
                using var outputStream2 = new MemoryStream();
                
                // Save as JPEG with high quality
                var jpegEncoder2 = new JpegEncoder
                {
                    Quality = 90
                };
                
                await rasterImage.SaveAsync(outputStream2, jpegEncoder2);
                
                return outputStream2.ToArray();
            }
            catch (Exception)
            {
                return imageData; // Return original if processing fails
            }
        }

        private byte[] CreateSvgPlaceholder()
        {
            // Create a simple PNG placeholder for SVG images
            // This is a minimal 1x1 transparent PNG
            return Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
        }
    }
}
