#!/bin/bash

echo "🧪 Testing StashApp-PlayaVR Bridge"
echo "================================="

BASE_URL="http://localhost:8890/api"

# Function to test endpoint
test_endpoint() {
    local endpoint=$1
    local description=$2
    echo "Testing $description..."
    echo "GET $BASE_URL$endpoint"
    
    response=$(curl -s -w "\nHTTP_CODE:%{http_code}" "$BASE_URL$endpoint")
    http_code=$(echo "$response" | tail -n1 | cut -d: -f2)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" -eq 200 ]; then
        echo "✅ Success (HTTP $http_code)"
        echo "Response: $(echo "$body" | head -c 200)..."
    else
        echo "❌ Failed (HTTP $http_code)"
        echo "Response: $body"
    fi
    echo ""
}

echo "Make sure to:"
echo "1. Update appsettings.json with your StashApp details"
echo "2. Start the bridge with: dotnet run"
echo "3. Run this test script from another terminal"
echo ""
echo "Press Enter to continue with tests..."
read

# Test PlayaVR v2 API endpoints
test_endpoint "/playa/v2/version" "API Version"
test_endpoint "/playa/v2/config" "Configuration"
test_endpoint "/playa/v2/videos?page-index=0&page-size=5" "Videos List"
test_endpoint "/playa/v2/categories" "Categories"
test_endpoint "/health" "Health Check"

echo "🏁 Test completed!"
echo ""
echo "If all tests pass, your bridge is ready for PlayaVR!"
echo "Add 'localhost:8890' as a website in PlayaVR app."
