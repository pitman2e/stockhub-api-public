COPY stock_price
FROM '/docker-entrypoint-initdb.d/stock_price.csv'
DELIMITER ',' 
CSV HEADER
NULL 'NULL';
