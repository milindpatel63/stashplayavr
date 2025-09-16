using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using PlayaApiV2.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlayaApiV2.Services
{
    public class BackgroundDataSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundDataSyncService> _logger;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(5); // Sync every 5 minutes

        public BackgroundDataSyncService(
            IServiceProvider serviceProvider,
            ILogger<BackgroundDataSyncService> logger,
            IMemoryCache cache)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Data Sync Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformDataSync();
                    _logger.LogInformation("Background data sync completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during background data sync");
                }

                await Task.Delay(_syncInterval, stoppingToken);
            }

            _logger.LogInformation("Background Data Sync Service stopped");
        }

        private async Task PerformDataSync()
        {
            using var scope = _serviceProvider.CreateScope();
            var stashAppService = scope.ServiceProvider.GetRequiredService<IStashAppService>();

            // Pre-fetch and cache the most commonly accessed data
            await PreFetchVideoData(stashAppService);
            await PreFetchActorData(stashAppService);
            await PreFetchStudioData(stashAppService);
            await PreFetchCategoryData(stashAppService);
        }

        private async Task PreFetchVideoData(IStashAppService stashAppService)
        {
            try
            {
                _logger.LogInformation("Pre-fetching video data...");
                
                // Pre-fetch first few pages of videos (most commonly accessed)
                for (int page = 1; page <= 5; page++)
                {
                    var videos = await stashAppService.GetVideosAsync(page, 100);
                    _logger.LogInformation($"Pre-fetched {videos.Count} videos from page {page}");
                }

                // Pre-fetch video details for most popular videos (first 50)
                var popularVideos = await stashAppService.GetVideosAsync(1, 50);
                foreach (var video in popularVideos.Take(20)) // Pre-fetch details for first 20
                {
                    try
                    {
                        await stashAppService.GetVideoAsync(video.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to pre-fetch details for video {video.Id}");
                    }
                }

                _logger.LogInformation("Video data pre-fetch completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pre-fetching video data");
            }
        }

        private async Task PreFetchActorData(IStashAppService stashAppService)
        {
            try
            {
                _logger.LogInformation("Pre-fetching actor data...");
                
                // Pre-fetch actors
                var actors = await stashAppService.GetActorsAsync();
                _logger.LogInformation($"Pre-fetched {actors.Count} actors");

                _logger.LogInformation("Actor data pre-fetch completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pre-fetching actor data");
            }
        }

        private async Task PreFetchStudioData(IStashAppService stashAppService)
        {
            try
            {
                _logger.LogInformation("Pre-fetching studio data...");
                
                // Pre-fetch studios
                var studios = await stashAppService.GetStudiosAsync();
                _logger.LogInformation($"Pre-fetched {studios.Count} studios");

                _logger.LogInformation("Studio data pre-fetch completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pre-fetching studio data");
            }
        }

        private async Task PreFetchCategoryData(IStashAppService stashAppService)
        {
            try
            {
                _logger.LogInformation("Pre-fetching category data...");
                
                var categories = await stashAppService.GetCategoriesAsync();
                _logger.LogInformation($"Pre-fetched {categories.Count} categories");

                _logger.LogInformation("Category data pre-fetch completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pre-fetching category data");
            }
        }
    }
}
