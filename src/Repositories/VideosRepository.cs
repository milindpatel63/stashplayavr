using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PlayaApiV2.Extensions;
using PlayaApiV2.Model;
using PlayaApiV2.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlayaApiV2.Repositories
{
    public class VideosRepository
    {
        private readonly IStashAppService _stashAppService;
        private readonly AppOptions _appOptions;

        public VideosRepository(IStashAppService stashAppService, IOptions<AppOptions> appOptions)
        {
            _stashAppService = stashAppService;
            _appOptions = appOptions.Value;
        }

        public async Task<Page<VideoListView>> GetVideosAsync(VideosQuery query)
        {
            try
            {
                List<VideoView> allVideos;

                // If ActorId is specified, get videos for that specific actor
                if (!string.IsNullOrEmpty(query.ActorId))
                {
                    Console.WriteLine($"Getting videos for actor: {query.ActorId}");
                    allVideos = await _stashAppService.GetVideosByActorAsync(query.ActorId, page: 1, perPage: 10000);
                }
                // If StudioId is specified, get videos for that specific studio
                else if (!string.IsNullOrEmpty(query.StudioId))
                {
                    Console.WriteLine($"Getting videos for studio: {query.StudioId}");
                    allVideos = await _stashAppService.GetVideosByStudioAsync(query.StudioId, page: 1, perPage: 10000);
                }
                else
                {
                    // Get ALL videos from StashApp first to calculate proper pagination
                    allVideos = await _stashAppService.GetVideosAsync(page: 1, perPage: 10000); // Get all videos
                }

                if (allVideos == null)
                {
                    Console.WriteLine("StashApp service returned null videos list");
                    return new Page<VideoListView>(new List<VideoListView>());
                }

                // Apply filters to all videos
                IEnumerable<VideoView> filteredVideos = allVideos;

                if (!string.IsNullOrEmpty(query.SearchTitle))
                    filteredVideos = filteredVideos.Where(v => v.Title.Contains(query.SearchTitle, StringComparison.OrdinalIgnoreCase));
                
                if (!query.ExcludedCategories.IsNullOrEmpty())
                    filteredVideos = filteredVideos.Where(e => !query.ExcludedCategories.Overlaps(e.Categories.OrEmpty().Select(c => c.Id)));
                
                if (!query.IncludedCategories.IsNullOrEmpty())
                    filteredVideos = filteredVideos.Where(e => query.IncludedCategories.IsSubsetOf(e.Categories.OrEmpty().Select(c => c.Id)));
                
                // Apply sorting
                if (query.SortOrder == SortOrders.Title)
                    filteredVideos = OrderBy(filteredVideos, e => e.Title, query.SortDirection, s_naturalComparer);
                if (query.SortOrder == SortOrders.ReleaseDate)
                    filteredVideos = OrderBy(filteredVideos, e => e.ReleaseDate.GetValueOrDefault(), query.SortDirection);
                if (query.SortOrder == SortOrders.Popularity)
                {
                    if (_appOptions.SortPopularityByRating)
                        filteredVideos = OrderBy(filteredVideos, e => e.Rating.GetValueOrDefault(), query.SortDirection);
                    else
                        filteredVideos = OrderBy(filteredVideos, e => e.OCount.GetValueOrDefault(), query.SortDirection);
                }

                // Convert to list and calculate pagination
                var allVideoViews = filteredVideos.Select(e => new VideoListView
                {
                    Id = e.Id,
                    Title = e.Title,
                    Subtitle = e.Subtitle,
                    Preview = e.Preview,
                    ReleaseDate = e.ReleaseDate,
                    Details = e.Details?.Select(d => new VideoListView.VideoDetails
                    {
                        Type = d.Type,
                        DurationSeconds = d.DurationSeconds,
                    }).ToList(),
                }).ToList();

                // Calculate pagination
                var totalItems = allVideoViews.Count;
                var pageSize = (int)query.PageSize;
                var pageIndex = (int)query.PageIndex;
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                
                // Get the items for the requested page
                var startIndex = pageIndex * pageSize;
                var pageItems = allVideoViews.Skip(startIndex).Take(pageSize).ToList();

                Console.WriteLine($"Returning page {pageIndex} with {pageItems.Count} videos (total: {totalItems}, pages: {totalPages})");
                
                return new Page<VideoListView>(pageItems)
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    PageTotal = totalPages,
                    ItemTotal = totalItems
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetVideosAsync: {ex.Message}");
                return new Page<VideoListView>(new List<VideoListView>());
            }
        }

        public async Task<VideoView> GetVideoAsync(string videoId)
        {
            try
            {
                var video = await _stashAppService.GetVideoAsync(videoId);
                if (video == null)
                {
                    Console.WriteLine($"Video {videoId} not found in StashApp");
                    throw GetNotFoundException();
                }
                
                return video;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetVideoAsync: {ex.Message}");
                throw GetNotFoundException();
            }
            
            Exception GetNotFoundException() => new ApiException(ApiStatusCodes.NOT_FOUND, $"Video '{videoId}' not found");
        }

        public async Task<List<CategoryListView>> GetCategoriesAsync(bool onlyPublished = true)
        {
            try
            {
                var categories = await _stashAppService.GetCategoriesAsync();
                if (categories == null)
                {
                    Console.WriteLine("StashApp service returned null categories list");
                    return new List<CategoryListView>();
                }
                
                Console.WriteLine($"Returning {categories.Count} categories to PlayaVR");
                return categories;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCategoriesAsync: {ex.Message}");
                return new List<CategoryListView>();
            }
        }

        private static readonly NaturalComparer s_naturalComparer = new NaturalComparer();

        private static IEnumerable<T> OrderBy<T, TKey>(IEnumerable<T> source, Func<T, TKey> keySelector, SortDirection direction, IComparer<TKey> comparer = null)
        {
            if (direction == SortDirections.Ascending)
                return source.OrderBy(keySelector, comparer);
            else
                return source.OrderByDescending(keySelector, comparer);
        }

    }

    public class VideosQuery : IEquatable<VideosQuery>
    {
        public long PageIndex { get; set; }
        public long PageSize { get; set; }

        public SortOrder SortOrder { get; set; }
        public SortDirection SortDirection { get; set; }

        public string SearchTitle { get; set; }
        public string StudioId { get; set; }
        public string ActorId { get; set; }

        /// <summary>
        /// All
        /// </summary>
        public HashSet<string> IncludedCategories { get; } = new HashSet<string>();

        /// <summary>
        /// None
        /// </summary>
        public HashSet<string> ExcludedCategories { get; } = new HashSet<string>();

        public void AddIncludedCategories(IEnumerable<string> categories) => AddRange(categories, IncludedCategories);
        public void AddExcludedCategories(IEnumerable<string> categories) => AddRange(categories, ExcludedCategories);

        private void AddRange<T>(IEnumerable<T> from, HashSet<T> to)
        {
            if (from == null)
                return;

            foreach (var category in from)
                to.Add(category);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as VideosQuery);
        }

        public bool Equals(VideosQuery other)
        {
            return other is not null &&
                   PageIndex == other.PageIndex &&
                   PageSize == other.PageSize &&
                   SortOrder.Equals(other.SortOrder) &&
                   SortDirection.Equals(other.SortDirection) &&
                   SearchTitle == other.SearchTitle &&
                   StudioId == other.StudioId &&
                   ActorId == other.ActorId &&
                   IncludedCategories.SetEquals(other.IncludedCategories) &&
                   ExcludedCategories.SetEquals(other.ExcludedCategories);
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(PageIndex);
            hash.Add(PageSize);
            hash.Add(SortOrder);
            hash.Add(SortDirection);
            hash.Add(SearchTitle);
            hash.Add(StudioId);
            hash.Add(ActorId);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static bool operator ==(VideosQuery left, VideosQuery right)
        {
            return EqualityComparer<VideosQuery>.Default.Equals(left, right);
        }

        public static bool operator !=(VideosQuery left, VideosQuery right)
        {
            return !(left == right);
        }
    }
}
