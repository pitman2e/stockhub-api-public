COPY stock_transaction
FROM '/docker-entrypoint-initdb.d/stock_transaction.csv'
DELIMITER ',' 
CSV HEADER
NULL 'NULL';
