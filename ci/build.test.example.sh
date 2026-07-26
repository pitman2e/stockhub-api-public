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

docker-compose -f docker-compose.test.yml build
docker run \
    -e DATABASE_CONSTR="${DATABASE_CONSTR}" \
    -e STOCKHUB_YFINANCE_GRPC="${STOCKHUB_YFINANCE_GRPC}" \
    --network=pg_net stockhub-api-test \
    ./build.test.init.sh
