using PlayaApiV2.Model;
using PlayaApiV2.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlayaApiV2.Repositories
{
    public class StudiosRepository
    {
        private readonly IStashAppService _stashAppService;

        public StudiosRepository(IStashAppService stashAppService)
        {
            _stashAppService = stashAppService;
        }

        public async Task<Page<StudioListView>> GetStudiosAsync(StudiosQuery query)
        {
            try
            {
                Console.WriteLine($"StudiosRepository: Received query - PageIndex: {query.PageIndex}, PageSize: {query.PageSize}, SearchTitle: '{query.SearchTitle}'");

                // Get ALL studios from StashApp first to calculate proper pagination
                var allStudios = await _stashAppService.GetStudiosAsync();

                if (allStudios == null)
                {
                    Console.WriteLine("StashApp service returned null studios list");
                    return new Page<StudioListView>(new List<StudioListView>());
                }

                // Apply filters to all studios
                IEnumerable<StudioListView> filteredStudios = allStudios;

                if (!string.IsNullOrEmpty(query.SearchTitle))
                    filteredStudios = filteredStudios.Where(s => s.Title.Contains(query.SearchTitle, StringComparison.OrdinalIgnoreCase));

                // Apply sorting
                if (query.SortOrder == SortOrders.Title)
                {
                    if (query.SortDirection == SortDirections.Ascending)
                        filteredStudios = filteredStudios.OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase);
                    else
                        filteredStudios = filteredStudios.OrderByDescending(s => s.Title, StringComparer.OrdinalIgnoreCase);
                }
                else if (query.SortOrder == SortOrders.Title)
                {
                    // No popularity data available, sort by title
                    filteredStudios = filteredStudios.OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase);
                }

                // Convert to list and calculate pagination
                var allStudioViews = filteredStudios.ToList();

                // Calculate pagination - ensure pageSize is at least 1
                var totalItems = allStudioViews.Count;
                var pageSize = Math.Max((int)query.PageSize, 1);
                var pageIndex = Math.Max((int)query.PageIndex, 0);
                var totalPages = totalItems > 0 ? (int)Math.Ceiling((double)totalItems / pageSize) : 1;
                
                // Get the items for the requested page
                var startIndex = pageIndex * pageSize;
                var pageItems = allStudioViews.Skip(startIndex).Take(pageSize).ToList();

                Console.WriteLine($"StudiosRepository: Returning page {pageIndex} with {pageItems.Count} studios (total: {totalItems}, pages: {totalPages}, pageSize: {pageSize})");
                
                return new Page<StudioListView>(pageItems)
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    PageTotal = totalPages,
                    ItemTotal = totalItems
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStudiosAsync: {ex.Message}");
                return new Page<StudioListView>(new List<StudioListView>());
            }
        }

        public async Task<StudioView> GetStudioAsync(string studioId)
        {
            try
            {
                var studio = await _stashAppService.GetStudioAsync(studioId);
                if (studio == null)
                {
                    Console.WriteLine($"Studio {studioId} not found in StashApp");
                    throw GetNotFoundException();
                }
                
                return studio;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStudioAsync: {ex.Message}");
                throw GetNotFoundException();
            }
            
            Exception GetNotFoundException() => new ApiException(ApiStatusCodes.NOT_FOUND, $"Studio '{studioId}' not found");
        }
    }

    public class StudiosQuery
    {
        public long PageIndex { get; set; } = 0;
        public long PageSize { get; set; } = 20;
        public SortOrder SortOrder { get; set; } = SortOrders.Title;
        public SortDirection SortDirection { get; set; } = SortDirections.Ascending;
        public string SearchTitle { get; set; }
    }
}
