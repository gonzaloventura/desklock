#!/bin/bash
set -e

APP_NAME="DeskLock"
BUILD_DIR="build"
APP_BUNDLE="$BUILD_DIR/$APP_NAME.app"

echo "Building $APP_NAME..."

# Clean previous build
rm -rf "$BUILD_DIR"

# Create app bundle structure
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copy Info.plist and icon
cp Resources/Info.plist "$APP_BUNDLE/Contents/"
if [ -f Resources/AppIcon.icns ]; then
    cp Resources/AppIcon.icns "$APP_BUNDLE/Contents/Resources/"
fi

# Detect architecture — build universal if both are available
ARCH=$(uname -m)

if swiftc -print-target-info 2>/dev/null | grep -q "arm64" && \
   swiftc -print-target-info 2>/dev/null | grep -q "x86_64"; then
    echo "Building universal binary (arm64 + x86_64)..."

    swiftc -o "$APP_BUNDLE/Contents/MacOS/${APP_NAME}_arm64" \
        -framework Cocoa -framework Carbon -swift-version 5 \
        -target arm64-apple-macos13.0 \
        Sources/*.swift

    swiftc -o "$APP_BUNDLE/Contents/MacOS/${APP_NAME}_x86_64" \
        -framework Cocoa -framework Carbon -swift-version 5 \
        -target x86_64-apple-macos13.0 \
        Sources/*.swift

    lipo -create \
        "$APP_BUNDLE/Contents/MacOS/${APP_NAME}_arm64" \
        "$APP_BUNDLE/Contents/MacOS/${APP_NAME}_x86_64" \
        -output "$APP_BUNDLE/Contents/MacOS/$APP_NAME"

    rm "$APP_BUNDLE/Contents/MacOS/${APP_NAME}_arm64" \
       "$APP_BUNDLE/Contents/MacOS/${APP_NAME}_x86_64"
else
    echo "Building for $ARCH..."
    swiftc -o "$APP_BUNDLE/Contents/MacOS/$APP_NAME" \
        -framework Cocoa -framework Carbon -swift-version 5 \
        -target "${ARCH}-apple-macos13.0" \
        Sources/*.swift
fi

echo ""
echo "Build successful!"
echo "App bundle: $APP_BUNDLE"
echo ""
echo "To run:"
echo "  open $APP_BUNDLE"
echo ""
echo "To install:"
echo "  cp -r $APP_BUNDLE /Applications/"
