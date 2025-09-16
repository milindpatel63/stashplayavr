# StashApp-PlayaVR Bridge

This is an enhanced version of the PlayaVR reference API that bridges StashApp content to the PlayaVR application. Instead of serving hardcoded sample data, this implementation fetches real video content from your StashApp instance via GraphQL.

## Features

- **Real StashApp Integration**: Fetches videos, categories, and metadata from your StashApp database
- **Video Streaming**: Proxies video streams from StashApp with proper authentication
- **Thumbnail Support**: Serves video thumbnails/posters through the bridge API
- **PlayaVR v2 Compatible**: Implements the complete PlayaVR v2 API specification
- **Health Monitoring**: Includes health check endpoints to monitor StashApp connectivity

## Configuration

Update the `appsettings.json` file with your StashApp connection details:

```json
{
  "StashApp": {
    "Url": "http://your-stash-server:9999",
    "GraphQLUrl": "http://your-stash-server:9999/graphql",
    "ApiKey": "your-stash-api-key"
  }
}
```

### Getting Your StashApp API Key

1. Open your StashApp web interface
2. Go to Settings → Security
3. Generate a new API Key
4. Copy the JWT token and use it in the configuration

## Running the Bridge

1. Update `appsettings.json` with your StashApp details
2. Build and run the project:
   ```bash
   dotnet build
   dotnet run
   ```
3. The bridge will start on `https://localhost:5001` (or `http://localhost:5000`)

## Connecting PlayaVR

1. Open the PlayaVR application
2. Go to the Web tab
3. Click "Add Website"
4. Enter your bridge URL: `localhost:5001` (or your server IP)
5. PlayaVR will connect and display your StashApp videos

## API Endpoints

### PlayaVR v2 API
- `GET /api/playa/v2/version` - API version information
- `GET /api/playa/v2/config` - Site configuration
- `GET /api/playa/v2/videos` - List videos with pagination and filtering
- `GET /api/playa/v2/video/{id}` - Get video details
- `GET /api/playa/v2/categories` - List categories (from StashApp tags)
- `GET /api/playa/v2/video/{id}/stream` - Stream video content
- `GET /api/playa/v2/video/{id}/poster` - Get video thumbnail

### Health Check
- `GET /api/health` - Check StashApp connectivity

## Video Quality Mapping

The bridge automatically maps StashApp video resolutions to PlayaVR quality names:
- 2160p+ → "4K"
- 1440p → "2K" 
- 1080p → "1080p"
- 720p → "720p"
- 480p → "480p"
- Lower → "SD"

## VR Content Detection

Currently, all videos are served as flat content. To properly detect VR videos, you can:
1. Tag VR content in StashApp with specific tags (VR, 360, 180, etc.)
2. Modify the `ConvertSceneToVideoView` method to detect VR tags and set appropriate projection/stereo modes

## Troubleshooting

### Connection Issues
- Verify StashApp is running and accessible
- Check the API key is valid and has proper permissions
- Ensure firewall allows connections between the bridge and StashApp

### Video Streaming Issues
- Verify video files are accessible to StashApp
- Check that the bridge server can reach StashApp's streaming endpoints
- Monitor the health check endpoint for connectivity status

### PlayaVR Connection Issues
- Ensure the bridge is accessible from your PlayaVR device
- Check CORS is properly configured
- Use the correct protocol (http/https) when adding the website

## Development Notes

This implementation uses:
- ASP.NET Core for the web API
- Newtonsoft.Json for JSON serialization
- HttpClient for StashApp communication
- PlayaVR v2 API specification

The code is structured to be easily extensible for additional features like authentication, advanced filtering, or VR content detection.
