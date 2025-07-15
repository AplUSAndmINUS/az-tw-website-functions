#!/bin/bash

# Test image upload script
# This script creates a simple test image and uploads it to test the UploadImage function

# Create a simple test image using ImageMagick (if available) or curl a test image
if command -v convert &> /dev/null; then
    echo "Creating test image with ImageMagick..."
    convert -size 200x200 xc:blue test-image.jpg
    echo "Created test-image.jpg"
elif command -v curl &> /dev/null; then
    echo "Downloading test image..."
    curl -o test-image.jpg "https://via.placeholder.com/200x200/0000FF/FFFFFF.jpg"
    echo "Downloaded test-image.jpg"
else
    echo "Neither ImageMagick nor curl available. Please create a test image manually."
    exit 1
fi

# Check if the image was created
if [ -f "test-image.jpg" ]; then
    echo "Test image ready: test-image.jpg ($(du -h test-image.jpg | cut -f1))"
    echo ""
    echo "To test upload, use this curl command (replace YOUR_API_KEY and YOUR_FUNCTION_URL):"
    echo "curl -X POST \"YOUR_FUNCTION_URL/media/images?fileName=test-image.jpg&authorId=test-author&description=Test%20Image&altText=Test%20alt%20text&purpose=coverImage\" \\"
    echo "  -H \"x-api-key: YOUR_API_KEY\" \\"
    echo "  -H \"Content-Type: image/jpeg\" \\"
    echo "  --data-binary @test-image.jpg"
    echo ""
else
    echo "Failed to create test image"
    exit 1
fi
