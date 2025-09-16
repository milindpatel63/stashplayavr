using PlayaApiV2.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlayaApiV2.Services
{
    public interface IStashAppService
    {
        Task<List<VideoView>> GetVideosAsync(int page = 1, int perPage = 50);
        Task<List<VideoView>> GetVideosByActorAsync(string actorId, int page = 1, int perPage = 50);
        Task<VideoView> GetVideoAsync(string videoId);
        Task<List<CategoryListView>> GetCategoriesAsync();
        Task<List<ActorListView>> GetActorsAsync();
        Task<ActorView> GetActorAsync(string actorId);
        Task<List<StudioListView>> GetStudiosAsync();
        Task<StudioView> GetStudioAsync(string studioId);
        Task<List<VideoView>> GetVideosByStudioAsync(string studioId, int page = 1, int perPage = 50);
        Task<bool> IsHealthyAsync();
    }
}
