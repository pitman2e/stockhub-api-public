#!/bin/sh
export COMPOSE_PROJECT_NAME=stockhub-uat
export API_PORT=9001
export PG_HOST=pg_db
export PG_PORT=5432
export PG_DATABASE=sh_uat
export PG_USERNAME=${PG_CREDS_USR}
export PG_PASSWORD=${PG_CREDS_PSW}
export GIT_SHA=$(git rev-parse --short HEAD)
export CORS_ORIGINS=https://www.example.com
export STOCKHUB_API_PY_BASE_URL="stockhub-api-py:5000"
export JWT_SECRET_KEY=${JWT_SECRET_KEY}

docker network create pg_net 2>/dev/null || true

docker-compose -f ./ci/docker-compose.yml up -d
