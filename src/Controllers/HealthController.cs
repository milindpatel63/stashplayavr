using Microsoft.AspNetCore.Mvc;
using PlayaApiV2.Filters;
using PlayaApiV2.Model;
using PlayaApiV2.Services;
using System;
using System.Threading.Tasks;

namespace PlayaApiV2.Controllers
{
    [Route("api/health")]
    [ApiController]
    [TypeFilter(typeof(ExceptionFilter))]
    public class HealthController : ControllerBase
    {
        private readonly IStashAppService _stashAppService;

        public HealthController(IStashAppService stashAppService)
        {
            _stashAppService = stashAppService;
        }

        [HttpGet]
        public async Task<Rsp<object>> GetHealth()
        {
            try
            {
                var isHealthy = await _stashAppService.IsHealthyAsync();
                
                var healthData = new
                {
                    status = isHealthy ? "healthy" : "unhealthy",
                    stashConnected = isHealthy,
                    timestamp = DateTime.UtcNow.ToString("O")
                };

                return new Rsp<object>(ApiStatus.OK, healthData);
            }
            catch (Exception ex)
            {
                var errorData = new
                {
                    status = "unhealthy",
                    stashConnected = false,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow.ToString("O")
                };

                return new Rsp<object>(ApiStatus.From(ApiStatusCodes.ERROR, ex.Message), errorData);
            }
        }
    }
}
