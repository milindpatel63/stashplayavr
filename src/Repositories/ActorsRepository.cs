using PlayaApiV2.Model;
using PlayaApiV2.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlayaApiV2.Repositories
{
    public class ActorsRepository
    {
        private readonly IStashAppService _stashAppService;

        public ActorsRepository(IStashAppService stashAppService)
        {
            _stashAppService = stashAppService;
        }

        public async Task<Page<ActorListView>> GetActorsAsync(ActorsQuery query)
        {
            try
            {
                Console.WriteLine($"ActorsRepository: Received query - PageIndex: {query.PageIndex}, PageSize: {query.PageSize}, SearchTitle: '{query.SearchTitle}'");

                // Get ALL actors from StashApp first to calculate proper pagination
                var allActors = await _stashAppService.GetActorsAsync();

                if (allActors == null)
                {
                    Console.WriteLine("StashApp service returned null actors list");
                    return new Page<ActorListView>(new List<ActorListView>());
                }

                // Apply filters to all actors
                IEnumerable<ActorListView> filteredActors = allActors;

                if (!string.IsNullOrEmpty(query.SearchTitle))
                    filteredActors = filteredActors.Where(a => a.Title.Contains(query.SearchTitle, StringComparison.OrdinalIgnoreCase));

                // Apply sorting
                if (query.SortOrder == SortOrders.Title)
                {
                    if (query.SortDirection == SortDirections.Ascending)
                        filteredActors = filteredActors.OrderBy(a => a.Title, StringComparer.OrdinalIgnoreCase);
                    else
                        filteredActors = filteredActors.OrderByDescending(a => a.Title, StringComparer.OrdinalIgnoreCase);
                }
                else if (query.SortOrder == SortOrders.Popularity)
                {
                    // Sort by rating (from StashApp's rating100 field)
                    if (query.SortDirection == SortDirections.Ascending)
                        filteredActors = filteredActors.OrderBy(a => a.Rating ?? 0);
                    else
                        filteredActors = filteredActors.OrderByDescending(a => a.Rating ?? 0);
                }

                // Convert to list and calculate pagination
                var allActorViews = filteredActors.ToList();

                // Calculate pagination - ensure pageSize is at least 1
                var totalItems = allActorViews.Count;
                var pageSize = Math.Max((int)query.PageSize, 1);
                var pageIndex = Math.Max((int)query.PageIndex, 0);
                var totalPages = totalItems > 0 ? (int)Math.Ceiling((double)totalItems / pageSize) : 1;
                
                // Get the items for the requested page
                var startIndex = pageIndex * pageSize;
                var pageItems = allActorViews.Skip(startIndex).Take(pageSize).ToList();

                Console.WriteLine($"ActorsRepository: Returning page {pageIndex} with {pageItems.Count} actors (total: {totalItems}, pages: {totalPages}, pageSize: {pageSize})");
                
                return new Page<ActorListView>(pageItems)
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    PageTotal = totalPages,
                    ItemTotal = totalItems
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetActorsAsync: {ex.Message}");
                return new Page<ActorListView>(new List<ActorListView>());
            }
        }

        public async Task<ActorView> GetActorAsync(string actorId)
        {
            try
            {
                var actor = await _stashAppService.GetActorAsync(actorId);
                if (actor == null)
                {
                    Console.WriteLine($"Actor {actorId} not found in StashApp");
                    throw GetNotFoundException();
                }
                
                return actor;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetActorAsync: {ex.Message}");
                throw GetNotFoundException();
            }
            
            Exception GetNotFoundException() => new ApiException(ApiStatusCodes.NOT_FOUND, $"Actor '{actorId}' not found");
        }
    }

    public class ActorsQuery
    {
        public long PageIndex { get; set; } = 0;
        public long PageSize { get; set; } = 20;
        public SortOrder SortOrder { get; set; } = SortOrders.Title;
        public SortDirection SortDirection { get; set; } = SortDirections.Ascending;
        public string SearchTitle { get; set; }
    }
}
