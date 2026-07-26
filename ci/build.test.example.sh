#!/bin/sh
set -e # Exit immediately on error

# Change current working directory to the script's directory
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

export PG_HOST=pg_db
export PG_PORT=5432
export PG_DATABASE=sh_test
export PG_USERNAME=db_user_name
export PG_PASSWORD=db_user_password
export DATABASE_CONSTR="User ID=${PG_USERNAME};Password=${PG_PASSWORD};Host=${PG_HOST};Port=${PG_PORT};Database=${PG_DATABASE};"
export STOCKHUB_YFINANCE_GRPC=http://stockhub-yfinance-grpc:50051

# The --exit-code-from flag is critical here.
docker-compose -f docker-compose.unittests.yml up --build --exit-code-from test
