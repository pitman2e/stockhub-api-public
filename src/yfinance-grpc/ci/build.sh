#!/bin/sh
set -e # Exit immediately on error

# Change current working directory to the script's directory
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

export WEB_PORT=50051
export GIT_SHA=$(git rev-parse --short HEAD)

docker-compose -f docker-compose.yml up --build -d
