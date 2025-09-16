# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- GitHub Actions CI/CD workflow
- Comprehensive documentation
- Example configuration files
- Docker Compose example
- Contributing guidelines
- Code of conduct

### Changed
- Removed all debug logging for production readiness
- Cleaned up sensitive data from configuration examples

## [1.0.0] - 2025-01-19

### Added
- Initial release of StashPlayaVR API
- Full PlayaVR API compatibility
- JWT-based authentication system
- Session-based authentication for streaming
- Guest access support
- Video streaming with range request support
- Image processing and resizing
- SVG to raster conversion for compatibility
- Docker containerization
- Background data synchronization with StashApp
- Comprehensive error handling
- Health check endpoints
- Multi-user authentication support
- Configuration-based user management

### Features
- **Authentication**: JWT tokens with refresh mechanism
- **Streaming**: High-performance video streaming
- **Images**: Automatic image processing and optimization
- **Guest Mode**: Limited access without authentication
- **Docker**: Ready-to-deploy containerization
- **Background Sync**: Efficient data synchronization
- **Multi-format Support**: Handles various video and image formats

### API Endpoints
- Authentication endpoints (`/auth/signin`, `/auth/refresh`)
- Video management (`/videos`, `/video/{id}`)
- Streaming (`/video/{id}/stream`)
- Media (`/video/{id}/poster`, `/video/{id}/preview`)
- Metadata (`/categories`, `/actors`, `/studios`)
- System (`/version`, `/configuration`, `/health`)

### Configuration
- Environment variable support
- Docker Compose integration
- Flexible user management
- Customizable JWT settings
- StashApp integration settings

### Known Issues
- PlayaVR Steam client categories issue (workaround: restart app after login)

## [0.9.0] - 2025-01-18

### Added
- Basic API structure
- StashApp integration
- Initial authentication system
- Video streaming capabilities
- Image processing foundation

### Changed
- Multiple iterations of authentication implementation
- Various streaming optimizations
- Image processing improvements

## [0.8.0] - 2025-01-17

### Added
- Docker support
- Background data sync
- SVG conversion capabilities
- Enhanced error handling

### Fixed
- Various streaming issues
- Authentication edge cases
- Image processing bugs

## [0.7.0] - 2025-01-16

### Added
- Guest access implementation
- Enhanced authentication filters
- Improved error handling

### Fixed
- Categories display issues
- Authentication flow problems
- Streaming authentication

## [0.6.0] - 2025-01-15

### Added
- JWT authentication system
- User management
- Session-based authentication

### Changed
- Complete authentication rewrite
- Improved security model

## [0.5.0] - 2025-01-14

### Added
- Image processing and resizing
- Poster and preview support
- Sprite proxy functionality

### Fixed
- Various image-related bugs
- Performance optimizations

## [0.4.0] - 2025-01-13

### Added
- Video streaming implementation
- Range request support
- Download functionality

### Fixed
- Streaming performance issues
- Download problems

## [0.3.0] - 2025-01-12

### Added
- Basic API endpoints
- StashApp GraphQL integration
- Data models and repositories

## [0.2.0] - 2025-01-11

### Added
- Project structure
- Basic configuration
- Initial development setup

## [0.1.0] - 2025-01-10

### Added
- Initial project creation
- Basic .NET Core setup
- Project configuration
