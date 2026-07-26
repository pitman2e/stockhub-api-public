#!/bin/sh
dbs=$(docker ps | grep -Po 'pg_db')
echo "Containers to backup: "
echo $dbs

nowDate=$(date +%Y%m%d)

for db in $dbs; do
    docker exec $db pg_dump -U pgadmin -h localhost -p 5432 sh_uat > ${nowDate}_sh_uat.pgdump
    docker exec $db pg_dump -U pgadmin -h localhost -p 5432 sh_prod > ${nowDate}_sh_prod.pgdump
done
