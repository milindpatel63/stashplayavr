# StashPlayaVR API

A .NET Core API that provides a PlayaVR-compatible interface for StashApp, enabling VR video streaming and management.

## 🚀 Features

- **PlayaVR Compatibility**: Full API compatibility with PlayaVR client applications
- **Video Streaming**: High-performance video streaming with range request support
- **Authentication**: JWT-based authentication with session support
- **Image Processing**: Automatic image resizing and SVG conversion for compatibility
- **Docker Support**: Ready-to-deploy Docker containerization
- **Guest Access**: Support for both authenticated and guest users
- **Background Sync**: Efficient data synchronization with StashApp

## 📋 Prerequisites

- .NET 8.0 SDK
- Docker (optional)
- StashApp instance running and accessible

## 🛠️ Installation

### Option 1: Docker (Recommended)

1. Clone the repository:
```bash
git clone https://github.com/yourusername/stashplayavr-api.git
cd stashplayavr-api
```

2. Copy the example configuration:
```bash
cp src/appsettings.example.json src/appsettings.json
```

3. Edit `src/appsettings.json` with your StashApp details:
```json
{
  "StashApp": {
    "Url": "http://your-stashapp-host:9999",
    "GraphQLUrl": "http://your-stashapp-host:9999/graphql",
    "ApiKey": "your-stash-api-key"
  },
  "JWT": {
    "Secret": "your-jwt-secret-key"
  },
  "Users": {
    "your-username": "your-password"
  }
}
```

4. Build and run with Docker:
```bash
docker build -t stashplayavr-api .
docker run -p 8890:8890 -v $(pwd)/src/appsettings.json:/app/appsettings.json stashplayavr-api
```

### Option 2: Local Development

1. Clone the repository:
```bash
git clone https://github.com/yourusername/stashplayavr-api.git
cd stashplayavr-api
```

2. Copy the example configuration:
```bash
cp src/appsettings.example.json src/appsettings.json
```

3. Edit `src/appsettings.json` with your configuration

4. Restore dependencies and run:
```bash
dotnet restore src
dotnet run --project src
```

## ⚙️ Configuration

### Environment Variables

You can override configuration using environment variables:

```bash
export StashApp__Url="http://your-stashapp:9999"
export StashApp__ApiKey="your-api-key"
export JWT__Secret="your-jwt-secret"
export App__Port="8890"
```

### JWT Secret Generation

Generate a secure JWT secret:

```bash
# Using OpenSSL
openssl rand -base64 64

# Using PowerShell
[System.Web.Security.Membership]::GeneratePassword(64, 0)

# Using Node.js
node -e "console.log(require('crypto').randomBytes(64).toString('base64'))"
```

## 🔧 API Endpoints

### Authentication
- `POST /api/playa/v2/auth/signin` - User authentication
- `POST /api/playa/v2/auth/refresh` - Token refresh

### Videos
- `GET /api/playa/v2/videos` - List videos with pagination
- `GET /api/playa/v2/video/{id}` - Get video details
- `GET /api/playa/v2/video/{id}/stream` - Stream video content
- `GET /api/playa/v2/video/{id}/poster` - Get video poster
- `GET /api/playa/v2/video/{id}/preview` - Get video preview

### Categories & Metadata
- `GET /api/playa/v2/categories` - List categories/tags
- `GET /api/playa/v2/actors` - List actors/performers
- `GET /api/playa/v2/studios` - List studios

### System
- `GET /api/playa/v2/version` - API version
- `GET /api/playa/v2/configuration` - Client configuration
- `GET /api/playa/v2/health` - Health check

## 🔐 Authentication

The API supports multiple authentication methods:

1. **JWT Bearer Token**: Standard Authorization header
2. **Cookie Authentication**: Session-based cookies
3. **Query Parameter**: `?auth_token=...` for direct URLs
4. **Guest Access**: Limited access without authentication

### Guest Access

Some endpoints support guest access with limited functionality:
- Categories return a dummy "Free" category
- Videos return empty results
- Posters and sprites work without authentication

## 🐳 Docker Deployment

### Docker Compose

Create a `docker-compose.yml`:

```yaml
version: '3.8'
services:
  stashplayavr-api:
    image: yourusername/stashplayavr-api:latest
    ports:
      - "8890:8890"
    environment:
      - StashApp__Url=http://your-stashapp:9999
      - StashApp__ApiKey=your-api-key
      - JWT__Secret=your-jwt-secret
    restart: unless-stopped
```

### Health Checks

The Docker image includes health checks:
```bash
docker run --health-cmd="curl -f http://localhost:8890/api/playa/v2/health || exit 1" yourimage
```

## 🔧 Known Issues & Workarounds

### PlayaVR Steam Client Categories Issue

**Issue**: Categories may not appear for logged-in users on PlayaVR Steam client.

**Workaround**: After logging in, restart the PlayaVR application to see all categories.

**Technical Details**: The PlayaVR Steam client has inconsistent authentication behavior - it only requests categories once during initial startup (when not logged in) and doesn't re-request them after login. The API includes special handling to detect PlayaVR clients and return appropriate category data based on authentication status.

## 🛠️ Development

### Project Structure

```
src/
├── Controllers/          # API controllers
├── Services/            # Business logic services
├── Repositories/        # Data access layer
├── Filters/            # Action filters (auth, exception handling)
├── Models/             # Data models
└── Program.cs          # Application entry point
```

### Building

```bash
# Build the project
dotnet build src

# Run tests
dotnet test

# Publish for production
dotnet publish src -c Release -o ./publish
```

## 📝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [StashApp](https://github.com/stashapp/stash) - The amazing media management system
- [PlayaVR](https://github.com/playavr/playavr) - VR video player client
- .NET Core community for excellent tooling and documentation

## 📞 Support

If you encounter any issues or have questions:

1. Check the [Issues](https://github.com/yourusername/stashplayavr-api/issues) page
2. Create a new issue with detailed information
3. Include logs and configuration (remove sensitive data)

## 🔄 Changelog

### v1.0.0
- Initial release
- Full PlayaVR API compatibility
- JWT authentication support
- Docker containerization
- Image processing and SVG conversion
- Guest access support