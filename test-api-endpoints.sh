#!/bin/bash

# API Testing Script for az-tw-website-functions
# This script tests all endpoints defined in the project

# Configuration
API_BASE_URL="http://localhost:7071"
API_KEY="az-tw-DEV-website-api-key-9874"
TIMESTAMP=$(date +%s)
TEST_OUTPUT_DIR="./test-results"

# Create test output directory if it doesn't exist
mkdir -p "$TEST_OUTPUT_DIR"
echo "Test results will be saved in $TEST_OUTPUT_DIR"

# Helper functions for colorized output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to test an endpoint and output the result
test_endpoint() {
    local method=$1
    local endpoint=$2
    local description=$3
    local data=$4
    local expected_status_code=${5:-200}
    
    echo -e "${BLUE}===================================================${NC}"
    echo -e "${BLUE}Testing: $description${NC}"
    echo -e "${BLUE}$method $endpoint${NC}"
    
    # Create output file names
    local output_file="$TEST_OUTPUT_DIR/${method}_${endpoint//\//_}_${TIMESTAMP}.json"
    output_file=${output_file//\{*\}/placeholder}
    local headers_file="${output_file%.json}_headers.txt"
    
    # Construct curl command based on method
    local curl_cmd="curl -s -X $method \"$API_BASE_URL$endpoint\" -H \"x-api-key: $API_KEY\" -w \"\\n%{http_code}\""
    
    if [ "$method" == "POST" ] || [ "$method" == "PUT" ] && [ ! -z "$data" ]; then
        curl_cmd="$curl_cmd -H \"Content-Type: application/json\" -d '$data'"
    fi
    
    # Execute the curl command and capture output
    local result=$(eval $curl_cmd)
    local status_code=$(echo "$result" | tail -n1)
    local response=$(echo "$result" | sed '$d')
    
    # Save the response to a file
    echo "$response" > "$output_file"
    
    # Check if status code matches expected
    if [ "$status_code" -eq "$expected_status_code" ]; then
        echo -e "${GREEN}✓ Success: Status code $status_code${NC}"
    else
        echo -e "${RED}✗ Failed: Expected status code $expected_status_code, got $status_code${NC}"
    fi
    
    # Pretty print the response (limited to first few lines if large)
    if [ -n "$response" ]; then
        # Try to format as JSON if possible
        echo -e "${YELLOW}Response:${NC}"
        echo "$response" | jq '.' 2>/dev/null || echo "$response" | head -n 10
        if [ ${#response} -gt 500 ]; then
            echo -e "${YELLOW}...(output truncated, see full response in $output_file)${NC}"
        fi
    else
        echo -e "${YELLOW}Empty response${NC}"
    fi
    echo -e "${BLUE}===================================================${NC}"
    echo ""
}

echo "=== API Endpoint Testing ==="
echo "Starting tests at $(date)"

# =============================================
# Books API Tests
# =============================================
echo -e "\n${BLUE}== BOOKS API TESTS ==${NC}"

# Get all books
test_endpoint "GET" "/books" "Get all books"

# Get books with filter
test_endpoint "GET" "/books?category=Fiction&includeMedia=true" "Get fiction books with media"

# Create a test book
TEST_BOOK='{
  "title": "Test Book",
  "authorSlug": "test-author",
  "description": "A test book created via API testing",
  "content": "This is test content for the book.",
  "category": "Test",
  "status": "Draft",
  "tagsList": ["test", "api"]
}'

test_endpoint "POST" "/books/test-book-$TIMESTAMP" "Create test book" "$TEST_BOOK" 201

# Get single book
test_endpoint "GET" "/books/test-book-$TIMESTAMP" "Get created test book"

# Get single book with media
test_endpoint "GET" "/books/test-book-$TIMESTAMP?includeMedia=true" "Get created test book with media"

# Update the book
UPDATE_BOOK='{
  "title": "Updated Test Book",
  "authorSlug": "test-author",
  "description": "An updated test book via API testing",
  "content": "This is updated test content for the book.",
  "category": "Test",
  "status": "Draft",
  "tagsList": ["test", "api", "updated"]
}'

test_endpoint "PUT" "/books/test-book-$TIMESTAMP" "Update test book" "$UPDATE_BOOK"

# =============================================
# Contact Me API Tests
# =============================================
echo -e "\n${BLUE}== CONTACT ME API TESTS ==${NC}"

# Submit contact form
CONTACT_DATA='{
  "name": "Test User",
  "email": "test@example.com",
  "message": "This is a test message from the API test script. Please ignore."
}'

test_endpoint "POST" "/contact" "Submit contact form" "$CONTACT_DATA"

# Test validation - missing fields
BAD_CONTACT_DATA='{
  "name": "",
  "email": "invalid-email",
  "message": ""
}'

test_endpoint "POST" "/contact" "Submit invalid contact form (should fail)" "$BAD_CONTACT_DATA" 400

# =============================================
# Authors API Tests
# =============================================
echo -e "\n${BLUE}== AUTHORS API TESTS ==${NC}"

# Get all authors
test_endpoint "GET" "/authors" "Get all authors"

# Create a test author
TEST_AUTHOR='{
  "name": "Test Author",
  "slug": "test-author-'$TIMESTAMP'",
  "bio": "This is a test author created via API testing.",
  "socialLinks": {
    "twitter": "https://twitter.com/testauthor",
    "github": "https://github.com/testauthor"
  }
}'

test_endpoint "POST" "/authors/test-author-$TIMESTAMP" "Create test author" "$TEST_AUTHOR" 201

# Get single author
test_endpoint "GET" "/authors/test-author-$TIMESTAMP" "Get created test author"

# Update the author
UPDATE_AUTHOR='{
  "name": "Updated Test Author",
  "slug": "test-author-'$TIMESTAMP'",
  "bio": "This is an updated test author via API testing.",
  "socialLinks": {
    "twitter": "https://twitter.com/testauthor",
    "github": "https://github.com/testauthor",
    "linkedin": "https://linkedin.com/in/testauthor"
  }
}'

test_endpoint "PUT" "/authors/test-author-$TIMESTAMP" "Update test author" "$UPDATE_AUTHOR"

# =============================================
# BlogPosts API Tests
# =============================================
echo -e "\n${BLUE}== BLOG POSTS API TESTS ==${NC}"

# Get all blog posts
test_endpoint "GET" "/blogposts" "Get all blog posts"

# Get blog posts with filter
test_endpoint "GET" "/blogposts?authorSlug=test-author&includeMedia=true" "Get blog posts by test author with media"

# Create a test blog post
TEST_BLOG='{
  "title": "Test Blog Post",
  "authorSlug": "test-author",
  "excerpt": "This is a test excerpt.",
  "content": "This is test content for the blog post.",
  "category": "Test",
  "status": "Draft",
  "tagsList": ["test", "api"]
}'

test_endpoint "POST" "/blogposts/test-blog-$TIMESTAMP" "Create test blog post" "$TEST_BLOG" 201

# Get single blog post
test_endpoint "GET" "/blogposts/test-blog-$TIMESTAMP" "Get created test blog post"

# Get single blog post with media
test_endpoint "GET" "/blogposts/test-blog-$TIMESTAMP?includeMedia=true" "Get created test blog post with media"

# Update the blog post
UPDATE_BLOG='{
  "title": "Updated Test Blog Post",
  "authorSlug": "test-author",
  "excerpt": "This is an updated test excerpt.",
  "content": "This is updated test content for the blog post.",
  "category": "Test",
  "status": "Draft",
  "tagsList": ["test", "api", "updated"]
}'

test_endpoint "PUT" "/blogposts/test-blog-$TIMESTAMP" "Update test blog post" "$UPDATE_BLOG"

# =============================================
# Portfolio Piece API Tests
# =============================================
echo -e "\n${BLUE}== PORTFOLIO PIECE API TESTS ==${NC}"

# Get all portfolio pieces
test_endpoint "GET" "/portfolio" "Get all portfolio pieces"

# Create a test portfolio piece
TEST_PORTFOLIO='{
  "title": "Test Portfolio Piece",
  "authorSlug": "test-author",
  "excerpt": "This is a test portfolio excerpt.",
  "content": "This is test content for the portfolio piece.",
  "category": "Test",
  "status": "Draft",
  "tagsList": ["test", "api"]
}'

test_endpoint "POST" "/portfolio/test-portfolio-$TIMESTAMP" "Create test portfolio piece" "$TEST_PORTFOLIO" 201

# Get single portfolio piece
test_endpoint "GET" "/portfolio/test-portfolio-$TIMESTAMP" "Get created test portfolio piece"

# Get single portfolio piece with media
test_endpoint "GET" "/portfolio/test-portfolio-$TIMESTAMP?includeMedia=true" "Get created test portfolio piece with media"

# Update the portfolio piece
UPDATE_PORTFOLIO='{
  "title": "Updated Test Portfolio Piece",
  "authorSlug": "test-author",
  "excerpt": "This is an updated test portfolio excerpt.",
  "content": "This is updated test content for the portfolio piece.",
  "category": "Test",
  "status": "Draft",
  "tagsList": ["test", "api", "updated"]
}'

test_endpoint "PUT" "/portfolio/test-portfolio-$TIMESTAMP" "Update test portfolio piece" "$UPDATE_PORTFOLIO"

# =============================================
# Cleanup Tests (if needed)
# =============================================
echo -e "\n${BLUE}== CLEANUP TESTS ==${NC}"

# Uncomment these if you want to delete test items
# test_endpoint "DELETE" "/books/test-book-$TIMESTAMP" "Delete test book"
# test_endpoint "DELETE" "/authors/test-author-$TIMESTAMP" "Delete test author"
# test_endpoint "DELETE" "/blogposts/test-blog-$TIMESTAMP" "Delete test blog post"
# test_endpoint "DELETE" "/portfolio/test-portfolio-$TIMESTAMP" "Delete test portfolio piece"

# =============================================
# Summary
# =============================================
echo -e "\n${BLUE}== TEST SUMMARY ==${NC}"
echo "Tests completed at $(date)"
echo "Test results saved in $TEST_OUTPUT_DIR"
