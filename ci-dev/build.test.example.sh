#!/bin/sh
export PG_HOST=192.168.1.123
export PG_PORT=5432
export PG_DATABASE=sh_test
export PG_USERNAME=db_user_name
export PG_PASSWORD=db_user_password
export DATABASE_CONSTR="User ID=${PG_USERNAME};Password=${PG_PASSWORD};Host=${PG_HOST};Port=${PG_PORT};Database=${PG_DATABASE};"

docker-compose -f ./ci-dev/docker-compose.test.yml build
docker run -e DATABASE_CONSTR="${DATABASE_CONSTR}" stockhub-api-test ./build.test.init.sh