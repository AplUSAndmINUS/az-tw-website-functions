# Test image upload script for PowerShell
# This script downloads a simple test image and uploads it to test the UploadImage function

# Create a temp directory for the test image
$tempDir = ".\temp"
if (-not (Test-Path -Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir | Out-Null
}

# Download a sample image from Pixabay (free to use)
$testImagePath = "$tempDir\test-image.jpg"
Write-Host "Downloading test image..."

# Try to download from picsum
try {
    Invoke-WebRequest -Uri "https://picsum.photos/200/200" -OutFile $testImagePath
    Write-Host "Downloaded test-image.jpg from Lorem Picsum"
}
catch {
    Write-Host "Failed to download from Lorem Picsum, trying alternative source..."
    try {
        Invoke-WebRequest -Uri "https://raw.githubusercontent.com/mdn/learning-area/master/html/multimedia-and-embedding/images-in-html/dinosaur.jpg" -OutFile $testImagePath
        Write-Host "Downloaded test-image.jpg from GitHub"
    }
    catch {
        Write-Host "Failed to download image from alternative source. Creating a simple image file..."
        
        # Create a minimal valid JPEG file
        [byte[]]$jpegBytes = @(
            # JPEG SOI marker
            0xFF, 0xD8,
            # JFIF APP0 marker
            0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00,
            # Simple scan data
            0xFF, 0xDB, 0x00, 0x43, 0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09, 0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12, 0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20, 0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29, 0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39, 0x3D, 0x38, 0x32, 0x3C, 0x2E, 0x33, 0x34, 0x32,
            # End of image marker
            0xFF, 0xD9
        )
        
        [System.IO.File]::WriteAllBytes($testImagePath, $jpegBytes)
        Write-Host "Created minimal JPEG test file"
    }
}

# Check if the image was created
if (Test-Path -Path $testImagePath) {
    $fileInfo = Get-Item -Path $testImagePath
    Write-Host "Test image ready: $testImagePath (Size: $($fileInfo.Length) bytes)"
    
    # Get the API key from local.settings.json
    $localSettings = Get-Content -Path ".\src\Functions\local.settings.json" | ConvertFrom-Json
    $apiKey = $localSettings.Values.X_API_ENVIRONMENT_KEY
    
    # Determine the function URL (assuming running locally)
    $functionUrl = "http://localhost:7071"
    
    # Test using Invoke-RestMethod instead of curl
    Write-Host ""
    Write-Host "Testing image upload with Invoke-RestMethod:"
    
    # Build the URI with query parameters
    $uri = "$functionUrl/media/images?fileName=test-image.jpg&authorId=test-author&description=Test%20Image&altText=Test%20alt%20text&purpose=coverImage"
    Write-Host "URI: $uri"
    
    # Set up headers
    $headers = @{
        "x-api-key" = $apiKey
        "Content-Type" = "image/jpeg"
    }
    
    # Read file content as bytes
    $fileContent = [System.IO.File]::ReadAllBytes($testImagePath)
    
    Write-Host "Sending request..."
    
    try {
        # Send the request
        $response = Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body $fileContent
        
        # Display the response
        Write-Host "Upload successful!"
        $response | ConvertTo-Json -Depth 4
    }
    catch {
        Write-Host "Error during upload:"
        Write-Host $_.Exception.Message
        if ($_.ErrorDetails) {
            Write-Host $_.ErrorDetails
        }
    }
} else {
    Write-Host "Failed to create test image"
}
