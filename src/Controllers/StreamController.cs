using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PlayaApiV2.Filters;
using PlayaApiV2.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using Svg.Skia;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PlayaApiV2.Controllers
{
    [Route("api/playa/v2")]
    [ApiController]
    [TypeFilter(typeof(ExceptionFilter))]
    public class StreamController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly StashAppOptions _options;

        public StreamController(HttpClient httpClient, IOptions<StashAppOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        [HttpGet("video/{videoId}/stream")]
        [HttpPost("video/{videoId}/stream")]
        [HttpPut("video/{videoId}/stream")]
        [HttpDelete("video/{videoId}/stream")]
        [HttpHead("video/{videoId}/stream")]
        public async Task<IActionResult> StreamVideo([FromRoute(Name = "videoId")] string videoId)
        {
            try
            {
                // Check if this is a HEAD request (for file size/metadata)
                var isHeadRequest = Request.Method == "HEAD";
                
                // Check if this is a download request
                // Downloads are GET requests WITHOUT Range headers (want full file)
                // Streaming are GET requests WITH Range headers (want partial content)
                var hasRangeHeader = Request.Headers.ContainsKey("Range");
                var isDownload = Request.Method == "GET" && !hasRangeHeader && !isHeadRequest;
                
                var streamUrl = $"{_options.Url}/scene/{videoId}/stream";
                
                // Get the video stream from StashApp
                // For HEAD requests, use GET with range 0-0 to get just headers
                var request = new HttpRequestMessage(HttpMethod.Get, streamUrl);
                request.Headers.Add("ApiKey", _options.ApiKey);
                
                if (isHeadRequest)
                {
                    request.Headers.Add("Range", "bytes=0-0");
                }
                
                // Handle range requests for video streaming (but not for downloads)
                if (!isDownload && Request.Headers.ContainsKey("Range"))
                {
                    var range = Request.Headers["Range"].ToString();
                    request.Headers.Add("Range", range);
                }

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                // Set appropriate headers for video streaming/downloading
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "video/mp4";
                var contentLength = response.Content.Headers.ContentLength;
                
                Response.Headers["Cache-Control"] = "public, max-age=3600";
                Response.Headers["Access-Control-Allow-Origin"] = "*";
                
                if (contentLength.HasValue)
                {
                    Response.Headers["Content-Length"] = contentLength.Value.ToString();
                }

                if (isHeadRequest)
                {
                    // For HEAD requests, return just headers without body
                    Response.Headers["Accept-Ranges"] = "bytes";
                    
                    // Extract full file size from Content-Range header if available
                    if (response.Content.Headers.Contains("Content-Range"))
                    {
                        var contentRange = response.Content.Headers.GetValues("Content-Range").FirstOrDefault();
                        if (!string.IsNullOrEmpty(contentRange) && contentRange.Contains("/"))
                        {
                            var fullSize = contentRange.Split('/')[1];
                            if (long.TryParse(fullSize, out var size))
                            {
                                Response.Headers["Content-Length"] = size.ToString();
                            }
                        }
                    }
                    
                    return new EmptyResult();
                }
                else if (isDownload)
                {
                    // For downloads, set download headers
                    Response.Headers["Content-Disposition"] = $"attachment; filename=video_{videoId}.mp4";
                    
                    // Copy response headers from StashApp
                    foreach (var header in response.Content.Headers)
                    {
                        if (header.Key != "Content-Disposition") // Don't override our download header
                        {
                            Response.Headers[header.Key] = header.Value.ToArray();
                        }
                    }
                    
                    // Stream the content directly from StashApp to client
                    var sourceStream = await response.Content.ReadAsStreamAsync();
                    await sourceStream.CopyToAsync(Response.Body);
                    
                    return new EmptyResult();
                }
                else
                {
                    // For streaming, handle range requests
                    Response.Headers["Accept-Ranges"] = "bytes";
                    
                    // Handle partial content responses - copy headers from StashApp response
                    if (response.StatusCode == System.Net.HttpStatusCode.PartialContent)
                    {
                        Response.StatusCode = 206;
                        
                        // Copy Content-Range header from StashApp response
                        if (response.Content.Headers.Contains("Content-Range"))
                        {
                            var contentRange = response.Content.Headers.GetValues("Content-Range").FirstOrDefault();
                            Response.Headers["Content-Range"] = contentRange;
                        }
                    }

                    var stream = await response.Content.ReadAsStreamAsync();
                    return new FileStreamResult(stream, contentType)
                    {
                        EnableRangeProcessing = true
                    };
                }
            }
            catch (Exception)
            {
                return StatusCode(500, "Error streaming video");
            }
        }

        [HttpGet("video/{videoId}/download")]
        [HttpPost("video/{videoId}/download")]
        [HttpPut("video/{videoId}/download")]
        [HttpHead("video/{videoId}/download")]
        public async Task<IActionResult> DownloadVideo([FromRoute(Name = "videoId")] string videoId)
        {
            try
            {
                
                var downloadUrl = $"{_options.Url}/scene/{videoId}/stream";
                
                // Check if this is a HEAD request (for file size/metadata)
                var isHeadRequest = Request.Method == "HEAD";
                
                // Get the video file from StashApp
                var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                request.Headers.Add("ApiKey", _options.ApiKey);
                
                if (isHeadRequest)
                {
                    request.Headers.Add("Range", "bytes=0-0");
                }

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                // Set appropriate headers for video download
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "video/mp4";
                var contentLength = response.Content.Headers.ContentLength;
                
                
                // Set headers for download
                Response.Headers["Cache-Control"] = "public, max-age=3600";
                Response.Headers["Access-Control-Allow-Origin"] = "*";
                Response.Headers["Content-Disposition"] = $"attachment; filename=video_{videoId}.mp4";
                
                if (isHeadRequest)
                {
                    // For HEAD requests, extract full file size from Content-Range header
                    if (response.Content.Headers.Contains("Content-Range"))
                    {
                        var contentRange = response.Content.Headers.GetValues("Content-Range").FirstOrDefault();
                        if (!string.IsNullOrEmpty(contentRange) && contentRange.Contains("/"))
                        {
                            var fullSize = contentRange.Split('/')[1];
                            if (long.TryParse(fullSize, out var size))
                            {
                                Response.Headers["Content-Length"] = size.ToString();
                            }
                        }
                    }
                    return new EmptyResult();
                }
                
                if (contentLength.HasValue)
                {
                    Response.Headers["Content-Length"] = contentLength.Value.ToString();
                }

                // For downloads, stream the content without loading it all into memory
                var stream = await response.Content.ReadAsStreamAsync();
                return new FileStreamResult(stream, contentType)
                {
                    FileDownloadName = $"video_{videoId}.mp4"
                };
            }
            catch (Exception)
            {
                return StatusCode(500, $"Error downloading video: ");
            }
        }

        [HttpGet("video/{videoId}/poster")]
        public async Task<IActionResult> GetVideoPoster([FromRoute(Name = "videoId")] string videoId)
        {
            try
            {
                var thumbnailUrl = $"{_options.Url}/scene/{videoId}/screenshot";
                
                var request = new HttpRequestMessage(HttpMethod.Get, thumbnailUrl);
                request.Headers.Add("ApiKey", _options.ApiKey);

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
                var imageData = await response.Content.ReadAsByteArrayAsync();
                
                // Process and resize the image to fix any corruption and optimize size
                var processedImageData = await ProcessImageAsync(imageData, contentType);
                
                Response.Headers["Cache-Control"] = "public, max-age=3600";
                Response.Headers["Access-Control-Allow-Origin"] = "*";
                
                return File(processedImageData, contentType);
            }
            catch (Exception)
            {
                return StatusCode(500, $"Error getting poster: ");
            }
        }

        [HttpGet("sprite-proxy/{*path}")]
        public async Task<IActionResult> ProxySpriteRequest(string path)
        {
            try
            {
                // Construct the full StashApp sprite URL
                var stashAppUrl = $"{_options.Url.TrimEnd('/')}/{path}";
                
                // Create request to StashApp with API key
                var request = new HttpRequestMessage(HttpMethod.Get, stashAppUrl);
                request.Headers.Add("ApiKey", _options.ApiKey);

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode);
                }

                var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
                var data = await response.Content.ReadAsByteArrayAsync();
                
                // Set appropriate headers for sprites
                Response.Headers["Cache-Control"] = "public, max-age=3600";
                Response.Headers["Access-Control-Allow-Origin"] = "*";
                
                return File(data, contentType);
            }
            catch (Exception)
            {
                return StatusCode(500, $"Error proxying sprite: ");
            }
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

        [HttpGet("video/{videoId}/preview")]
        [HttpPost("video/{videoId}/preview")]
        [HttpPut("video/{videoId}/preview")]
        [HttpDelete("video/{videoId}/preview")]
        [HttpHead("video/{videoId}/preview")]
        public async Task<IActionResult> GetVideoPreview([FromRoute(Name = "videoId")] string videoId)
        {
            try
            {
                var previewUrl = $"{_options.Url}/scene/{videoId}/preview";
                
                // Get the preview from StashApp
                var request = new HttpRequestMessage(HttpMethod.Get, previewUrl);
                request.Headers.Add("ApiKey", _options.ApiKey);
                
                // Handle range requests for preview streaming
                if (Request.Headers.ContainsKey("Range"))
                {
                    var range = Request.Headers["Range"].ToString();
                    request.Headers.Add("Range", range);
                }

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                // Set appropriate headers for preview streaming
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "video/mp4";
                var contentLength = response.Content.Headers.ContentLength;
                
                
                Response.Headers["Accept-Ranges"] = "bytes";
                Response.Headers["Cache-Control"] = "public, max-age=3600";
                Response.Headers["Access-Control-Allow-Origin"] = "*";
                
                if (contentLength.HasValue)
                {
                    Response.Headers["Content-Length"] = contentLength.Value.ToString();
                }

                // Handle partial content responses - copy headers from StashApp response
                if (response.StatusCode == System.Net.HttpStatusCode.PartialContent)
                {
                    Response.StatusCode = 206;
                    
                    // Copy Content-Range header from StashApp response
                    if (response.Content.Headers.Contains("Content-Range"))
                    {
                        var contentRange = response.Content.Headers.GetValues("Content-Range").FirstOrDefault();
                        Response.Headers["Content-Range"] = contentRange;
                    }
                }

                var stream = await response.Content.ReadAsStreamAsync();
                return new FileStreamResult(stream, contentType)
                {
                    EnableRangeProcessing = true
                };
            }
            catch (Exception)
            {
                return StatusCode(500, $"Error getting preview: ");
            }
        }

        // Catch-all endpoint to log any unmatched requests
        [Route("{*path}")]
        public IActionResult CatchAll(string path)
        {
            
            return NotFound($"Endpoint not found: {Request.Method} {Request.Path}");
        }

        private byte[] CreateSvgPlaceholder()
        {
            // Create a simple PNG placeholder for SVG images
            // This is a minimal 1x1 transparent PNG
            return Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
        }
    }
}
