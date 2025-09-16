#!/bin/bash

# StashPlayaVR API Docker Deployment Script
# This script helps you deploy the StashPlayaVR API using Docker

set -e

echo "🐳 StashPlayaVR API Docker Deployment Script"
echo "=============================================="

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

# Check if .env file exists
if [ ! -f .env ]; then
    echo "⚠️  .env file not found. Creating from template..."
    cp env.example .env
    echo "📝 Please edit .env file with your configuration before running again."
    echo "   Important: Change JWT_SECRET, STASH_API_KEY, and user passwords!"
    exit 1
fi

# Function to check if port is in use
check_port() {
    if lsof -Pi :8890 -sTCP:LISTEN -t >/dev/null ; then
        echo "⚠️  Port 8890 is already in use. Please stop the service using that port first."
        exit 1
    fi
}

# Function to deploy
deploy() {
    echo "🚀 Deploying StashPlayaVR API..."
    
    # Check port availability
    check_port
    
    # Build and start the service
    echo "📦 Building Docker image..."
    docker-compose build
    
    echo "🔄 Starting services..."
    docker-compose up -d
    
    echo "⏳ Waiting for service to start..."
    sleep 10
    
    # Check if service is running
    if docker-compose ps | grep -q "Up"; then
        echo "✅ StashPlayaVR API deployed successfully!"
        echo ""
        echo "🌐 API is available at: http://localhost:8890"
        echo "📋 Configuration endpoint: http://localhost:8890/api/playa/v2/config"
        echo "🔐 Authentication endpoint: http://localhost:8890/api/playa/v2/auth/sign-in-password"
        echo ""
        echo "📊 To view logs: docker-compose logs -f"
        echo "🛑 To stop: docker-compose down"
    else
        echo "❌ Deployment failed. Check logs with: docker-compose logs"
        exit 1
    fi
}

# Function to stop
stop() {
    echo "🛑 Stopping StashPlayaVR API..."
    docker-compose down
    echo "✅ Service stopped."
}

# Function to show status
status() {
    echo "📊 StashPlayaVR API Status:"
    docker-compose ps
}

# Function to show logs
logs() {
    echo "📋 StashPlayaVR API Logs:"
    docker-compose logs -f
}

# Function to update
update() {
    echo "🔄 Updating StashPlayaVR API..."
    docker-compose pull
    docker-compose up -d --build
    echo "✅ Update completed."
}

# Main script logic
case "${1:-deploy}" in
    "deploy")
        deploy
        ;;
    "stop")
        stop
        ;;
    "status")
        status
        ;;
    "logs")
        logs
        ;;
    "update")
        update
        ;;
    "help"|"-h"|"--help")
        echo "Usage: $0 [command]"
        echo ""
        echo "Commands:"
        echo "  deploy  - Deploy the PlayaVR API (default)"
        echo "  stop    - Stop the PlayaVR API"
        echo "  status  - Show service status"
        echo "  logs    - Show service logs"
        echo "  update  - Update and restart the service"
        echo "  help    - Show this help message"
        ;;
    *)
        echo "❌ Unknown command: $1"
        echo "Use '$0 help' for available commands."
        exit 1
        ;;
esac
