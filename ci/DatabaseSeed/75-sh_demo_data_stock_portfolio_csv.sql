COPY stock_portfolio
FROM '/docker-entrypoint-initdb.d/stock_portfolio.csv'
DELIMITER ',' 
CSV HEADER
NULL 'NULL';
