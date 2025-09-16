#!/bin/bash

# JWT Secret Generator for StashPlayaVR API
# This script generates a secure JWT secret key

echo "🔐 StashPlayaVR API JWT Secret Generator"
echo "=========================================="
echo ""

# Generate a secure random JWT secret (64 characters)
JWT_SECRET=$(openssl rand -base64 48 | tr -d "=+/" | cut -c1-64)

echo "Generated JWT Secret:"
echo "======================"
echo "$JWT_SECRET"
echo ""

echo "📝 Add this to your .env file:"
echo "JWT_SECRET=$JWT_SECRET"
echo ""

echo "🔒 Security Notes:"
echo "- Keep this secret secure and private"
echo "- Use different secrets for different environments"
echo "- Never commit this secret to version control"
echo "- Store it securely (password manager, environment variables, etc.)"
echo ""

echo "✅ JWT Secret generated successfully!"
