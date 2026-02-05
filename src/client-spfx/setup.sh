#!/bin/bash

# Development Environment Setup Script
# This script helps set up the SharePoint Framework development environment

echo "🚀 SharePoint External User Manager - Setup Script"
echo "=================================================="

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "❌ Node.js is not installed. Please install Node.js 18.x"
    echo "   Download from: https://nodejs.org/"
    echo "   Recommended version: 18.19.0"
    exit 1
fi

# Check Node.js version
NODE_VERSION=$(node -v)
echo "📋 Current Node.js version: $NODE_VERSION"

# Check if the version is compatible with SPFx 1.18.2
MAJOR_VERSION=$(echo $NODE_VERSION | cut -d'.' -f1 | cut -d'v' -f2)
if [ "$MAJOR_VERSION" -eq 18 ]; then
    echo "✅ Node.js version is compatible with SPFx 1.18.2"
elif [ "$MAJOR_VERSION" -eq 16 ]; then
    echo "⚠️  Node.js 16.x is supported but 18.x is recommended"
else
    echo "❌ Node.js $NODE_VERSION is NOT compatible with SPFx 1.18.2"
    echo "   SPFx 1.18.2 requires Node.js 18.17.1+ (but <19.0.0)"
    echo "   Current version: $NODE_VERSION"
    echo ""
    echo "🔧 Please switch to Node.js 18.x:"
    echo "   Using nvm: nvm install 18.19.0 && nvm use 18.19.0"
    echo "   Or download from: https://nodejs.org/"
    exit 1
fi
MAJOR_VERSION=$(echo $NODE_VERSION | cut -d'.' -f1 | sed 's/v//')
if [ "$MAJOR_VERSION" -lt 16 ] || [ "$MAJOR_VERSION" -gt 18 ]; then
    echo "⚠️  Warning: Node.js version $NODE_VERSION may not be compatible"
    echo "   SharePoint Framework 1.18.2 requires Node.js 16.x or 18.x"
    echo "   Consider using a version manager:"
    echo "   - Windows: nvm-windows"
    echo "   - macOS/Linux: nvm"
    echo ""
    echo "   Install compatible version:"
    echo "   nvm install 18.17.1"
    echo "   nvm use 18.17.1"
    echo ""
    read -p "Continue anyway? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo "📦 Installing dependencies..."
npm install

if [ $? -eq 0 ]; then
    echo "✅ Dependencies installed successfully!"
else
    echo "❌ Failed to install dependencies"
    echo "   This might be due to Node.js version compatibility"
    echo "   Please ensure you're using Node.js 16.x or 18.x"
    exit 1
fi

echo ""
echo "🎉 Setup complete! You can now:"
echo "   • Run 'npm run serve' to start development server"
echo "   • Run 'npm run build' to build the solution"
echo "   • Run 'npm run package-solution' to create deployment package"
echo ""
echo "📖 For detailed development guide, see DEVELOPER_GUIDE.md"