# Docker Deployment Guide for PlayaVR API

This guide explains how to build and deploy the PlayaVR API using Docker.

## 🐳 **Quick Start**

### **1. Build the Docker Image**
```bash
# Build the image
docker build -t playa-vr-api .

# Or build with a specific tag
docker build -t playa-vr-api:latest .
```

### **2. Run with Docker Compose (Recommended)**
```bash
# Start the service
docker-compose up -d

# View logs
docker-compose logs -f playa-vr-api

# Stop the service
docker-compose down
```

### **3. Run with Docker Run**
```bash
# Run the container
docker run -d \
  --name playa-vr-api \
  -p 8890:8890 \
  -e StashApp__Url=http://172.26.31.72:9969 \
  -e StashApp__GraphQLUrl=http://172.26.31.72:9969/graphql \
  -e StashApp__ApiKey=your-stash-api-key \
  playa-vr-api:latest
```

## 🔧 **Configuration**

### **Environment Variables**

You can configure the API using environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `App__Host` | Host to bind to | `0.0.0.0` |
| `App__Port` | Port to listen on | `8890` |
| `JWT__Secret` | JWT signing secret | (see appsettings.json) |
| `JWT__Issuer` | JWT issuer | `PlayaVR-API` |
| `JWT__Audience` | JWT audience | `PlayaVR-Client` |
| `StashApp__Url` | StashApp URL | `http://172.26.31.72:9969` |
| `StashApp__GraphQLUrl` | StashApp GraphQL URL | `http://172.26.31.72:9969/graphql` |
| `StashApp__ApiKey` | StashApp API key | (see appsettings.json) |
| `Users__admin` | Admin password | `admin123` |
| `Users__user` | User password | `user123` |

### **Volume Mounts**

Mount `appsettings.json` for custom configuration:
```bash
docker run -d \
  --name playa-vr-api \
  -p 8890:8890 \
  -v /path/to/your/appsettings.json:/app/appsettings.json:ro \
  playa-vr-api:latest
```

## 🚀 **Production Deployment**

### **1. Security Considerations**

**Change Default Passwords:**
```bash
# Set secure passwords
docker run -d \
  --name playa-vr-api \
  -p 8890:8890 \
  -e Users__admin=your-secure-admin-password \
  -e Users__user=your-secure-user-password \
  -e JWT__Secret=your-super-secure-jwt-secret-key-at-least-32-characters \
  playa-vr-api:latest
```

**Use HTTPS in Production:**
```bash
# Add SSL certificates
docker run -d \
  --name playa-vr-api \
  -p 443:8890 \
  -v /path/to/ssl/cert.pem:/app/cert.pem:ro \
  -v /path/to/ssl/key.pem:/app/key.pem:ro \
  -e ASPNETCORE_URLS=https://+:8890 \
  -e ASPNETCORE_Kestrel__Certificates__Default__Path=/app/cert.pem \
  -e ASPNETCORE_Kestrel__Certificates__Default__KeyPath=/app/key.pem \
  playa-vr-api:latest
```

### **2. Docker Compose for Production**

Update `docker-compose.yml` for production:
```yaml
version: '3.8'

services:
  playa-vr-api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: playa-vr-api
    ports:
      - "443:8890"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:8890
      - JWT__Secret=${JWT_SECRET}
      - StashApp__Url=${STASH_URL}
      - StashApp__ApiKey=${STASH_API_KEY}
      - Users__admin=${ADMIN_PASSWORD}
      - Users__user=${USER_PASSWORD}
    volumes:
      - ./ssl:/app/ssl:ro
      - ./logs:/app/logs
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "https://localhost:8890/api/playa/v2/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

### **3. Environment File**

Create `.env` file:
```bash
JWT_SECRET=your-super-secure-jwt-secret-key-at-least-32-characters
STASH_URL=http://your-stash-server:9969
STASH_API_KEY=your-stash-api-key
ADMIN_PASSWORD=your-secure-admin-password
USER_PASSWORD=your-secure-user-password
```

## 🔍 **Monitoring & Health Checks**

### **Health Check Endpoint**
The API includes a health check at:
```
GET http://localhost:8890/api/playa/v2/health
```

### **Docker Health Check**
The container includes built-in health checks:
```bash
# Check container health
docker ps
docker inspect playa-vr-api | grep Health -A 10
```

### **Logs**
```bash
# View logs
docker logs playa-vr-api

# Follow logs
docker logs -f playa-vr-api

# With docker-compose
docker-compose logs -f playa-vr-api
```

## 🛠 **Development**

### **Build for Development**
```bash
# Build with development settings
docker build -t playa-vr-api:dev --build-arg ASPNETCORE_ENVIRONMENT=Development .

# Run in development mode
docker run -d \
  --name playa-vr-api-dev \
  -p 8890:8890 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  playa-vr-api:dev
```

### **Debug Container**
```bash
# Enter the container
docker exec -it playa-vr-api /bin/bash

# Check application status
docker exec playa-vr-api dotnet --info
docker exec playa-vr-api ls -la /app
```

## 📦 **Image Details**

- **Base Image:** `mcr.microsoft.com/dotnet/aspnet:8.0`
- **Build Image:** `mcr.microsoft.com/dotnet/sdk:8.0`
- **Size:** ~200MB (optimized)
- **Port:** 8890
- **User:** Non-root (`appuser`)
- **Health Check:** Built-in

## 🔧 **Troubleshooting**

### **Common Issues**

**1. Port Already in Use:**
```bash
# Check what's using port 8890
sudo netstat -tulpn | grep 8890

# Kill the process or use different port
docker run -p 8891:8890 playa-vr-api:latest
```

**2. SkiaSharp Native Libraries:**
The image includes all required native libraries for SVG conversion.

**3. StashApp Connection:**
Ensure StashApp is accessible from the container:
```bash
# Test connectivity
docker exec playa-vr-api curl -f http://172.26.31.72:9969/graphql
```

**4. Permission Issues:**
The container runs as non-root user. If you need to mount volumes:
```bash
# Fix permissions
sudo chown -R 1000:1000 /path/to/mounted/directory
```

## 🎯 **API Endpoints**

Once running, the API will be available at:
- **Base URL:** `http://localhost:8890`
- **Health:** `http://localhost:8890/api/playa/v2/health`
- **Auth:** `http://localhost:8890/api/playa/v2/auth/*`
- **Videos:** `http://localhost:8890/api/playa/v2/videos`
- **Actors:** `http://localhost:8890/api/playa/v2/actors`
- **Studios:** `http://localhost:8890/api/playa/v2/studios`

---

**Your PlayaVR API is now containerized and ready for deployment!** 🚀
