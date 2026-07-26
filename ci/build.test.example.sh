#!/bin/sh
export PG_HOST=pg_db
export PG_PORT=5432
export PG_DATABASE=sh_test
export PG_USERNAME=db_user_name
export PG_PASSWORD=db_user_password
export DATABASE_CONSTR="User ID=${PG_USERNAME};Password=${PG_PASSWORD};Host=${PG_HOST};Port=${PG_PORT};Database=${PG_DATABASE};"
export STOCKHUB_API_PY_BASE_URL="stockhub-api-py:5000"

docker-compose -f ./ci/docker-compose.test.yml build
docker run \
    -e DATABASE_CONSTR="${DATABASE_CONSTR}" \
    -e STOCKHUB_API_PY_BASE_URL="${STOCKHUB_API_PY_BASE_URL}" \
    --network=pg_net stockhub-api-test \
    ./build.test.init.sh
