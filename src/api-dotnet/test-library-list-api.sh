#!/bin/bash

# Manual Test Script for Library & List Management
# This script demonstrates how to use the new API endpoints

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration (Update these with your values)
API_BASE_URL="${API_BASE_URL:-http://localhost:7071/api}"
CLIENT_ID="${CLIENT_ID:-1}"
AUTH_TOKEN="${AUTH_TOKEN:-mock-token-for-testing}"

echo -e "${YELLOW}=== Library & List Management API Test ===${NC}\n"

# Test 1: Get existing libraries
echo -e "${GREEN}Test 1: Get existing libraries${NC}"
echo "GET ${API_BASE_URL}/clients/${CLIENT_ID}/libraries"
curl -s -X GET "${API_BASE_URL}/clients/${CLIENT_ID}/libraries" \
  -H "Authorization: Bearer ${AUTH_TOKEN}" \
  -H "Content-Type: application/json" | jq '.' || echo -e "${RED}Failed${NC}"
echo -e "\n"

# Test 2: Get existing lists
echo -e "${GREEN}Test 2: Get existing lists${NC}"
echo "GET ${API_BASE_URL}/clients/${CLIENT_ID}/lists"
curl -s -X GET "${API_BASE_URL}/clients/${CLIENT_ID}/lists" \
  -H "Authorization: Bearer ${AUTH_TOKEN}" \
  -H "Content-Type: application/json" | jq '.' || echo -e "${RED}Failed${NC}"
echo -e "\n"

# Test 3: Create a new library
echo -e "${GREEN}Test 3: Create a new library${NC}"
echo "POST ${API_BASE_URL}/clients/${CLIENT_ID}/libraries"
curl -s -X POST "${API_BASE_URL}/clients/${CLIENT_ID}/libraries" \
  -H "Authorization: Bearer ${AUTH_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Client Documents",
    "description": "Document library created for testing"
  }' | jq '.' || echo -e "${RED}Failed${NC}"
echo -e "\n"

# Test 4: Create a new list
echo -e "${GREEN}Test 4: Create a new list${NC}"
echo "POST ${API_BASE_URL}/clients/${CLIENT_ID}/lists"
curl -s -X POST "${API_BASE_URL}/clients/${CLIENT_ID}/lists" \
  -H "Authorization: Bearer ${AUTH_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Project Tasks",
    "description": "Task list created for testing",
    "template": "tasks"
  }' | jq '.' || echo -e "${RED}Failed${NC}"
echo -e "\n"

# Test 5: Validation error - missing name
echo -e "${GREEN}Test 5: Validation error - missing name${NC}"
echo "POST ${API_BASE_URL}/clients/${CLIENT_ID}/libraries (should fail)"
curl -s -X POST "${API_BASE_URL}/clients/${CLIENT_ID}/libraries" \
  -H "Authorization: Bearer ${AUTH_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "description": "Library without a name"
  }' | jq '.' || echo -e "${RED}Failed${NC}"
echo -e "\n"

# Test 6: Validation error - invalid template
echo -e "${GREEN}Test 6: Validation error - invalid template${NC}"
echo "POST ${API_BASE_URL}/clients/${CLIENT_ID}/lists (should fail)"
curl -s -X POST "${API_BASE_URL}/clients/${CLIENT_ID}/lists" \
  -H "Authorization: Bearer ${AUTH_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test List",
    "template": "invalidTemplate"
  }' | jq '.' || echo -e "${RED}Failed${NC}"
echo -e "\n"

echo -e "${YELLOW}=== Test Complete ===${NC}"
echo -e "Note: These tests will only work if:"
echo -e "  1. The backend is running (npm start)"
echo -e "  2. Authentication is configured (or mocked)"
echo -e "  3. A client with ID ${CLIENT_ID} exists in the database"
echo -e "  4. jq is installed for JSON formatting"
