using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using PlayaApiV2.Model;
using PlayaApiV2.Repositories;
using PlayaApiV2.Services;
using PlayaApiV2.Filters;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure StashApp options
        builder.Services.Configure<StashAppOptions>(
            builder.Configuration.GetSection(StashAppOptions.SectionName));
            
        // Configure App options
        builder.Services.Configure<AppOptions>(
            builder.Configuration.GetSection(AppOptions.SectionName));

        builder.Services
            .AddControllers()
            .AddNewtonsoftJson(o =>
            {
                var serializerSettings = o.SerializerSettings;
                serializerSettings.Formatting = Formatting.None;
                serializerSettings.NullValueHandling = NullValueHandling.Ignore;
                serializerSettings.Converters.Add(new SemVersionConverter());
            });

        // Register HttpClient for StashApp service
        builder.Services.AddHttpClient<IStashAppService, StashAppService>();
        
        // Disable HTTP client logging to reduce console noise
        builder.Logging.AddFilter("System.Net.Http.HttpClient.IStashAppService.LogicalHandler", LogLevel.Warning);
        builder.Logging.AddFilter("System.Net.Http.HttpClient.IStashAppService.ClientHandler", LogLevel.Warning);
        builder.Logging.AddFilter("System.Net.Http.HttpClient.Default.LogicalHandler", LogLevel.Warning);
        builder.Logging.AddFilter("System.Net.Http.HttpClient.Default.ClientHandler", LogLevel.Warning);
        
        // Register HttpContextAccessor for dynamic URL generation
        builder.Services.AddHttpContextAccessor();
        
        // Add memory caching for better performance
        builder.Services.AddMemoryCache();
        
        // Register repositories
        builder.Services.AddScoped<VideosRepository>();
        builder.Services.AddScoped<ActorsRepository>();
        builder.Services.AddScoped<StudiosRepository>();
        
        // Register authentication service
        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
        
        // Register background services
        builder.Services.AddHostedService<BackgroundDataSyncService>();

        // Add response compression for better performance
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        });

        // Add CORS for PlayaVR compatibility
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Enable response compression
        app.UseResponseCompression();

        // Enable CORS
        app.UseCors();

        app.UseRouting();

        app.MapControllers();

        // Get AppOptions to configure host and port
        var appOptions = app.Services.GetRequiredService<IOptions<AppOptions>>().Value;
        var urls = $"http://{appOptions.Host}:{appOptions.Port}";
        
        app.Run(urls);
    }
}
