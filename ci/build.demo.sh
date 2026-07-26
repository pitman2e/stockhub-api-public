#!/bin/sh
set -e # Exit immediately on error

# Change current working directory to the script's directory
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

export COMPOSE_PROJECT_NAME=sh-demo
docker-compose -f docker-compose-demo.yml up --build
docker-compose -f docker-compose-demo.yml down --rmi local
