# Configuration Options

This document describes the configuration options for the PlayaVR API.

## Configuration Options

### 1. ShowAllActors

**Default**: `false`  
**Description**: Controls whether to show all actors or only those with at least 1 scene.

- `false` (default): Only shows actors that have at least 1 scene (current behavior)
- `true`: Shows all actors regardless of scene count

### 2. SortPopularityByRating

**Default**: `false`  
**Description**: Controls how popularity sorting works for videos.

- `false` (default): Sort by `o_counter` (view count) for popularity
- `true`: Sort by `rating100` (rating) for popularity

### 3. Host

**Default**: `"0.0.0.0"`  
**Description**: Host address for the application to bind to.

- `"0.0.0.0"` (default): Bind to all interfaces (accessible from any IP)
- `"localhost"` or `"127.0.0.1"`: Only accessible from localhost
- `"192.168.1.100"`: Bind to specific IP address

### 4. Port

**Default**: `8890`  
**Description**: Port number for the application to listen on.

- `8890` (default): Standard port for PlayaVR API
- Any valid port number (1-65535)

## Configuration Methods

### Method 1: Edit appsettings.json (Recommended)

Simply edit the `appsettings.json` file:

```json
{
  "App": {
    "ShowAllActors": true,
    "SortPopularityByRating": true,
    "Host": "0.0.0.0",
    "Port": 8890
  }
}
```

### Method 2: Environment Variables

Set environment variables before running:

```bash
export App__ShowAllActors=true
export App__SortPopularityByRating=true
export App__Host="0.0.0.0"
export App__Port=8890
dotnet run
```

### Method 3: Command Line Arguments

Pass configuration via command line:

```bash
dotnet run --App__ShowAllActors=true --App__SortPopularityByRating=true --App__Host="0.0.0.0" --App__Port=8890
```

## Testing the Configuration

### Test Actor Filtering

```bash
# Test with ShowAllActors=false (default)
curl "http://localhost:8890/api/playa/v2/actors?page-size=5"

# Test with ShowAllActors=true
export App__ShowAllActors=true
curl "http://localhost:8890/api/playa/v2/actors?page-size=5"
```

### Test Popularity Sorting

```bash
# Test with SortPopularityByRating=false (default - sort by o_counter)
curl "http://localhost:8890/api/playa/v2/videos?page-size=5&order=popularity&direction=descending"

# Test with SortPopularityByRating=true (sort by rating)
export App__SortPopularityByRating=true
curl "http://localhost:8890/api/playa/v2/videos?page-size=5&order=popularity&direction=descending"
```

### Test Host and Port Configuration

```bash
# Test with default host and port (0.0.0.0:8890)
curl "http://localhost:8890/api/playa/v2/version"

# Test with custom port
export App__Port=9999
# Restart the application, then test:
curl "http://localhost:9999/api/playa/v2/version"

# Test with localhost only
export App__Host="127.0.0.1"
export App__Port=8890
# Restart the application, then test:
curl "http://127.0.0.1:8890/api/playa/v2/version"
```

## Notes

- Changes require restarting the application
- Both boolean options default to `false` to maintain backward compatibility
- Host defaults to `"0.0.0.0"` (all interfaces) and Port defaults to `8890`
- The rating field (`rating100`) is now included in video data regardless of the sorting preference
- Simply edit `appsettings.json` for the easiest configuration
- Host and Port configuration allows flexible deployment scenarios (localhost-only, specific IP, custom ports)
